using Android.Content.PM;
using Microsoft.Maui.Authentication;
using Android.Content;
namespace Dauer.Ui.Android;

/// <summary>
/// Needed for WebAuthenticator.
/// https://learn.microsoft.com/en-us/xamarin/essentials/web-authenticator?tabs=android
/// </summary>
[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(
	new[] { Intent.ActionView },
	Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
	DataScheme = CallbackScheme)]
public class WebAuthenticatorCallbackActivity : Microsoft.Maui.Authentication.WebAuthenticatorCallbackActivity
{
  public const string CallbackScheme = "fitedit";

  public WebAuthenticatorCallbackActivity()
  { 
  }
}
