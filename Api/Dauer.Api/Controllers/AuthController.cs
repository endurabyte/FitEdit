using Dauer.Dto;
using Microsoft.AspNetCore.Mvc;

namespace Dauer.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
  private readonly ILogger<AuthController> _logger;

  public AuthController(ILogger<AuthController> logger)
  {
    _logger = logger;
  }

  [HttpGet(Name = "GetAuthorization")]
  public async Task<Authorization> Get([FromQuery] AuthRequest req)
  {
    return await Task.FromResult(new Authorization(""));
  }
}