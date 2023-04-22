using Dauer.Ui.Models;
using System.Collections.Generic;

namespace Dauer.Ui.Services;

public class Database
{
  public IEnumerable<InventoryItem> GetItems() => new[]
  {
      new InventoryItem { Description = "Computer" },
      new InventoryItem { Description = "Car" },
      new InventoryItem { Description = "Diamond ring", IsChecked = true },
  };
}
