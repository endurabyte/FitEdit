using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using ReactiveUI;

namespace Dauer.Ui.Controls;

public class EditableTextBlock : UserControl, ICommandSource
{
  public static readonly DirectProperty<EditableTextBlock, string> TextProperty =
      AvaloniaProperty.RegisterDirect<EditableTextBlock, string>(
          nameof(Text),
          o => o.Text,
          (o, v) => o.Text = v);

  public static readonly StyledProperty<ICommand?> CommandProperty =
      AvaloniaProperty.Register<EditableTextBlock, ICommand?>(nameof(Command), enableDataValidation: true);

  public static readonly StyledProperty<object?> CommandParameterProperty =
      AvaloniaProperty.Register<EditableTextBlock, object?>(nameof(CommandParameter));

  public ICommand? Command
  {
    get => GetValue(CommandProperty);
    set => SetValue(CommandProperty, value);
  }

  public object? CommandParameter
  {
    get => GetValue(CommandParameterProperty);
    set => SetValue(CommandParameterProperty, value);
  }

  public string Text
  {
    get { return text_; }
    set { SetAndRaise(TextProperty, ref text_, value); }
  }

  private string text_;
  private bool commandCanExecute_ = true;
  private bool isEditing_ = false;

  private readonly TextBlock textBlock_;
  private readonly TextBox textBox_;

  public EditableTextBlock()
  {
    textBlock_ = new TextBlock();
    textBox_ = new TextBox();
    text_ = "";

    // Set underline text decoration when mouse over
    textBlock_.TextDecorations = new TextDecorationCollection { new TextDecoration { Location = TextDecorationLocation.Underline } };

    Content = textBlock_;
    textBlock_.PointerPressed += (sender, e) => StartEditing();
    textBox_.LostFocus += (sender, e) => StopEditing();
    textBox_.KeyUp += (sender, e) =>
    {
      if (e.Key == Key.Enter)
      {
        StopEditing();

        if (Command?.CanExecute(CommandParameter) == true)
        {
          Command?.Execute(CommandParameter);
        }
      }
    };

    // Binding
    textBlock_.Bind(TextBlock.TextProperty, this.WhenAnyValue(x => x.Text));
    textBox_.Bind(TextBox.TextProperty, this.WhenAnyValue(x => x.Text), BindingPriority.LocalValue);
  }

  void ICommandSource.CanExecuteChanged(object sender, EventArgs e) => CanExecuteChanged(sender, e);

  protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
  {
    base.OnPropertyChanged(change);

    if (change.Property == CommandProperty)
    {
      if (((ILogical)this).IsAttachedToLogicalTree)
      {
        var (oldValue, newValue) = change.GetOldAndNewValue<ICommand?>();
        if (oldValue is ICommand oldCommand)
        {
          oldCommand.CanExecuteChanged -= CanExecuteChanged;
        }

        if (newValue is ICommand newCommand)
        {
          newCommand.CanExecuteChanged += CanExecuteChanged;
        }
      }

      CanExecuteChanged(this, EventArgs.Empty);
    }
    else if (change.Property == CommandParameterProperty)
    {
      CanExecuteChanged(this, EventArgs.Empty);
    }
  }

  /// <summary>
  /// Called when the <see cref="ICommand.CanExecuteChanged"/> event fires.
  /// </summary>
  /// <param name="sender">The event sender.</param>
  /// <param name="e">The event args.</param>
  private void CanExecuteChanged(object? sender, EventArgs e)
  {
    var canExecute = Command == null || Command.CanExecute(CommandParameter);

    if (canExecute != commandCanExecute_)
    {
      commandCanExecute_ = canExecute;
      UpdateIsEffectivelyEnabled();
    }
  }

  private void StartEditing()
  {
    if (!isEditing_)
    {
      isEditing_ = true;
      Content = textBox_;
      textBox_.Focus();
    }
  }

  private void StopEditing()
  {
    if (isEditing_)
    {
      isEditing_ = false;
      Text = textBox_.Text ?? "";
      Content = textBlock_;
    }
  }
}
