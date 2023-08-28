using Dauer.Model.Data;
using Dauer.Model.Extensions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;

namespace Dauer.Ui.Infra.Supabase;

public class SessionPersistence : ReactiveObject, IGotrueSessionPersistence<Session>
{
  private readonly IDatabaseAdapter db_;
  [Reactive] public Dauer.Model.Authorization? Authorization { get; set; }

  public SessionPersistence(IDatabaseAdapter db)
  {
    db_ = db;
  }

  public void SaveSession(Session session)
  {
    Authorization = session.Map();
    db_.InsertAsync(Authorization).Await();
  }

  public void DestroySession()
  {
    Authorization = new Dauer.Model.Authorization { Id = "Dauer.Api" };
    db_.DeleteAsync(Authorization).Await();
  }

  public Session? LoadSession() => Authorization.Map();
}
