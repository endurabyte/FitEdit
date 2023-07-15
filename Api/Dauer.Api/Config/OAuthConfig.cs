namespace Dauer.Api.Config;

public class OauthConfig
{
  public string Authority => $"https://cognito-idp.{AwsRegion}.amazonaws.com/{AwsRegion}_{UserPoolId}";
  public string AwsRegion { get; set; } = "";
  public string UserPoolId { get; set; } = "";
  public string ClientId { get; set; } = "";
  public string SecurityDefinitionName { get; set; } = "";
}
