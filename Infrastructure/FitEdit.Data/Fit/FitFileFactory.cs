﻿using FitEdit.Data.Fit;

namespace FitEdit.Ui.ViewModels;

public class FitFileFactory
{
  public FitFile CreateFake()
  {
    var fit = new FitFile();
    var session = new Dynastream.Fit.SessionMesg
    {
      LocalNum = 12
    };

    var start = new Dynastream.Fit.DateTime(DateTime.Now - TimeSpan.FromMinutes(6));
    session.SetTimestamp(start);
    session.SetStartTime(start);

    fit.MessagesByDefinition[Dynastream.Fit.MesgNum.Session].Add(session);

    var lap1 = new Dynastream.Fit.LapMesg();
    var lap2 = new Dynastream.Fit.LapMesg();
    var lap3 = new Dynastream.Fit.LapMesg();

    lap1.SetStartTime(start);
    lap2.SetStartTime(new Dynastream.Fit.DateTime(start.GetDateTime() + TimeSpan.FromMinutes(2)));
    lap3.SetStartTime(new Dynastream.Fit.DateTime(start.GetDateTime() + TimeSpan.FromMinutes(5)));

    fit.MessagesByDefinition[Dynastream.Fit.MesgNum.Lap].Add(lap1);
    fit.MessagesByDefinition[Dynastream.Fit.MesgNum.Lap].Add(lap2);
    fit.MessagesByDefinition[Dynastream.Fit.MesgNum.Lap].Add(lap3);

    foreach (int i in Enumerable.Range(0, 2*60))
    {
      var record = new Dynastream.Fit.RecordMesg();
      record.SetEnhancedSpeed(3.7f);
      record.SetTimestamp(new Dynastream.Fit.DateTime(start.GetDateTime() + TimeSpan.FromSeconds(i)));
      fit.MessagesByDefinition[Dynastream.Fit.MesgNum.Record].Add(record);
    }

    foreach (int i in Enumerable.Range(0, 3*60))
    {
      var record = new Dynastream.Fit.RecordMesg();
      record.SetEnhancedSpeed(6.7f);
      record.SetTimestamp(new Dynastream.Fit.DateTime(start.GetDateTime() + TimeSpan.FromMinutes(2) + TimeSpan.FromSeconds(i)));
      fit.MessagesByDefinition[Dynastream.Fit.MesgNum.Record].Add(record);
    }

    foreach (int i in Enumerable.Range(0, 1*60))
    {
      var record = new Dynastream.Fit.RecordMesg();
      record.SetEnhancedSpeed(2.5f);
      record.SetTimestamp(new Dynastream.Fit.DateTime(start.GetDateTime() + TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(i)));
      fit.MessagesByDefinition[Dynastream.Fit.MesgNum.Record].Add(record);
    }

    return fit;
  }
}
