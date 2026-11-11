using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using PatientManagement.API.Auth;
using PatientManagement.Application.Auth;
using PatientManagement.Application.Auth.Handlers;
using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Exceptions;

namespace PatientManagement.API.Controllers;

/// <summary>Sign-in, session refresh, sign-out and password change for the doctor's account.</summary>
[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(
    LoginCommandHandler login,
    RefreshSessionCommandHandler refreshSession,
    LogoutCommandHandler logout,
    ChangePasswordCommandHandler changePassword,
    IValidator<LoginCommand> loginValidator,
    IValidator<ChangePasswordCommand> changePasswordValidator,
    ICurrentUser currentUser,
    IOptions<AuthOptions> authOptions) : ControllerBase
{
    /// <summary>Signs in with username and password. Returns an access token and sets the refresh cookie.</summary>
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    [HttpPost("login")]
    [ProducesResponseType<AccessTokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AccessTokenResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var command = new LoginCommand(request.Username, request.Password);
        await loginValidator.ValidateAndThrowAsync(command, cancellationToken);

        var session = await login.HandleAsync(command, cancellationToken);
        RefreshTokenCookie.Set(Response, session);
        return Ok(ToResponse(session));
    }

    /// <summary>Exchanges the refresh cookie for a new access token and a new refresh cookie (rotation).</summary>
    [AllowAnonymous]
    [HttpPost("refresh-token")]
    [ProducesResponseType<AccessTokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AccessTokenResponse>> Refresh(CancellationToken cancellationToken)
    {
        var presented = Request.Cookies[RefreshTokenCookie.Name];
        if (string.IsNullOrEmpty(presented))
        {
            RefreshTokenCookie.Clear(Response);
            throw new AuthenticationFailedException(AuthMessages.SessionEndedCode, AuthMessages.SessionEndedMessage);
        }

        try
        {
            var session = await refreshSession.HandleAsync(new RefreshSessionCommand(presented), cancellationToken);
            RefreshTokenCookie.Set(Response, session);
            return Ok(ToResponse(session));
        }
        catch (AuthenticationFailedException)
        {
            RefreshTokenCookie.Clear(Response);
            throw;
        }
    }

    /// <summary>Ends the session that owns the refresh cookie. Safe to call without a valid session.</summary>
    [AllowAnonymous]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var presented = Request.Cookies[RefreshTokenCookie.Name];
        if (!string.IsNullOrEmpty(presented))
        {
            await logout.HandleAsync(new LogoutCommand(presented), cancellationToken);
        }

        RefreshTokenCookie.Clear(Response);
        return NoContent();
    }

    /// <summary>Changes the signed-in password. Every session ends, so the user signs in again.</summary>
    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var command = new ChangePasswordCommand(currentUser.UserId, request.CurrentPassword, request.NewPassword);
        await changePasswordValidator.ValidateAndThrowAsync(command, cancellationToken);

        await changePassword.HandleAsync(command, cancellationToken);
        RefreshTokenCookie.Clear(Response);
        return NoContent();
    }

    private AccessTokenResponse ToResponse(SessionTokens session) =>
        new(session.AccessToken, authOptions.Value.AccessTokenMinutes * 60);
}
