﻿using Android.Content;
using Android.Content.PM;
using Android.Views;
using Avalonia;
using Avalonia.Android;
using Avalonia.ReactiveUI;
using FitEdit.Ui.Infra;
using Microsoft.Maui.ApplicationModel;

namespace FitEdit.Ui.Android;

[Activity(Label = "FitEdit", Theme = "@style/MyTheme.NoActionBar", Icon = "@mipmap/ic_launcher", MainLauncher = true, 
  WindowSoftInputMode = SoftInput.AdjustResize, 
  ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
  protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
  {
    App.Root = ConfigurationRoot.Bootstrap(new AndroidCompositionRoot());

    return base.CustomizeAppBuilder(builder)
          .UseReactiveUI();
  }

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
