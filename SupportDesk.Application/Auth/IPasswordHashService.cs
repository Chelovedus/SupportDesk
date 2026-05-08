namespace SupportDesk.Application.Auth;

public interface IPasswordHashService
{
    public string Hash(string password);
    public bool Verify(string password, string passwordHash);
}