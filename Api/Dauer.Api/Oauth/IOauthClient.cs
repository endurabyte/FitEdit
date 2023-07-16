using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Dauer.Api.Oauth;

public interface IOauthClient
{
  void ConfigureJwt(JwtBearerOptions opts);
}