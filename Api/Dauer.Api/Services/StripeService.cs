using Stripe;

namespace Dauer.Api.Services;

public class StripeService : IStripeService
{
  private readonly ILogger<StripeService> log_;
  private readonly CustomerService customers_;

  public StripeService(ILogger<StripeService> log, CustomerService customers)
  {
    log_ = log;
    customers_ = customers;
  }

  public async Task CreateCustomer(Model.User user)
  {
    if (user.StripeId != null) { return; }

    var opts = new CustomerCreateOptions
    {
      Metadata = new Dictionary<string, string>
      {
        { "userId", user.Id ?? "" },
      },
      Name = user.Name,
      Email = user.Email,
    };

    Customer customer = await customers_.CreateAsync(opts).ConfigureAwait(false);
    user.StripeId = customer.Id;

    log_.LogInformation("Created customer {id} for name \'{name}\' (email=\'{email}\')", customer.Id, opts.Name, opts.Email);
  }
}
