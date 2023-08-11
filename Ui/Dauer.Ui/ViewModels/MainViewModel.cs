using System.Diagnostics;
using System.Net.Http.Json;
using System.Reactive.Linq;
using System.Text.Json;
using System.Web;
using Dauer.Model;
using Dauer.Ui.Infra;
using Dauer.Ui.Infra.Adapters.Windowing;
using Dauer.Ui.Supabase;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Dauer.Ui.ViewModels;

public interface IMainViewModel
{
  IMapViewModel Map { get; }
}

public class DesignMainViewModel : MainViewModel
{
  public DesignMainViewModel() : base(
    new FileService(),
    new NullWindowAdapter(),
    new DesignPlotViewModel(),
    new DesignLapViewModel(),
    new DesignRecordViewModel(),
    new DesignMapViewModel(),
    new DesignFileViewModel(),
    new DesignLogViewModel(),
    new NullWebAuthenticator(),
    new NullSupabaseAdapter()
  )
  { 
  }
}

public class MainViewModel : ViewModelBase, IMainViewModel
{
  public IPlotViewModel Plot { get; }
  public ILapViewModel Laps { get; }
  public IRecordViewModel Records { get; }
  public IMapViewModel Map { get; }
  public IFileViewModel File { get; }
  public ILogViewModel LogVm { get; }
  public IWebAuthenticator Authenticator { get; }

  [Reactive] public int SelectedTabIndex { get; set; }
  [Reactive] public bool IsAuthenticatedWithGarmin { get; private set; }

  private readonly IWindowAdapter window_;
  private readonly ISupabaseAdapter supa_;
  private readonly IFileService fileService_;

  public MainViewModel(
    IFileService fileService,
    IWindowAdapter window,
    IPlotViewModel plot,
    ILapViewModel laps,
    IRecordViewModel records,
    IMapViewModel map,
    IFileViewModel file,
    ILogViewModel log,
    IWebAuthenticator authenticator,
    ISupabaseAdapter supa
  )
  {
    window_ = window;
    fileService_ = fileService;
    Plot = plot;
    Laps = laps;
    Records = records;
    Map = map;
    File = file;
    LogVm = log;
    Authenticator = authenticator;
    supa_ = supa;

    window_.Resized.Subscribe(tup =>
    {
      Log.Info($"Window resized to {tup.Item1} {tup.Item2}");
    });

    supa_.ObservableForProperty(x => x.IsAuthenticatedWithGarmin).Subscribe(_ =>
    {
      IsAuthenticatedWithGarmin = supa_.IsAuthenticatedWithGarmin;
    });
  }

  public void HandleLoginClicked()
  {
    Log.Info($"{nameof(HandleLoginClicked)}");
    Log.Info($"Starting {Authenticator.GetType()}.{nameof(IWebAuthenticator.AuthenticateAsync)}");

    _ = Task.Run(() => Authenticator.AuthenticateAsync());
  }

  public void HandleLogoutClicked()
  {
    Log.Info($"{nameof(HandleLogoutClicked)}");
    Log.Info($"Starting {Authenticator.GetType()}.{nameof(IWebAuthenticator.LogoutAsync)}");

    _ = Task.Run(() => Authenticator.LogoutAsync());
  }

  public void HandleGarminAuthorizeClicked()
  {
    Log.Info($"{nameof(HandleGarminAuthorizeClicked)}");

    _ = Task.Run(AuthorizeGarminAsync);
  }

  public void HandleGarminDeauthorizeClicked()
  {
    Log.Info($"{nameof(HandleGarminDeauthorizeClicked)}");

    _ = Task.Run(DeauthorizeGarminAsync);
  }

  private async Task AuthorizeGarminAsync()
  {
    var client = new HttpClient() { BaseAddress = new Uri("http://api.fitedit.io") };
    var responseMsg = await client.GetAsync($"garmin/oauth/init?username={HttpUtility.UrlEncode(Authenticator.Username)}");

    if (!responseMsg.IsSuccessStatusCode)
    {
      return;
    }

    try
    {
      var content = await responseMsg.Content.ReadAsStringAsync();
      var token = JsonSerializer.Deserialize<OauthToken>(content);

      if (token?.Token == null) { return; }

      // Open browser to Garmin auth page
      string url = $"https://connect.garmin.com/oauthConfirm" +
        $"?oauth_token={token?.Token}" +
        $"&oauth_callback={HttpUtility.UrlEncode($"http://api.fitedit.io/garmin/oauth/complete?username={HttpUtility.UrlEncode(Authenticator.Username)}")}" +
        $"";

      Browser.Open(url);
    }
    catch (JsonException e)
    {
      Log.Error($"Error authorizing Garmin: {e}"); 
    }
    catch (Exception e)
    {
      Log.Error($"Error authorizing Garmin: {e}"); 
    }
  }

  private async Task DeauthorizeGarminAsync()
  {
    // TODO send request to API
    //var client = new HttpClient() { BaseAddress = new Uri("http://api.fitedit.io") };
    //var responseMsg = await client.GetAsync($"garmin/oauth/deregister?username={HttpUtility.UrlEncode(Authenticator.Username)}");
    //if (!responseMsg.IsSuccessStatusCode)
    //{
    //  return;
    //}

    await supa_.LogoutGarminAsync();
  }
}
