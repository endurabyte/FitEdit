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
  private readonly IEmailService email_;

  private const string html = @"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {
            background-color: #363636;
            color: #ffffff;
            font-family: Arial, sans-serif;
        }
        .button {
            display: inline-block;
            border-radius: 12px;
            background-color: #7981fe;
            border: none;
            color: #ffffff;
            text-align: center;
            font-size: 14px;
            padding: 15px;
            width: 200px;
            transition: all 0.5s;
            cursor: pointer;
            text-decoration: none;
            margin: 5px;
        }
        .button:hover {
            background-color: #5f63c8;
        }
    </style>
</head>
<body>
    <h1>Welcome to FitEdit!</h1>

    <p>You'll be whipping your fitness data into shape in no time.</p>

    <p>FitEdit is free in read-only mode. Saving edits requires payment. You can sign up now or try the app first.</p>

    <a href=""https://www.fitedit.io/download.html"" class=""button"">Download FitEdit</a>

    <a href=""https://buy.stripe.com/cN25lH2pla0p7YI4gh"" class=""button"">Sign up for the monthly plan</a>

    <a href=""https://buy.stripe.com/4gw5lH0hd4G5en6eUU"" class=""button"">Sign up for the annual plan</a>
</body>
</html>
";

  public StripeWebhookController(ILogger<StripeWebhookController> log, StripeConfig config, IEmailService email)
  {
    log_ = log;
    config_ = config;
    email_ = email;
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
        // get the customer email
        var customer = stripeEvent.Data.Object as Customer;
        if (customer == null)
        {
          log_.LogError("Could not get customer from event");
          return BadRequest();
        }

        await email_.SendEmailAsync(customer.Email, "Welcome to FitEdit", html);
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