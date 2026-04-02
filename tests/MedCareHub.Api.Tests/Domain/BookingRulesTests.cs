using FluentAssertions;
using MedCareHub.Api.Domain;
using MedCareHub.Api.Exceptions;
using MedCareHub.Api.Models;
using Xunit;

namespace MedCareHub.Api.Tests.Domain;

public sealed class BookingRulesTests
{
    [Fact]
    public void EnsureSlotCanBeBooked_ShouldThrow_WhenSlotIsNotAvailable()
    {
        var slot = new Slot { Status = SlotStatus.Booked };

        var act = () => BookingRules.EnsureSlotCanBeBooked(slot);

        act.Should().Throw<ConflictException>()
            .WithMessage("Slot not available.");
    }

    [Fact]
    public void TryCancel_ShouldCancelBooking_AndRestoreSlotAvailability()
    {
        var booking = new Booking
        {
            PatientSub = "patient-1",
            Status = BookingStatus.Confirmed,
            Slot = new Slot { Status = SlotStatus.Booked }
        };

        var changed = BookingRules.TryCancel(booking);

        changed.Should().BeTrue();
        booking.Status.Should().Be(BookingStatus.Cancelled);
        booking.Slot.Status.Should().Be(SlotStatus.Available);
    }

    [Fact]
    public void TryCancel_ShouldReturnFalse_WhenAlreadyCancelled()
    {
        var booking = new Booking
        {
            Status = BookingStatus.Cancelled,
            Slot = new Slot { Status = SlotStatus.Available }
        };

        var changed = BookingRules.TryCancel(booking);

        changed.Should().BeFalse();
    }

    [Fact]
    public void EnsurePatientCanCancel_ShouldThrow_WhenPatientIsNotOwner()
    {
        var booking = new Booking
        {
            PatientSub = "owner-sub",
            Status = BookingStatus.Confirmed
        };

        var act = () => BookingRules.EnsurePatientCanCancel(booking, "other-sub");

        act.Should().Throw<ForbiddenException>()
            .WithMessage("Not owner.");
    }

    [Fact]
    public void EnsurePatientCanCancel_ShouldThrow_WhenBookingIsCompleted()
    {
        var booking = new Booking
        {
            PatientSub = "owner-sub",
            Status = BookingStatus.Completed
        };

        var act = () => BookingRules.EnsurePatientCanCancel(booking, "owner-sub");

        act.Should().Throw<ConflictException>()
            .WithMessage("Completed bookings cannot be cancelled.");
    }

    [Fact]
    public void TryComplete_ShouldCompleteBooking()
    {
        var booking = new Booking
        {
            Status = BookingStatus.Confirmed
        };

        var changed = BookingRules.TryComplete(booking);

        changed.Should().BeTrue();
        booking.Status.Should().Be(BookingStatus.Completed);
    }

    [Fact]
    public void TryComplete_ShouldReturnFalse_WhenAlreadyCompleted()
    {
        var booking = new Booking
        {
            Status = BookingStatus.Completed
        };

        var changed = BookingRules.TryComplete(booking);

        changed.Should().BeFalse();
    }

    [Fact]
    public void TryComplete_ShouldThrow_WhenBookingIsCancelled()
    {
        var booking = new Booking
        {
            Status = BookingStatus.Cancelled
        };

        var act = () => BookingRules.TryComplete(booking);

        act.Should().Throw<ConflictException>()
            .WithMessage("Cancelled bookings cannot be completed.");
    }
}