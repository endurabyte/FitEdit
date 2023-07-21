using Dauer.Api.Config;
using Dauer.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace Dauer.Api.Controllers;

[ApiController]
[Route("stripe/webhooks")]
public class StripeWebhookController : ControllerBase
{
  private readonly ILogger<StripeWebhookController> log_;
  private readonly StripeConfig config_;
  private readonly IUserService users_;

  public StripeWebhookController(ILogger<StripeWebhookController> log, StripeConfig config, IUserService users)
  {
    log_ = log;
    config_ = config;
    users_ = users;
  }

  [HttpPost]
  public async Task<IActionResult> Index()
  {
    var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
    try
    {
      var stripeEvent = EventUtility.ConstructEvent(json,
          Request.Headers["Stripe-Signature"], config_?.EndpointSecret ?? "");

      // Handle the event
      log_.LogInformation("Got event {event}", stripeEvent.Type);

      switch (stripeEvent.Type)
      {
        case Events.CustomerCreated:
        {
          // get the customer email
          if (stripeEvent.Data.Object is not Customer customer)
          {
            log_.LogError("Could not get customer from event");
            return BadRequest();
          }

          await users_.AddOrUpdate(new Model.User
          {
            Name = customer.Name,
            Email = customer.Email,
            StripeId = customer.Id,
          });
          break;
        }

        case Events.CustomerDeleted:
          break;
        case Events.PersonCreated:
          break;
        case Events.PersonDeleted:
          break;
        case Events.PersonUpdated:
          break;
        case Events.SubscriptionScheduleAborted:
          break;
        case Events.SubscriptionScheduleCanceled:
          break;
        case Events.SubscriptionScheduleCreated:
          break;
        case Events.SubscriptionScheduleExpiring:
          break;
        case Events.SubscriptionScheduleUpdated:
          break;
        default:
          log_.LogError("Unhandled event type: {event}", stripeEvent.Type);
          break;
      }

      return Ok();
    }
    catch (StripeException e)
    {
      log_.LogError(e, "Stripe webhook exception");
      return BadRequest();
    }
  }
}