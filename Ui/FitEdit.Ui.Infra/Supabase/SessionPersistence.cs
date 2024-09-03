using FitEdit.Model.Data;
using FitEdit.Model.Extensions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;

namespace FitEdit.Ui.Infra.Supabase;

public class SessionPersistence : ReactiveObject, IGotrueSessionPersistence<Session>
{
  private readonly IDatabaseAdapter db_;
  [Reactive] public FitEdit.Model.Authorization? Authorization { get; set; }

  public SessionPersistence(IDatabaseAdapter db)
  {
    db_ = db;
  }

  public void SaveSession(Session? session)
  {
    if (session is null) { return; }

    Authorization = session.Map()!;
    db_.InsertAsync(Authorization).Await();
  }

  public void DestroySession()
  {
    Authorization = new FitEdit.Model.Authorization { Id = "FitEdit.Api" };
    db_.DeleteAsync(Authorization).Await();
  }

  public Session? LoadSession() => Authorization.Map();
}
