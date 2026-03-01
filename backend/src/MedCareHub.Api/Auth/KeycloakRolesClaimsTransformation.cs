using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Text.Json;

namespace MedCareHub.Api.Auth;

/// <summary>
/// Keycloak put roles inside realm_access.roles and/or resource_access[client].roles.
/// ASP.NET expects roles as ClaimTypes.Role.
/// This transformer flattens those roles into ClaimTypes.Role.
/// </summary>
public sealed class KeycloakRolesClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity id || !id.IsAuthenticated)
            return Task.FromResult(principal);

        // Already flattened?
        if (id.Claims.Any(c => c.Type == ClaimTypes.Role))
            return Task.FromResult(principal);

        TryAddRealmRoles(id);
        TryAddClientRoles(id);

        return Task.FromResult(principal);
    }

    private static void TryAddRealmRoles(ClaimsIdentity id)
    {
        var realmAccess = id.FindFirst("realm_access")?.Value;
        if (string.IsNullOrWhiteSpace(realmAccess))
            return;

        try
        {
            using var doc = JsonDocument.Parse(realmAccess);
            if (!doc.RootElement.TryGetProperty("roles", out var roles)) return;
            foreach (var r in roles.EnumerateArray())
            {
                var role = r.GetString();
                if (!string.IsNullOrWhiteSpace(role))
                    id.AddClaim(new Claim(ClaimTypes.Role, role));
            }
        }
        catch
        {
            // ignore malformed json
        }
    }

    private static void TryAddClientRoles(ClaimsIdentity id)
    {
        var resourceAccess = id.FindFirst("resource_access")?.Value;
        if (string.IsNullOrWhiteSpace(resourceAccess))
            return;

        try
        {
            using var doc = JsonDocument.Parse(resourceAccess);

            // Example:
            // "resource_access": { "medcarehub-web": { "roles": ["patient"] } }
            foreach (var client in doc.RootElement.EnumerateObject())
            {
                if (!client.Value.TryGetProperty("roles", out var roles)) continue;
                foreach (var r in roles.EnumerateArray())
                {
                    var role = r.GetString();
                    if (!string.IsNullOrWhiteSpace(role))
                        id.AddClaim(new Claim(ClaimTypes.Role, role));
                }
            }
        }
        catch
        {
            // ignore
        }
    }
}
