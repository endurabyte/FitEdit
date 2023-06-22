﻿using Dauer.Ui.Mapsui;
using NetTopologySuite.Geometries;
using NetTopologySuite.Utilities;

namespace Dauer.Ui.Mapsui;

public static class GeometryShapeFactoryExtensions
{
  public static Polygon CreateCircle(this GeometricShapeFactory factory, Coordinate center, double radius)
  {
    factory.Base = center;
    factory.Size = radius * 2; //Diameter
    return factory.CreateCircle();
  }
}