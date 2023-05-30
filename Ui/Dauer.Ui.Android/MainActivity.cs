using Android.Content;
using Android.Content.PM;
using Avalonia.Android;
using Microsoft.Maui.ApplicationModel;

namespace Dauer.Ui.Android;

[Activity(Label = "FitEdit", Theme = "@style/MyTheme.NoActionBar", Icon = "@drawable/icon", LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
public class MainActivity : AvaloniaMainActivity
{
  protected override void OnCreate(Bundle? savedInstanceState)
  {
    base.OnCreate(savedInstanceState);
		Platform.Init(this, savedInstanceState);
  }

  protected override void OnResume()
  {
    base.OnResume();
    Platform.OnResume(this);
  }

  protected override void OnNewIntent(Intent? intent)
  {
    base.OnNewIntent(intent);
    Platform.OnNewIntent(intent);
  }

  protected override void OnDestroy()
  {
    base.OnDestroy();
  }

}
