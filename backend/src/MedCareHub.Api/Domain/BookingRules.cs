using MedCareHub.Api.Exceptions;
using MedCareHub.Api.Models;

namespace MedCareHub.Api.Domain;

public static class BookingRules
{
    public static void EnsureSlotCanBeBooked(Slot slot)
    {
        if (!string.Equals(slot.Status, SlotStatus.Available, StringComparison.OrdinalIgnoreCase))
            throw new ConflictException("Slot not available.");
    }

    public static void EnsurePatientCanCancel(Booking booking, string patientSub)
    {
        if (!string.Equals(booking.PatientSub, patientSub, StringComparison.Ordinal))
            throw new ForbiddenException("Not owner.");

        if (string.Equals(booking.Status, BookingStatus.Completed, StringComparison.OrdinalIgnoreCase))
            throw new ConflictException("Completed bookings cannot be cancelled.");
    }

    public static bool TryCancel(Booking booking)
    {
        if (string.Equals(booking.Status, BookingStatus.Cancelled, StringComparison.OrdinalIgnoreCase))
            return false;

        booking.Status = BookingStatus.Cancelled;

        if (booking.Slot is not null &&
            string.Equals(booking.Slot.Status, SlotStatus.Booked, StringComparison.OrdinalIgnoreCase))
        {
            booking.Slot.Status = SlotStatus.Available;
        }

        return true;
    }

    public static bool TryComplete(Booking booking)
    {
        if (string.Equals(booking.Status, BookingStatus.Cancelled, StringComparison.OrdinalIgnoreCase))
            throw new ConflictException("Cancelled bookings cannot be completed.");

        if (string.Equals(booking.Status, BookingStatus.Completed, StringComparison.OrdinalIgnoreCase))
            return false;

        booking.Status = BookingStatus.Completed;
        return true;
    }
}