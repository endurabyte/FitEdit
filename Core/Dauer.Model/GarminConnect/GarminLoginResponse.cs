#nullable enable
using System.Text.Json.Serialization;

namespace Dauer.Model.GarminConnect;

public class GarminLoginResponse
{
  [JsonPropertyName("consentTypeList")]
  public List<string>? ConsentTypeList { get; set; }

  [JsonPropertyName("customerMfaInfo")]
  public string? CustomerMfaInfo { get; set; }

  [JsonPropertyName("responseStatus")]
  public GarminResponseStatus? ResponseStatus { get; set; }

  [JsonPropertyName("serviceTicketId")]
  public string? ServiceTicketId { get; set; }

  [JsonPropertyName("serviceURL")]
  public string? ServiceUrl { get; set; }
}
