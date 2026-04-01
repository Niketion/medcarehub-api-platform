using System.Security.Claims;

namespace MedCareHub.Api.Tests.TestHelpers;

public static class TestPrincipalFactory
{
    public static ClaimsPrincipal Create(string sub, params string[] roles)
    {
        var claims = new List<Claim>
        {
            new("sub", sub),
            new(ClaimTypes.NameIdentifier, sub),
            new(ClaimTypes.Name, sub)
        };

        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var identity = new ClaimsIdentity(claims, authenticationType: "TestAuth");
        return new ClaimsPrincipal(identity);
    }
}