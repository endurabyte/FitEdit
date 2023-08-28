#nullable enable

using Dauer;

namespace Dauer.Model.Clients;

public interface IFitEditClient
{
  string AccessToken { get; set; }

  Task<bool> IsAuthenticatedAsync(CancellationToken ct = default);
  Task<bool> AuthorizeGarminAsync(string? username, CancellationToken ct = default);
  Task<bool> DeauthorizeGarminAsync(string? username, CancellationToken ct = default);

  Task<bool> AuthorizeStravaAsync(string? username, CancellationToken ct = default);
  Task<bool> DeauthorizeStravaAsync(string? username, CancellationToken ct = default);
}


