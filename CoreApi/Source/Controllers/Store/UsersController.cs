using LittleEcommerce.Source.Application.Interfaces.Services;
using LittleEcommerce.Source.Shared.DTOs;
using LittleEcommerce.Source.Shared.DTOs.Requests.Users;
using LittleEcommerce.Source.Shared.DTOs.Results.Users;
using LittleEcommerce.Source.Shared.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LittleEcommerce.Source.Controllers.Store;

[ApiController]
[Route("api/v1/[controller]")]
[Tags("Usuários")]
[Authorize]
public class UsersController(IUserService service) : ControllerBase
{
    [HttpGet("me")]
    [EndpointSummary("Meu perfil")]
    [ProducesResponseType<APISuccessResponse<UserResult>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var result = await service.GetAsync(this.UserId(), ct);
        return APIResponse.Success(result);
    }

    [HttpPut("me")]
    [EndpointSummary("Atualizar meu perfil")]
    [ProducesResponseType<APISuccessResponse<UserResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<APIErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var result = await service.UpdateProfileAsync(this.UserId(), request, ct);
        return APIResponse.Success(result, "Perfil atualizado");
    }

    [HttpPatch("me/password")]
    [EndpointSummary("Trocar minha senha")]
    [EndpointDescription("Troca a senha e encerra todas as sessões; é preciso entrar de novo.")]
    [ProducesResponseType<APISuccessResponse<object>>(StatusCodes.Status200OK)]
    [ProducesResponseType<APIErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        await service.ChangePasswordAsync(this.UserId(), request, ct);
        return APIResponse.Success(new { }, "Senha alterada. Entre novamente.");
    }
}
