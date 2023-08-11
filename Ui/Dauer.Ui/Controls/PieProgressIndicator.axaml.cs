using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Path = Avalonia.Controls.Shapes.Path;

namespace Dauer.Ui.Controls;

public partial class PieProgressIndicator : UserControl
{
  public static readonly StyledProperty<double> ValueProperty = AvaloniaProperty.Register<PieProgressIndicator, double>(nameof(Value), 0);
  public static readonly StyledProperty<double> MaximumProperty = AvaloniaProperty.Register<PieProgressIndicator, double>(nameof(Maximum), 100);
  public static readonly StyledProperty<double> RadiusProperty = AvaloniaProperty.Register<PieProgressIndicator, double>(nameof(Radius), 10);

  private Ellipse? ellipse_;
  private Path? progressArc_;

  public PieProgressIndicator()
  {
    InitializeComponent();

    ellipse_ = this.FindControl<Ellipse>("PART_Ellipse");
    progressArc_ = this.FindControl<Path>("PART_ProgressArc");

    this.GetObservable(ValueProperty).Subscribe(_ => UpdateProgressArc());
    this.GetObservable(MaximumProperty).Subscribe(_ => UpdateProgressArc());
  }

  public double Value
  {
    get => GetValue(ValueProperty);
    set => SetValue(ValueProperty, value);
  }

  public double Maximum
  {
    get => GetValue(MaximumProperty);
    set => SetValue(MaximumProperty, value);
  }

  public double Radius
  {
    get => GetValue(RadiusProperty);
    set => SetValue(RadiusProperty, value);
  }

  public double Diameter => 2 * Radius;

  private void UpdateProgressArc()
  {
    if (progressArc_ == null || ellipse_ == null) { return; }

    double value = Math.Min(Math.Max(Value, 0), Maximum);
    double angle = 360 * value / Maximum;

    double radius = Radius;

    ellipse_.Width = Diameter;
    ellipse_.Height = Diameter;

    progressArc_.Data = value == Maximum
      ? new EllipseGeometry { Center = new Point(radius, radius), RadiusX = radius, RadiusY = radius }
      : CalculateArcPath(radius, radius, radius, angle);
  }

  private static Geometry CalculateArcPath(double centerX, double centerY, double radius, double angleInDegrees)
  {
    double angleInRadians = angleInDegrees * Math.PI / 180.0;
    double x = centerX + radius * Math.Sin(angleInRadians);
    double y = centerY - radius * Math.Cos(angleInRadians);

    var startPoint = new Point(centerX, centerY);
    var endPoint = new Point(x, y);

    var geometry = new StreamGeometry();

    using (var context = geometry.Open())
    {
      context.BeginFigure(startPoint, true);
      context.LineTo(new Point(centerX, centerY - radius));
      context.ArcTo(endPoint, new Size(radius, radius), 0, angleInDegrees > 180, SweepDirection.Clockwise);
    }

    return geometry;
  }
}
