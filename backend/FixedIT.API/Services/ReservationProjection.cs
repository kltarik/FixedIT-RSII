using System.Linq.Expressions;
using FixedIT.API.DTOs.Reservations;
using FixedIT.API.Models;
using FixedIT.API.Models.Enums;

namespace FixedIT.API.Services;

internal static class ReservationProjection
{
    public static readonly Expression<Func<Reservation, ReservationResponse>> Response =
        reservation => new ReservationResponse(
            reservation.Id,
            reservation.ClientUserId,
            reservation.ClientUser.FirstName,
            reservation.ClientUser.LastName,
            reservation.ProfessionalProfileId,
            reservation.ProfessionalProfile.UserId,
            reservation.ProfessionalProfile.User.FirstName,
            reservation.ProfessionalProfile.User.LastName,
            reservation.CategoryId,
            reservation.Category.Name,
            reservation.ServiceDescription,
            reservation.ScheduledAt,
            reservation.DurationMinutes,
            reservation.TotalPrice,
            reservation.Status,
            reservation.CancellationReason,
            reservation.Payment != null
                && reservation.Payment.Status == PaymentStatus.Completed,
            reservation.Payment == null
                ? null
                : (PaymentStatus?)reservation.Payment.Status,
            reservation.CreatedAt,
            reservation.UpdatedAt);
}
