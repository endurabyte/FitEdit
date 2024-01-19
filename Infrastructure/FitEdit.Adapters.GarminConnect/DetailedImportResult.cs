using System.Text.Json.Serialization;

namespace FitEdit.Adapters.GarminConnect;

public class DetailedImportResult
{
  [JsonPropertyName("uploadId")]
  // In the response JSON, this is a number literal e.g. "uploadId": 1234567 when the uploaded succeded,
  // and it is an empty string "uploadId": "" when it failed. Gah!
  public long UploadId { get; set; } 

  [JsonPropertyName("uploadUuid")]
  public UploadUuid UploadUuid { get; set; }

  [JsonPropertyName("owner")]
  public long Owner { get; set; }

  [JsonPropertyName("fileSize")]
  public long FileSize { get; set; }

  [JsonPropertyName("processingTime")]
  public int ProcessingTime { get; set; }

  [JsonPropertyName("creationDate")]
  public string CreationDate { get; set; } 

  [JsonPropertyName("ipAddress")]
  public string IpAddress { get; set; }

  [JsonPropertyName("fileName")]
  public string FileName { get; set; }

  [JsonPropertyName("report")]
  public object Report { get; set; }

  [JsonPropertyName("successes")]
  public List<Success> Successes { get; set; }

  [JsonPropertyName("failures")]
  public List<Failure> Failures { get; set; }
}
