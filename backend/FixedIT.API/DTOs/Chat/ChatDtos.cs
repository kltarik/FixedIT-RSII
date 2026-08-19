using System.ComponentModel.DataAnnotations;

namespace FixedIT.API.DTOs.Chat;

public sealed record CreateConversationRequest : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int? ReservationId { get; init; }

    [StringLength(450)]
    public string? ParticipantUserId { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var hasReservation = ReservationId.HasValue;
        var hasParticipant = !string.IsNullOrWhiteSpace(ParticipantUserId);
        if (hasReservation == hasParticipant)
        {
            yield return new ValidationResult(
                "Navedite rezervaciju ili učesnika razgovora, ali ne oboje.",
                [nameof(ReservationId), nameof(ParticipantUserId)]);
        }
    }
}

public sealed record ConversationParticipantResponse(
    string UserId,
    string FirstName,
    string LastName,
    string? ProfilePictureUrl);

public sealed record MessageResponse(
    int Id,
    int ConversationId,
    string SenderUserId,
    string SenderFirstName,
    string SenderLastName,
    string Content,
    DateTime SentAt);

public sealed record ConversationResponse(
    int Id,
    int? ReservationId,
    DateTime CreatedAt,
    IReadOnlyCollection<ConversationParticipantResponse> Participants,
    MessageResponse? LastMessage);
