namespace PatientManagement.Application.Interfaces;

/// <summary>The signed-in user, read from the access token's claims (CLAUDE.md 6.4).</summary>
public interface ICurrentUser
{
    int UserId { get; }
}
