using System.Globalization;
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Exceptions;

namespace PatientManagement.API.Auth;

/// <summary>Reads the user id from the access token's subject claim.</summary>
public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public int UserId
    {
        get
        {
            var subject = httpContextAccessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            return int.TryParse(subject, NumberStyles.None, CultureInfo.InvariantCulture, out var userId)
                ? userId
                : throw new AuthenticationFailedException("session_ended", "Your session has ended. Sign in again.");
        }
    }
}
