using SupportDesk.Contracts.Contracts.Requests;
using SupportDesk.Contracts.Contracts.Responses;
using SupportDesk.Contracts.Requests;
using SupportDesk.Contracts.Responses;

namespace SupportDesk.Application.Auth;

public interface IAuthService
{
    Task<LoginResponse> TryLogin(LoginRequest request, CancellationToken cancellationToken);
}