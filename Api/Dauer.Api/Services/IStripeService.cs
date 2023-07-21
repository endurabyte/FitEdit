namespace Dauer.Api.Services
{
  public interface IStripeService
  {
    Task CreateCustomer(Model.User user);
  }
}