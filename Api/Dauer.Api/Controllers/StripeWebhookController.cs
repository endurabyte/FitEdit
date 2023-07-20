using Dauer.Api.Config;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace Dauer.Api.Controllers;

[ApiController]
[Route("stripe/webhooks")]
public class StripeWebhookController : ControllerBase
{
  private readonly ILogger<StripeWebhookController> log_;
  private readonly StripeConfig config_;

  public StripeWebhookController(ILogger<StripeWebhookController> log, StripeConfig config)
  {
    log_ = log;
    config_ = config;
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
      log_.LogInformation($"Got event {stripeEvent.Type}");

      if (stripeEvent.Type == Events.CustomerCreated)
      {
      }
      else if (stripeEvent.Type == Events.CustomerDeleted)
      {
      }
      else if (stripeEvent.Type == Events.PersonCreated)
      {
      }
      else if (stripeEvent.Type == Events.PersonDeleted)
      {
      }
      else if (stripeEvent.Type == Events.PersonUpdated)
      {
      }
      else if (stripeEvent.Type == Events.SubscriptionScheduleAborted)
      {
      }
      else if (stripeEvent.Type == Events.SubscriptionScheduleCanceled)
      {
      }
      else if (stripeEvent.Type == Events.SubscriptionScheduleCreated)
      {
      }
      else if (stripeEvent.Type == Events.SubscriptionScheduleExpiring)
      {
      }
      else if (stripeEvent.Type == Events.SubscriptionScheduleUpdated)
      {
      }
      else
      {
        log_.LogError("Unhandled event type: {0}", stripeEvent.Type);
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