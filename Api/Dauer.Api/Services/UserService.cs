using Dauer.Api.Data;

namespace Dauer.Api.Services;

public class UserService : IUserService
{
  private readonly ILogger<UserService> log_;
  private readonly IEmailService email_;
  private readonly ICognitoService cognito_;
  private readonly IStripeService stripe_;
  private readonly IUserRepo repo_;

  public UserService(ILogger<UserService> log, IEmailService email, ICognitoService cognito, IStripeService stripe, IUserRepo repo)
  {
    log_ = log;
    email_ = email;
    cognito_ = cognito;
    stripe_ = stripe;
    repo_ = repo;
  }

  public async Task AddOrUpdate(Model.User? user)
  {
    if (user == null) { return; }

    if (user.Email != null && !repo_.Exists(user.Email))
    {
      user.Id = $"{Guid.NewGuid()}";
      await repo_.AddOrUpdateAsync(user).ConfigureAwait(false);

      // New user. Send onboarding email, add Stripe customer.
      await email_.AddContactAsync(user).ConfigureAwait(false);
      await stripe_.CreateCustomer(user).ConfigureAwait(false);
      await cognito_.SignUpAsync(user).ConfigureAwait(false);
    }

    // Existing user.
    await repo_.AddOrUpdateAsync(user).ConfigureAwait(false);
  }

  public async Task<Model.User?> FindAsync(string? email) => await repo_.FindAsync(email).ConfigureAwait(false);
}
