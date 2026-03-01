namespace MedCareHub.Api.Models;

public static class SlotStatus
{
    public const string Available = "available";
    public const string Booked = "booked";
    public const string Cancelled = "cancelled";
}

public static class BookingStatus
{
    public const string Confirmed = "confirmed";
    public const string Cancelled = "cancelled";
    public const string Completed = "completed";
}

public static class AuditOutcome
{
    public const string Success = "success";
    public const string Fail = "fail";
}