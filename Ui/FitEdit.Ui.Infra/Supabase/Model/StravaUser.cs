﻿using Postgrest.Attributes;
using Postgrest.Models;

namespace FitEdit.Ui.Infra.Supabase.Model;

public class StravaUser : BaseModel
{
  [PrimaryKey]
  public string Id { get; set; } = "";
  [Column(nameof(AccessToken))]
  public string? AccessToken { get; set; }
}