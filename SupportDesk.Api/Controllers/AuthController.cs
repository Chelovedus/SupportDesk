using Microsoft.AspNetCore.Mvc;
using SupportDesk.Application.Auth;
using SupportDesk.Contracts.Contracts.Requests;
using SupportDesk.Contracts.Contracts.Responses;
using SupportDesk.Domain;

namespace SupportDesk.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{

    private readonly IAuthService _authService;
    
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }
    
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.TryLogin(request: request, cancellationToken: cancellationToken);
            return Ok(response);
        }
        catch (DomainException exception)
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }
    }
}