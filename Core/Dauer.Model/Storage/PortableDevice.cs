#nullable enable

namespace Dauer.Model.Storage;

/// <param name="Name"> Friendly name like "Garmin Forerunner 945" </param>
/// <param name="Id"> Serial number or other unique ID </param>
public record PortableDevice(string Name, string Id);
