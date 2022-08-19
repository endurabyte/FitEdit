namespace Dauer.Model
{
  public class GpsRunSample : RunSample
  {
    public double? Altitude { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public bool HasPosition => Latitude != default && Longitude != default;
  }
}