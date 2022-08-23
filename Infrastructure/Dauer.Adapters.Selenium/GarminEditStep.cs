using Dauer.Model;
using Dauer.Model.Web;
using FundLog.Model.Extensions;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;

namespace Dauer.Adapters.Selenium;

public class GarminEditStep : Step, IStep
{
  public string ActivityId { get; set; }
  public string Title { get; set; }
  public string Note { get; set; }

  public GarminEditStep(IWebDriver driver) : base(driver) => Name = "Garmin Activity Edit";

  public async Task<bool> Run()
  {
    driver_.Url = $"https://connect.garmin.com/modern/activity/{ActivityId}";
    bool ok = true;

    if (!string.IsNullOrWhiteSpace(Title))
    {
      ok &= await EditTitle(Title).AnyContext();
    }

    if (!string.IsNullOrWhiteSpace(Note))
    {
      ok &= await EditNote(Note).AnyContext();
    }

    return ok;
  }

  private async Task<bool> EditTitle(string title)
  {
    // Click the pencil icon
    if (!driver_.WaitForClickable(By.CssSelector(".modal-trigger > .icon-pencil"), out IWebElement editButton))
    {
      return false;
    }

    // Elements don't become interactable until the page sees any mouse movement
    new Actions(driver_).MoveToElement(editButton).Perform();
    editButton.Click();

    // Clear and type new title
    if (!driver_.WaitForElement(By.CssSelector(".inline-edit-editable > .page-title-overflow"), out IWebElement titleEditor))
    {
      return false;
    }

    titleEditor.Clear();
    titleEditor.SendKeys(title);
    titleEditor.SendKeys(Keys.Enter);

    await Task.CompletedTask;
    return true;
  }

  private async Task<bool> EditNote(string note)
  {
    // If there is an existing note, the edit link is visible. We must first click it.
    if (driver_.WaitForClickable(By.CssSelector(".edit-note-button"), out IWebElement editButton))
    {
      // Elements don't become interactable until the page sees any mouse movement
      new Actions(driver_).MoveToElement(editButton).Perform();
      editButton.Click();
    }

    // Find the note text area. 
    if (!driver_.WaitForElement(By.CssSelector(".noteTextarea"), out IWebElement noteArea))
    {
      Log.Error("Could not find note text area");
      return false;
    }

    // Elements don't become interactable until the page sees any mouse movement
    new Actions(driver_).MoveToElement(noteArea).Perform();
    noteArea.Click();
    noteArea.Clear();
    noteArea.SendKeys(note);

    if (!driver_.WaitForElement(By.CssSelector(".add-note-button"), out IWebElement saveButton))
    {
      Log.Error("Could not find save note button");
      return false;
    }

    saveButton.Click();

    await Task.CompletedTask;
    return true;
  }
}