using Dauer.Model;
using Dauer.Model.Strava;

namespace Dauer.Adapters.Strava;

public class StravaClient : IStravaClient
{
  public StravaConfig Config { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

  public bool IsSignedIn => throw new NotImplementedException();

  public Dictionary<string, Cookie> Cookies { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

  public double AuthenticateProgress => throw new NotImplementedException();

  public Task<bool> AuthenticateAsync()
  {
    throw new NotImplementedException();
  }

  public Task<bool> IsAuthenticatedAsync()
  {
    throw new NotImplementedException();
  }

  public Task<bool> LogoutAsync()
  {
    throw new NotImplementedException();
  }
}
