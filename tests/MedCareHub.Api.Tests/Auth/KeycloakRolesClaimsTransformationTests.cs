using FluentAssertions;
using MedCareHub.Api.Auth;
using System.Security.Claims;
using Xunit;

namespace MedCareHub.Api.Tests.Auth;

public sealed class KeycloakRolesClaimsTransformationTests
{
    [Fact]
    public async Task TransformAsync_ShouldFlattenRealmAndClientRoles()
    {
        var identity = new ClaimsIdentity(authenticationType: "Bearer");
        identity.AddClaim(new Claim("realm_access", """{"roles":["operator"]}"""));
        identity.AddClaim(new Claim("resource_access", """{"medcarehub-web":{"roles":["patient","doctor"]}}"""));

        var principal = new ClaimsPrincipal(identity);
        var sut = new KeycloakRolesClaimsTransformation();

        var result = await sut.TransformAsync(principal);

        result.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .Should()
            .BeEquivalentTo(new[] { "operator", "patient", "doctor" });
    }

    [Fact]
    public async Task TransformAsync_ShouldNotDuplicate_WhenRoleClaimsAlreadyExist()
    {
        var identity = new ClaimsIdentity(authenticationType: "Bearer");
        identity.AddClaim(new Claim(ClaimTypes.Role, "operator"));
        identity.AddClaim(new Claim("realm_access", """{"roles":["admin"]}"""));

        var principal = new ClaimsPrincipal(identity);
        var sut = new KeycloakRolesClaimsTransformation();

        var result = await sut.TransformAsync(principal);

        result.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .Should()
            .Equal("operator");
    }

    [Fact]
    public async Task TransformAsync_ShouldIgnoreMalformedJson()
    {
        var identity = new ClaimsIdentity(authenticationType: "Bearer");
        identity.AddClaim(new Claim("realm_access", """{not-valid-json}"""));

        var principal = new ClaimsPrincipal(identity);
        var sut = new KeycloakRolesClaimsTransformation();

        var act = async () => await sut.TransformAsync(principal);

        await act.Should().NotThrowAsync();
        principal.Claims.Where(c => c.Type == ClaimTypes.Role).Should().BeEmpty();
    }

    [Fact]
    public async Task TransformAsync_ShouldDoNothing_WhenIdentityIsNotAuthenticated()
    {
        var identity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(identity);
        var sut = new KeycloakRolesClaimsTransformation();

        var result = await sut.TransformAsync(principal);

        result.Claims.Where(c => c.Type == ClaimTypes.Role).Should().BeEmpty();
    }
}