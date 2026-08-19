namespace FixedIT.API.Models.Enums;

public enum JobPostingStatus
{
    Open = 1,
    Closed = 2
}
public enum JobOfferStatus
{
    Pending = 1,
    Accepted = 2,
    Rejected = 3
}

public enum ReservationStatus
{
    Pending = 1,
    Accepted = 2,
    InProgress = 3,
    Completed = 4,
    Cancelled = 5
}

public enum PaymentStatus
{
    Pending = 1,
    Completed = 2,
    Refunded = 3
}

public enum NotificationType
{
    General = 1,
    Reservation = 2,
    Message = 3,
    Payment = 4
}
