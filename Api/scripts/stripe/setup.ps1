
$sk = $env:STRIPE_SECRET_KEY

echo $sk
$events = @(
    "charge.failed",
    "charge.succeeded",
    "person.created",
    "person.deleted",
    "person.updated",
    "subscription_schedule.canceled",
    "subscription_schedule.aborted",
    "subscription_schedule.created",
    "subscription_schedule.expiring",
    "subscription_schedule.updated"
)

$eventsData = $events | ForEach-Object { "-d"; "enabled_events[]=$_" }

curl "https://api.stripe.com/v1/webhook_endpoints" `
  -u "${sk}:" `
  -d "url=https://api.fitedit.io/stripe/webhooks" `
  $eventsData


# {
#   "id": "we_1NTtV1Ig4FIuTIjml45OCgyO",
#   "object": "webhook_endpoint",
#   "api_version": null,
#   "application": null,
#   "created": 1689369963,
#   "description": null,
#   "enabled_events": [
#     "charge.failed",
#     "charge.succeeded",
#     "person.created",
#     "person.deleted",
#     "person.updated",
#     "subscription_schedule.canceled",
#     "subscription_schedule.aborted",
#     "subscription_schedule.created",
#     "subscription_schedule.expiring",
#     "subscription_schedule.updated"
#   ],
#   "livemode": true,
#   "metadata": {},
#   "secret": "whsec_awOCRQwqsSbQlPTZVJnm5KVJVXnEoRTx",
#   "status": "enabled",
#   "url": "https://api.fitedit.io/stripe/webhooks"
# }
