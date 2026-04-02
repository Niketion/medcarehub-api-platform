using MedCareHub.Api.Models;

namespace MedCareHub.Api.Services;

/// <summary>
/// Defines the application service responsible for booking lifecycle operations.
/// </summary>
/// <remarks>
/// This service encapsulates the business flow for:
/// creating a booking, cancelling it, completing it, and marking it as paid.
/// </remarks>
public interface IBookingService
{
    /// <summary>
    /// Creates a booking for the specified patient and slot.
    /// </summary>
    /// <param name="patientSub">Stable user identifier taken from the authenticated principal.</param>
    /// <param name="slotId">Identifier of the slot to book.</param>
    /// <param name="ct">Cancellation token for the current request.</param>
    /// <returns>The created booking entity.</returns>
    /// <exception cref="MedCareHub.Api.Exceptions.NotFoundException">
    /// Thrown when the target slot does not exist.
    /// </exception>
    /// <exception cref="MedCareHub.Api.Exceptions.ConflictException">
    /// Thrown when the slot is no longer available or has already been booked.
    /// </exception>
    Task<Booking> CreateBookingAsync(string patientSub, Guid slotId, CancellationToken ct);

    /// <summary>
    /// Cancels an existing booking owned by the specified patient.
    /// </summary>
    /// <param name="patientSub">Stable user identifier taken from the authenticated principal.</param>
    /// <param name="bookingId">Identifier of the booking to cancel.</param>
    /// <param name="ct">Cancellation token for the current request.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    /// <exception cref="MedCareHub.Api.Exceptions.NotFoundException">
    /// Thrown when the booking does not exist.
    /// </exception>
    /// <exception cref="MedCareHub.Api.Exceptions.ForbiddenException">
    /// Thrown when the booking is not owned by the calling patient.
    /// </exception>
    /// <exception cref="MedCareHub.Api.Exceptions.ConflictException">
    /// Thrown when the booking cannot be cancelled, for example because it is already completed.
    /// </exception>
    Task CancelBookingAsync(string patientSub, Guid bookingId, CancellationToken ct);

    /// <summary>
    /// Marks a booking as completed.
    /// </summary>
    /// <param name="bookingId">Identifier of the booking to complete.</param>
    /// <param name="ct">Cancellation token for the current request.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    /// <exception cref="MedCareHub.Api.Exceptions.NotFoundException">
    /// Thrown when the booking does not exist.
    /// </exception>
    /// <exception cref="MedCareHub.Api.Exceptions.ConflictException">
    /// Thrown when the booking is cancelled and therefore cannot be completed.
    /// </exception>
    Task CompleteBookingAsync(Guid bookingId, CancellationToken ct);

    /// <summary>
    /// Marks a booking as paid and stores the payment timestamp.
    /// </summary>
    /// <param name="bookingId">Identifier of the booking to update.</param>
    /// <param name="ct">Cancellation token for the current request.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    /// <exception cref="MedCareHub.Api.Exceptions.NotFoundException">
    /// Thrown when the booking does not exist.
    /// </exception>
    /// <exception cref="MedCareHub.Api.Exceptions.ConflictException">
    /// Thrown when the booking is cancelled and therefore cannot be marked as paid.
    /// </exception>
    Task MarkBookingPaidAsync(Guid bookingId, CancellationToken ct);
}