namespace MedCareHub.Api.Models;

/// <summary>
/// Available lifecycle states for a slot.
/// </summary>
public static class SlotStatus
{
    public const string Available = "available";
    public const string Booked = "booked";
    public const string Cancelled = "cancelled";
}

/// <summary>
/// Available lifecycle states for a booking.
/// </summary>
public static class BookingStatus
{
    public const string Confirmed = "confirmed";
    public const string Cancelled = "cancelled";
    public const string Completed = "completed";
}

/// <summary>
/// Available payment states for a booking.
/// </summary>
public static class PaymentStatuses
{
    public const string Unpaid = "unpaid";
    public const string Paid = "paid";
}

/// <summary>
/// Available outcomes for audit events.
/// </summary>
public static class AuditOutcome
{
    public const string Success = "success";
    public const string Fail = "fail";
}