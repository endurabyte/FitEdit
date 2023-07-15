using Dauer.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dauer.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
  private readonly ILogger<AuthController> log_;

  public AuthController(ILogger<AuthController> log)
  {
    log_ = log;
  }

  [HttpGet(Name = "GetAuthorization"), Authorize]
  public async Task<Authorization> Get([FromQuery] AuthRequest _)
  {
    return await Task.FromResult(new Authorization());
  }
}