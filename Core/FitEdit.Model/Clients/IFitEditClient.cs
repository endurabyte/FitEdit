using FitEdit;

namespace FitEdit.Model.Clients;

public interface IFitEditClient
{
  string AccessToken { get; set; }

  Task<bool> IsAuthenticatedAsync(CancellationToken ct = default);
  Task<bool> AuthorizeGarminAsync(string? username, CancellationToken ct = default);
  Task<bool> DeauthorizeGarminAsync(CancellationToken ct = default);

  Task<bool> AuthorizeStravaAsync(CancellationToken ct = default);
  Task<bool> DeauthorizeStravaAsync(CancellationToken ct = default);
}


