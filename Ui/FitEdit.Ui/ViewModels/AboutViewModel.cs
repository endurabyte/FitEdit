namespace FitEdit.Ui.ViewModels;

public interface IAboutViewModel
{
  
}

public class DesignAboutViewModel : AboutViewModel
{
  
}

public class AboutViewModel : ViewModelBase, IAboutViewModel
{
  public string Text { get; set; } = "FitEdit is a training data manager.\nLearn more at https://www.fitedit.io/";
}