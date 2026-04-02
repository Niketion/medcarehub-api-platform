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

public sealed class PrestazioniControllerTests
{
    [Fact]
    public async Task Create_ShouldPersistBasePrice_AndAuditSuccess()
    {
        await using var db = TestDbFactory.Create();
        var fakeAudit = new FakeAuditService();

        var sut = new PrestazioniController(db, fakeAudit)
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
            new CreatePrestazioneRequest("Visita cardiologica", 30, "ECG base", 80m),
            CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<PrestazioneDto>().Subject;

        dto.Name.Should().Be("Visita cardiologica");
        dto.BasePrice.Should().Be(80m);

        db.Prestazioni.Should().ContainSingle();
        db.Prestazioni.Single().BasePrice.Should().Be(80m);

        fakeAudit.Calls.Should().ContainSingle(x =>
            x.Event == "prestazione_created" &&
            x.Outcome == AuditOutcome.Success &&
            x.ActorSub == "operator-sub");
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenBasePriceIsNegative()
    {
        await using var db = TestDbFactory.Create();
        var fakeAudit = new FakeAuditService();

        var sut = new PrestazioniController(db, fakeAudit);

        var result = await sut.Create(
            new CreatePrestazioneRequest("Visita cardiologica", 30, null, -1m),
            CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        fakeAudit.Calls.Should().BeEmpty();
    }
}