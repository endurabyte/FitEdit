﻿using System.Net;
using System.Net.Sockets;
using FitEdit.Model.Abstractions;

namespace FitEdit.Services;

public class TcpService : ITcpService
{
  public int GetRandomUnusedPort()
  {
    var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    var port = ((IPEndPoint)listener.LocalEndpoint).Port;
    listener.Stop();
    return port;
  }
}
