using MedCareHub.Api.Exceptions;
using MedCareHub.Api.Models;

namespace MedCareHub.Api.Domain;

/// <summary>
/// Pure domain rules for booking state transitions.
/// </summary>
/// <remarks>
/// These rules are side-effect free except for controlled mutation of the passed entities.
/// They are intended for unit-level validation of booking lifecycle constraints.
/// </remarks>
public static class BookingRules
{
    /// <summary>
    /// Ensures that a slot is in a bookable state.
    /// </summary>
    /// <exception cref="ConflictException">Thrown when the slot is not available.</exception>
    public static void EnsureSlotCanBeBooked(Slot slot)
    {
        if (!string.Equals(slot.Status, SlotStatus.Available, StringComparison.OrdinalIgnoreCase))
            throw new ConflictException("Slot not available.");
    }

    /// <summary>
    /// Ensures that the specified patient can cancel the given booking.
    /// </summary>
    /// <exception cref="ForbiddenException">Thrown when the booking is not owned by the patient.</exception>
    /// <exception cref="ConflictException">Thrown when the booking is already completed.</exception>
    public static void EnsurePatientCanCancel(Booking booking, string patientSub)
    {
        if (!string.Equals(booking.PatientSub, patientSub, StringComparison.Ordinal))
            throw new ForbiddenException("Not owner.");

        if (string.Equals(booking.Status, BookingStatus.Completed, StringComparison.OrdinalIgnoreCase))
            throw new ConflictException("Completed bookings cannot be cancelled.");
    }

    /// <summary>
    /// Cancels the booking if possible and restores slot availability when applicable.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when the booking state changed;
    /// <see langword="false"/> when it was already cancelled.
    /// </returns>
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

    /// <summary>
    /// Marks the booking as completed when allowed.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when the booking state changed;
    /// <see langword="false"/> when it was already completed.
    /// </returns>
    /// <exception cref="ConflictException">Thrown when a cancelled booking is completed.</exception>
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