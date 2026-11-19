using LittleEcommerce.Source.Application.Interfaces.Services;
using LittleEcommerce.Source.Shared.DTOs;
using LittleEcommerce.Source.Shared.DTOs.Requests.Auth;
using LittleEcommerce.Source.Shared.DTOs.Results.Auth;
using LittleEcommerce.Source.Shared.Extensions;
using LittleEcommerce.Source.Shared.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LittleEcommerce.Source.Controllers.Store;

[ApiController]
[Route("api/v1/[controller]")]
[Tags("Autenticação")]
public class AuthController(IAuthService service) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingOptions.AuthPolicy)]
    [EndpointSummary("Cadastrar cliente")]
    [EndpointDescription("Cria a conta com o papel Customer e já devolve o par de tokens.")]
    [ProducesResponseType<APISuccessResponse<AuthResult>>(StatusCodes.Status201Created)]
    [ProducesResponseType<APIErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<APIErrorResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<APIErrorResponse>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await service.RegisterAsync(request, this.Client(), ct);
        return APIResponse.Created(result, "Cadastro realizado");
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingOptions.AuthPolicy)]
    [EndpointSummary("Entrar")]
    [EndpointDescription("Troca e-mail e senha por um access token (curto) e um refresh token (longo).")]
    [ProducesResponseType<APISuccessResponse<AuthResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<APIErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<APIErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<APIErrorResponse>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await service.LoginAsync(request, this.Client(), ct);
        return APIResponse.Success(result, "Login realizado");
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingOptions.AuthPolicy)]
    [EndpointSummary("Renovar tokens")]
    [EndpointDescription("Gera um novo par de tokens e invalida o refresh token usado. Reusar um refresh token já trocado encerra todas as sessões do usuário.")]
    [ProducesResponseType<APISuccessResponse<AuthResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<APIErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<APIErrorResponse>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var result = await service.RefreshAsync(request, this.Client(), ct);
        return APIResponse.Success(result, "Tokens renovados");
    }

    [HttpPost("logout")]
    [Authorize]
    [EndpointSummary("Sair")]
    [EndpointDescription("Coloca o access token atual na blacklist até ele expirar. Se o refresh token for enviado, a sessão dele também é encerrada.")]
    [ProducesResponseType<APISuccessResponse<object>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest? request, CancellationToken ct)
    {
        await service.LogoutAsync(this.UserId(), this.TokenId(), this.TokenExpiresAt(), request ?? new LogoutRequest(null), ct);
        return APIResponse.Success(new { }, "Sessão encerrada");
    }

    [HttpPost("logout-all")]
    [Authorize]
    [EndpointSummary("Sair de todos os dispositivos")]
    [EndpointDescription("Invalida todos os access tokens e refresh tokens do usuário.")]
    [ProducesResponseType<APISuccessResponse<object>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> LogoutAll(CancellationToken ct)
    {
        await service.LogoutAllAsync(this.UserId(), ct);
        return APIResponse.Success(new { }, "Todas as sessões foram encerradas");
    }
}
