using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Dauer.Api;

public interface IOauthClient
{
  void ConfigureJwt(JwtBearerOptions opts);
}