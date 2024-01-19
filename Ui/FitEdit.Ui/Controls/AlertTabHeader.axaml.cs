using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace FitEdit.Ui.Controls;

public partial class AlertTabHeader : UserControl
{
  public static readonly StyledProperty<SolidColorBrush> AlertDotColorProperty = AvaloniaProperty.Register<AlertTabHeader, SolidColorBrush>(nameof(AlertDotColor), new SolidColorBrush(FitColor.RedCrayon));
  public static readonly DirectProperty<AlertTabHeader, string> TextProperty = 
    AvaloniaProperty.RegisterDirect<AlertTabHeader, string>(nameof(Text), o => o.Text, (o, v) => o.Text = v);
  public static readonly DirectProperty<AlertTabHeader, bool> IsAlertOnProperty =
      AvaloniaProperty.RegisterDirect<AlertTabHeader, bool>(nameof(IsAlertOn), o => o.IsAlertOn, (o, v) => o.IsAlertOn = v);

  public SolidColorBrush AlertDotColor
  {
    get => GetValue(AlertDotColorProperty);
    set => SetValue(AlertDotColorProperty, value);
  }

  private string text_ = "HeaderName";
  public string Text
  {
    get => text_;
    set { SetAndRaise(TextProperty, ref text_, value); }
  }

  private bool isAlertOn_ = true;
  public bool IsAlertOn
  {
    get { return isAlertOn_; }
    set { SetAndRaise(IsAlertOnProperty, ref isAlertOn_, value); }
  }

  public AlertTabHeader()
  {
    InitializeComponent();
  }
}