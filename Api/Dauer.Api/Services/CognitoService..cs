using System.Net;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Dauer.Api.Model;
using SQLitePCL;

namespace Dauer.Api.Services;

public class CognitoService : ICognitoService
{
  private readonly ILogger<CognitoService> log_;
  private readonly IAmazonCognitoIdentityProvider cognito_;
  private readonly string clientId_;
  private readonly string poolId_;

  public CognitoService(ILogger<CognitoService> log, IAmazonCognitoIdentityProvider cognito, string clientId, string poolId)
  {
    log_ = log;
    cognito_ = cognito;
    clientId_ = clientId;
    poolId_ = poolId;
  }

  public async Task<bool> SignUpAsync(User user)
  {
    if (user.CognitoId != null) { return false; }
    if (user.Email == null) { return false; }

    log_.LogInformation("Creating Cognito user for email=\'{email}\'", user.Email);

    var email = new AttributeType
    {
      Name = "email",
      Value = user.Email,
    };

    var name = new AttributeType
    {
      Name = "name",
      Value = user.Name,
    };

    var attributes = new List<AttributeType>
    {
      email,
      name
    };

    var signUpRequest = new SignUpRequest
    {
      UserAttributes = attributes,
      ClientId = clientId_,
      Username = user.Email,
      Password = PasswordGenerator.Generate(16),
    };

    try
    {
      SignUpResponse response = await cognito_.SignUpAsync(signUpRequest);
      bool ok = response.HttpStatusCode == HttpStatusCode.OK;

      user.CognitoId = response.UserSub;

      if (!ok) { return false; }
      //var adminAser = await GetAdminUserAsync(user.Email, poolId_).ConfigureAwait(false);
    }
    catch (Exception e)
    {
      log_.LogInformation("Exception creating Cognito user: {e}", e);
    }


    return true;
  }

  private async Task<AdminGetUserResponse> GetAdminUserAsync(string userName, string poolId)
  {
    AdminGetUserRequest userRequest = new AdminGetUserRequest
    {
      Username = userName,
      UserPoolId = poolId,
    };

    AdminGetUserResponse response = await cognito_.AdminGetUserAsync(userRequest);
    return response;
  }
}
