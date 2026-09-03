using System.Data;
using System.Linq.Expressions;
using FixedIT.API.CustomExceptions;
using FixedIT.API.Data;
using FixedIT.API.DTOs.Common;
using FixedIT.API.DTOs.Jobs;
using FixedIT.API.Models;
using FixedIT.API.Models.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FixedIT.API.Services;

public sealed class JobOfferService(
    AppDbContext db,
    IPaginationService paginationService) : IJobOfferService
{
    private const string LockedJobPostingsSql =
        "SELECT * FROM [JobPostings] WITH (UPDLOCK, HOLDLOCK)";

    private static readonly Expression<Func<JobOffer, JobOfferResponse>> OfferProjection =
        offer => new JobOfferResponse(
            offer.Id,
            offer.JobPostingId,
            offer.ProfessionalProfileId,
            offer.ProfessionalProfile.UserId,
            offer.ProfessionalProfile.User.FirstName,
            offer.ProfessionalProfile.User.LastName,
            offer.ProfessionalProfile.User.ProfilePictureUrl,
            offer.ProfessionalProfile.AverageRating,
            offer.Message,
            offer.ProposedPrice,
            offer.Status);

    public async Task<JobOfferResponse> SubmitAsync(
        string professionalUserId,
        int jobId,
        CreateJobOfferRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var job = await db.JobPostings
            .FromSqlRaw(LockedJobPostingsSql)
            .SingleOrDefaultAsync(item => item.Id == jobId, cancellationToken)
            ?? throw new NotFoundException("Oglas za posao nije pronađen.");
        if (job.Status != JobPostingStatus.Open)
        {
            throw new BusinessException("Ponudu je moguće poslati samo na otvoren oglas za posao.");
        }

        if (job.ClientUserId == professionalUserId)
        {
            throw new BusinessException("Vlasnik oglasa ne može poslati ponudu na vlastiti oglas.");
        }

        var professionalProfileId = await db.ProfessionalProfiles
            .Where(profile => profile.UserId == professionalUserId
                && profile.IsVerified)
            .Select(profile => (int?)profile.Id)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Profil profesionalca nije pronađen.");
        if (await db.JobOffers
            .IgnoreQueryFilters()
            .AnyAsync(
                offer => offer.JobPostingId == jobId
                    && offer.ProfessionalProfileId == professionalProfileId,
                cancellationToken))
        {
            throw new BusinessException("Profesionalac može poslati samo jednu ponudu po oglasu.");
        }

        var offer = new JobOffer
        {
            JobPostingId = jobId,
            ProfessionalProfileId = professionalProfileId,
            Message = request.Message.Trim(),
            ProposedPrice = request.ProposedPrice,
            Status = JobOfferStatus.Pending
        };
        db.JobOffers.Add(offer);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            throw new BusinessException("Profesionalac može poslati samo jednu ponudu po oglasu.");
        }

        await transaction.CommitAsync(cancellationToken);
        return await GetOfferAsync(offer.Id, cancellationToken);
    }

    public async Task<PagedResponse<JobOfferResponse>> GetForJobAsync(
        string clientUserId,
        int jobId,
        PagedRequest request,
        CancellationToken cancellationToken)
    {
        await GetOwnedJobAsync(clientUserId, jobId, cancellationToken);
        var page = paginationService.Normalize(request);
        var query = db.JobOffers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(offer => offer.JobPostingId == jobId);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(offer => offer.Status)
            .ThenBy(offer => offer.ProposedPrice)
            .ThenBy(offer => offer.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(OfferProjection)
            .ToListAsync(cancellationToken);

        return new PagedResponse<JobOfferResponse>(
            items,
            total,
            page.Page,
            page.PageSize);
    }

    public Task<JobOfferResponse> AcceptAsync(
        string clientUserId,
        int jobId,
        int offerId,
        CancellationToken cancellationToken)
    {
        return ChangeStatusAsync(
            clientUserId,
            jobId,
            offerId,
            JobOfferStatus.Accepted,
            cancellationToken);
    }

    public Task<JobOfferResponse> RejectAsync(
        string clientUserId,
        int jobId,
        int offerId,
        CancellationToken cancellationToken)
    {
        return ChangeStatusAsync(
            clientUserId,
            jobId,
            offerId,
            JobOfferStatus.Rejected,
            cancellationToken);
    }

    private async Task<JobOfferResponse> ChangeStatusAsync(
        string clientUserId,
        int jobId,
        int offerId,
        JobOfferStatus requestedStatus,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var job = await GetOwnedJobForUpdateAsync(clientUserId, jobId, cancellationToken);
        var offers = await db.JobOffers
            .IgnoreQueryFilters()
            .Where(offer => offer.JobPostingId == jobId)
            .OrderBy(offer => offer.Id)
            .ToListAsync(cancellationToken);
        var target = offers.SingleOrDefault(offer => offer.Id == offerId)
            ?? throw new NotFoundException("Ponuda za posao nije pronađena.");

        if (target.Status == requestedStatus)
        {
            await transaction.CommitAsync(cancellationToken);
            return await GetOfferAsync(target.Id, cancellationToken);
        }

        if (target.Status != JobOfferStatus.Pending)
        {
            throw new BusinessException("Status je moguće promijeniti samo ponudama na čekanju.");
        }

        if (requestedStatus == JobOfferStatus.Accepted)
        {
            if (job.Status != JobPostingStatus.Open)
            {
                throw new BusinessException("Ponudu je moguće prihvatiti samo za otvoren oglas.");
            }

            if (offers.Any(offer => offer.Status == JobOfferStatus.Accepted))
            {
                throw new BusinessException("Za jedan oglas moguće je prihvatiti samo jednu ponudu.");
            }

            target.Status = JobOfferStatus.Accepted;
            foreach (var otherOffer in offers.Where(offer => offer.Id != target.Id))
            {
                otherOffer.Status = JobOfferStatus.Rejected;
            }

            job.Status = JobPostingStatus.Closed;
        }
        else
        {
            target.Status = JobOfferStatus.Rejected;
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            throw new BusinessException("Za jedan oglas moguće je prihvatiti samo jednu ponudu.");
        }

        await transaction.CommitAsync(cancellationToken);
        return await GetOfferAsync(target.Id, cancellationToken);
    }

    private async Task<JobPosting> GetOwnedJobAsync(
        string clientUserId,
        int jobId,
        CancellationToken cancellationToken)
    {
        return await db.JobPostings.SingleOrDefaultAsync(
            job => job.Id == jobId && job.ClientUserId == clientUserId,
            cancellationToken)
            ?? throw new NotFoundException("Oglas za posao nije pronađen.");
    }

    private async Task<JobPosting> GetOwnedJobForUpdateAsync(
        string clientUserId,
        int jobId,
        CancellationToken cancellationToken)
    {
        return await db.JobPostings
            .FromSqlRaw(LockedJobPostingsSql)
            .SingleOrDefaultAsync(
                job => job.Id == jobId && job.ClientUserId == clientUserId,
                cancellationToken)
            ?? throw new NotFoundException("Oglas za posao nije pronađen.");
    }

    private async Task<JobOfferResponse> GetOfferAsync(
        int offerId,
        CancellationToken cancellationToken)
    {
        return await db.JobOffers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(offer => offer.Id == offerId)
            .Select(OfferProjection)
            .SingleAsync(cancellationToken);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqlException { Number: 2601 or 2627 };
    }
}
