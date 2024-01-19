namespace FitEdit.Ui.Infra;

public interface IWebAuthenticator
{
  string? Username { get; set; }
  bool IsAuthenticated { get; set; }

  Task<bool> AuthenticateAsync(CancellationToken ct = default);
  Task<bool> LogoutAsync(CancellationToken ct = default);
}