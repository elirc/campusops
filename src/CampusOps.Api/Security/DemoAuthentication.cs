using System.Security.Claims;
using System.Text.Encodings.Web;
using CampusOps.Core.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
namespace CampusOps.Api.Security;

public static class SecurityPolicies { public const string Scheme = "DemoHeader"; public const string Operations = "Operations"; public const string Manager = "Manager"; }
public sealed class DemoAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync() { if (!Request.Headers.TryGetValue("X-Demo-Role", out var role) || !Request.Headers.TryGetValue("X-Demo-Campus", out var campus)) return Task.FromResult(AuthenticateResult.NoResult()); var roleValue = role.ToString(); if (roleValue is not ("Technician" or "Dispatcher" or "Manager")) return Task.FromResult(AuthenticateResult.Fail("Unknown demo role.")); var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "demo-user"), new Claim(ClaimTypes.Role, roleValue), new Claim("campus", campus.ToString())], SecurityPolicies.Scheme); return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SecurityPolicies.Scheme))); }
}
public sealed class CampusAuthorizer
{
    public void EnsureAccess(ClaimsPrincipal principal, string campus) { var allowed = principal.FindFirstValue("campus"); if (!string.Equals(allowed, campus, StringComparison.OrdinalIgnoreCase)) throw new DomainException("security.campus_forbidden", "Identity is not authorized for this campus."); }
}
