﻿using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using Avalonia.Controls;
using FitEdit.Model;
using FitEdit.Services;
using FitEdit.Ui.Model;
using FitEdit.Ui.Views;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FitEdit.Ui.ViewModels;

public interface IMainViewModel
{
  IMapViewModel Map { get; }
  bool IsCompact { get; }
}

public class DesignMainViewModel : MainViewModel
{
  public DesignMainViewModel() : base(
    new NullWindowAdapter(),
    new DesignPlotViewModel(),
    new DesignLapViewModel(),
    new DesignRecordViewModel(),
    new DesignMapViewModel(),
    new DesignFileViewModel(),
    new DesignLogViewModel(),
    new DesignSettingsViewModel(),
    new DesignAboutViewModel(),
    new NullFitEditService(),
    new DesignNotifyViewModel(),
    isCompact: false
  )
  { 
  }
}

public class MainViewModel : ViewModelBase, IMainViewModel
{
  public IPlotViewModel Plot { get; }
  public ILapViewModel Laps { get; }
  public IRecordViewModel Records { get; }
  public IMapViewModel Map { get; }
  public IFileViewModel File { get; }
  public ILogViewModel LogVm { get; }
  public ISettingsViewModel Settings { get; }
  public IAboutViewModel About { get; }
  public IFitEditService FitEdit { get; set; }

  [Reactive] public ViewModelBase NotifyViewModel { get; set; }

  private string? AppTitle_ => $"FitEdit | Training Data Editor | Version {Version} {Titlebar.Instance.Message}";
  [Reactive] public string? AppTitle { get; set; }

  public string? Version { get; set; }

  [Reactive] public int SelectedTabIndex { get; set; }
  [Reactive] public bool IsCompact { get; set; }

  private readonly IWindowAdapter window_;

  public MainViewModel(
    IWindowAdapter window,
    IPlotViewModel plot,
    ILapViewModel laps,
    IRecordViewModel records,
    IMapViewModel map,
    IFileViewModel file,
    ILogViewModel logVm,
    ISettingsViewModel settings,
    IAboutViewModel about,
    IFitEditService fitEdit,
    NotifyViewModel notifyViewModel,
    bool isCompact
  )
  {
    window_ = window;
    Plot = plot;
    Laps = laps;
    Records = records;
    Map = map;
    File = file;
    LogVm = logVm;
    Settings = settings;
    About = about;
    FitEdit = fitEdit;
    NotifyViewModel = notifyViewModel;
    IsCompact = isCompact;

    GetVersion();

    Titlebar.Instance.ObservableForProperty(x => x.Message).Subscribe(_ => AppTitle = AppTitle_);
    window_.Resized.Subscribe(tup =>
    {
      double width = tup.Item1;
      double height = tup.Item2;
      Log.Info($"Window resized to {width} {height}");

      // If we were told at construction to be compact, always remain compact.
      // We're probably on a mobile device.
      if (isCompact) { return; } 

      IsCompact = width < height;
    });
  }

  /// <summary>
  /// Get the assembly version that is displayed in the titlebar and update the titlebar with it
  /// </summary>
  private void GetVersion()
  {
    var assembly = Assembly.GetAssembly(typeof(App));
    var attr = assembly?.GetCustomAttribute(typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute;
    Version = attr?.InformationalVersion.Split("+")[0] ?? "Unknown Version";
    AppTitle = AppTitle_;
  }
  
  public void ShowAbout()
  {
    var dialog = new AboutWindow
    {
      DataContext = About
    };
    if (window_.Main is not Window window) { return; }
    
    dialog.ShowDialog(window);
  }
}
