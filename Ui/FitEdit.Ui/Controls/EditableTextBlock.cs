using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using ReactiveUI;

namespace FitEdit.Ui.Controls;

public class EditableTextBlock : UserControl
{
  public static readonly DirectProperty<EditableTextBlock, string?> TextProperty =
      AvaloniaProperty.RegisterDirect<EditableTextBlock, string?>(nameof(Text), o => o.Text, (o, v) => o.Text = v);

  public static readonly DirectProperty<EditableTextBlock, string?> DisplayTextProperty =
      AvaloniaProperty.RegisterDirect<EditableTextBlock, string?>(nameof(DisplayText), o => o.DisplayText, (o, v) => o.DisplayText = v);

  public string? Text
  {
    get { return text_; }
    set { SetAndRaise(TextProperty, ref text_, value); }
  }

  public string? DisplayText
  {
    get { return displayText_; }
    set { SetAndRaise(DisplayTextProperty, ref displayText_, value); }
  }

  private string? text_ = "";
  private string? displayText_ = "";
  private bool isEditing_ = false;

  private readonly TextBlock textBlock_;
  private readonly TextBox textBox_;

  public EditableTextBlock()
  {
    textBlock_ = new TextBlock();
    textBox_ = new TextBox();

    textBlock_.TextWrapping = TextWrapping.Wrap;
    textBox_.TextWrapping = TextWrapping.Wrap;

    // Ensure there is something to click on even if text is empty
    textBox_.MinWidth = 100;

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
      }
    };

    // Binding
    textBlock_.Bind(TextBlock.TextProperty, this.WhenAnyValue(x => x.DisplayText));
    textBox_.Bind(TextBox.TextProperty, this.WhenAnyValue(x => x.Text), BindingPriority.LocalValue);
  }

  protected override void OnInitialized()
  {
    base.OnInitialized();

    if (string.IsNullOrWhiteSpace(DisplayText))
    {
      DisplayText = "(None)";
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
      try
      {
        Text = string.IsNullOrWhiteSpace(textBox_.Text) ? "(None)" : textBox_.Text;
      }
      catch (Exception)
      {
        // e.g. user entered ".5" instead of ".5km"
      }
      Content = textBlock_;
    }
  }
}
