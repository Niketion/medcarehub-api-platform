using FluentAssertions;
using MedCareHub.Api.Auth;
using MedCareHub.Api.Controllers;
using MedCareHub.Api.DTOs;
using MedCareHub.Api.Models;
using MedCareHub.Api.Tests.TestHelpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace MedCareHub.Api.Tests.Controllers;

public sealed class SlotsControllerTests
{
    [Fact]
    public async Task Get_ShouldReturnPrestazioneBasePrice()
    {
        await using var db = TestDbFactory.Create();

        var prestazione = new Prestazione
        {
            Id = Guid.NewGuid(),
            Name = "Visita cardiologica",
            BasePrice = 90m
        };

        var slot = new Slot
        {
            Id = Guid.NewGuid(),
            DoctorId = "dr-rossi",
            PrestazioneId = prestazione.Id,
            Prestazione = prestazione,
            StartsAt = new DateTimeOffset(2026, 04, 01, 9, 0, 0, TimeSpan.Zero),
            EndsAt = new DateTimeOffset(2026, 04, 01, 9, 30, 0, TimeSpan.Zero),
            Status = SlotStatus.Available
        };

        db.Prestazioni.Add(prestazione);
        db.Slots.Add(slot);
        await db.SaveChangesAsync();

        var sut = new SlotsController(db, new FakeAuditService());

        var result = await sut.Get(null, null, null, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var items = ok.Value.Should().BeAssignableTo<IEnumerable<SlotDto>>().Subject.ToList();

        items.Should().ContainSingle();
        items[0].PrestazioneBasePrice.Should().Be(90m);
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenPrestazioneDoesNotExist()
    {
        await using var db = TestDbFactory.Create();
        var fakeAudit = new FakeAuditService();

        var sut = new SlotsController(db, fakeAudit)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = TestPrincipalFactory.Create("operator-sub", Roles.Operator)
                }
            }
        };

        var result = await sut.Create(
            new CreateSlotRequest(
                "dr-rossi",
                Guid.NewGuid(),
                new DateTimeOffset(2026, 04, 01, 9, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 04, 01, 9, 30, 0, TimeSpan.Zero)),
            CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        fakeAudit.Calls.Should().BeEmpty();
    }
}