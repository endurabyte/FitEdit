﻿using Dauer.Data.Fit;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Dauer.Ui.ViewModels;

public interface IFileService
{
  FitFile? FitFile { get; set; }
  int SelectedIndex { get; set; }
}

/// <summary>
/// Encapsulates the shared state of the loaded file and which record is currently visualized / editable.
/// </summary>
public class FileService : ReactiveObject, IFileService
{
  [Reactive] public FitFile? FitFile { get; set; }
  [Reactive] public int SelectedIndex { get; set; }
}
