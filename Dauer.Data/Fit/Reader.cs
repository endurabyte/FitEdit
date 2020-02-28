using System;
using System.Collections.Generic;
using System.IO;

namespace Dauer.Data.Fit
{
    public class Reader
    {
        public FitFile Read(string source)
        {
            try
            {
                // Attempt to open .FIT file
                using var fitSource = new FileStream(source, FileMode.Open);

                var decoder = new Decoder();
                var mesgBroadcaster = new MesgBroadcaster();

                // Connect the Broadcaster to our event (message) source (in this case the Decoder)
                decoder.MesgEvent += mesgBroadcaster.HandleMessage;
                decoder.MesgDefinitionEvent += mesgBroadcaster.HandleMesgDefinition;
                //decodeDemo.DeveloperFieldDescriptionEvent += OnDeveloperFieldDescriptionEvent;

                var fitFile = new FitFile();
                mesgBroadcaster.MesgEvent += (o, s) => fitFile.Messages.Add(s.mesg);
                mesgBroadcaster.MesgDefinitionEvent += (o, s) => fitFile.MessageDefinitions.Add(s.mesgDef);

                bool ok = decoder.IsFIT(fitSource);
                ok &= decoder.CheckIntegrity(fitSource);

                // Process the file
                if (ok)
                {
                    decoder.Read(fitSource);
                }
                else
                {
                    Console.WriteLine("Integrity Check Failed {0}", source);
                    if (decoder.InvalidDataSize)
                    {
                        Console.WriteLine("Invalid Size Detected, Attempting to decode...");
                        decoder.Read(fitSource);
                    }
                    else
                    {
                        Console.WriteLine("Attempting to decode by skipping the header...");
                        decoder.Read(fitSource, DecodeMode.InvalidHeader);
                    }
                }

                return fitFile;
            }
            catch (FitException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return null;
        }

        private static void OnDeveloperFieldDescriptionEvent(object sender, DeveloperFieldDescriptionEventArgs args)
        {
            Console.WriteLine("New Developer Field Description");
            Console.WriteLine("   App Id: {0}", args.Description.ApplicationId);
            Console.WriteLine("   App Version: {0}", args.Description.ApplicationVersion);
            Console.WriteLine("   Field Number: {0}", args.Description.FieldDefinitionNumber);
        }

        #region Message Handlers
        // Client implements their handlers of interest and subscribes to MesgBroadcaster events
        static void OnMesgDefn(object sender, MesgDefinitionEventArgs e)
        {
            Console.WriteLine("OnMesgDef: Received Defn for local message #{0}, global num {1}", e.mesgDef.LocalMesgNum, e.mesgDef.GlobalMesgNum);
            Console.WriteLine("\tIt has {0} fields {1} developer fields and is {2} bytes long",
                e.mesgDef.NumFields,
                e.mesgDef.NumDevFields,
                e.mesgDef.GetMesgSize());
        }
        
        static void OnMesg(object sender, MesgEventArgs e)
        {
            Console.WriteLine("OnMesg: Received Mesg with global ID#{0}, its name is {1}", e.mesg.Num, e.mesg.Name);

            int i = 0;
            foreach (Field field in e.mesg.Fields)
            {
                for (int j = 0; j < field.GetNumValues(); j++)
                {
                    Console.WriteLine("\tField{0} Index{1} (\"{2}\" Field#{4}) Value: {3} (raw value {5})",
                        i,
                        j,
                        field.GetName(),
                        field.GetValue(j),
                        field.Num,
                        field.GetRawValue(j));
                }

                i++;
            }

            foreach (var devField in e.mesg.DeveloperFields.Values)
            {
                for (int j = 0; j < devField.GetNumValues(); j++)
                {
                    Console.WriteLine("\tDeveloper{0} Field#{1} Index{2} (\"{3}\") Value: {4} (raw value {5})",
                        devField.DeveloperDataIndex,
                        devField.Num,
                        j,
                        devField.Name,
                        devField.GetValue(j),
                        devField.GetRawValue(j));
                }
            }
        }

        static void OnFileIDMesg(object sender, MesgEventArgs e)
        {
            Console.WriteLine("FileIdHandler: Received {1} Mesg with global ID#{0}", e.mesg.Num, e.mesg.Name);
            FileIdMesg myFileId = (FileIdMesg)e.mesg;
            try
            {
                Console.WriteLine("\tType: {0}", myFileId.GetType());
                Console.WriteLine("\tManufacturer: {0}", myFileId.GetManufacturer());
                Console.WriteLine("\tProduct: {0}", myFileId.GetProduct());
                Console.WriteLine("\tSerialNumber {0}", myFileId.GetSerialNumber());
                Console.WriteLine("\tNumber {0}", myFileId.GetNumber());
                Console.WriteLine("\tTimeCreated {0}", myFileId.GetTimeCreated());

                //Make sure properties with sub properties arent null before trying to create objects based on them
                if (myFileId.GetTimeCreated() != null)
                {
                    Dauer.Data.Fit.DateTime dtTime = new Dauer.Data.Fit.DateTime(myFileId.GetTimeCreated().GetTimeStamp());
                }
            }
            catch (FitException exception)
            {
                Console.WriteLine("\tOnFileIDMesg Error {0}", exception.Message);
                Console.WriteLine("\t{0}", exception.InnerException);
            }
        }

        static void OnUserProfileMesg(object sender, MesgEventArgs e)
        {
            Console.WriteLine("UserProfileHandler: Received {1} Mesg, it has global ID#{0}", e.mesg.Num, e.mesg.Name);
            UserProfileMesg myUserProfile = (UserProfileMesg)e.mesg;
            string friendlyName;
            try
            {
                try
                {
                    friendlyName = myUserProfile.GetFriendlyNameAsString();
                }
                catch (ArgumentNullException)
                {
                    //There is no FriendlyName property
                    friendlyName = "";
                }
                Console.WriteLine("\tFriendlyName \"{0}\"", friendlyName);
                Console.WriteLine("\tGender {0}", myUserProfile.GetGender().ToString());
                Console.WriteLine("\tAge {0}", myUserProfile.GetAge());
                Console.WriteLine("\tWeight  {0}", myUserProfile.GetWeight());
            }
            catch (FitException exception)
            {
                Console.WriteLine("\tOnUserProfileMesg Error {0}", exception.Message);
                Console.WriteLine("\t{0}", exception.InnerException);
            }
        }

        static void OnDeviceInfoMessage(object sender, MesgEventArgs e)
        {
            Console.WriteLine("DeviceInfoHandler: Received {1} Mesg, it has global ID#{0}", e.mesg.Num, e.mesg.Name);
            DeviceInfoMesg myDeviceInfoMessage = (DeviceInfoMesg)e.mesg;
            try
            {
                Console.WriteLine("\tTimestamp  {0}", myDeviceInfoMessage.GetTimestamp());
                Console.WriteLine("\tBattery Status{0}", myDeviceInfoMessage.GetBatteryStatus());
            }
            catch (FitException exception)
            {
                Console.WriteLine("\tOnDeviceInfoMesg Error {0}", exception.Message);
                Console.WriteLine("\t{0}", exception.InnerException);
            }
        }

        static void OnMonitoringMessage(object sender, MesgEventArgs e)
        {
            Console.WriteLine("MonitoringHandler: Received {1} Mesg, it has global ID#{0}", e.mesg.Num, e.mesg.Name);
            MonitoringMesg myMonitoringMessage = (MonitoringMesg)e.mesg;
            try
            {
                Console.WriteLine("\tTimestamp  {0}", myMonitoringMessage.GetTimestamp());
                Console.WriteLine("\tActivityType {0}", myMonitoringMessage.GetActivityType());
                switch (myMonitoringMessage.GetActivityType()) // Cycles is a dynamic field
                {
                    case ActivityType.Walking:
                    case ActivityType.Running:
                        Console.WriteLine("\tSteps {0}", myMonitoringMessage.GetSteps());
                        break;
                    case ActivityType.Cycling:
                    case ActivityType.Swimming:
                        Console.WriteLine("\tStrokes {0}", myMonitoringMessage.GetStrokes());
                        break;
                    default:
                        Console.WriteLine("\tCycles {0}", myMonitoringMessage.GetCycles());
                        break;
                }
            }
            catch (FitException exception)
            {
                Console.WriteLine("\tOnDeviceInfoMesg Error {0}", exception.Message);
                Console.WriteLine("\t{0}", exception.InnerException);
            }
        }

        private static void OnRecordMessage(object sender, MesgEventArgs e)
        {
            Console.WriteLine("Record Handler: Received {0} Mesg, it has global ID#{1}",
                e.mesg.Num,
                e.mesg.Name);

            var recordMessage = (RecordMesg)e.mesg;

            WriteFieldWithOverrides(recordMessage, RecordMesg.FieldDefNum.HeartRate);
            WriteFieldWithOverrides(recordMessage, RecordMesg.FieldDefNum.Cadence);
            WriteFieldWithOverrides(recordMessage, RecordMesg.FieldDefNum.Speed);
            WriteFieldWithOverrides(recordMessage, RecordMesg.FieldDefNum.Distance);

            WriteDeveloperFields(recordMessage);
        }

        private static void WriteDeveloperFields(Mesg mesg)
        {
            foreach (var devField in mesg.DeveloperFields.Values)
            {
                if (devField.GetNumValues() <= 0)
                {
                    continue;
                }

                if (devField.IsDefined)
                {
                    Console.Write("\t{0}", devField.Name);

                    if (devField.Units != null)
                    {
                        Console.Write(" [{0}]", devField.Units);
                    }
                    Console.Write(": ");
                }
                else
                {
                    Console.Write("\tUndefined Field: ");
                }

                Console.Write("{0}", devField.GetValue(0));
                for (int i = 1; i < devField.GetNumValues(); i++)
                {
                    Console.Write(",{0}", devField.GetValue(i));
                }

                Console.WriteLine();
            }
        }

        private static void WriteFieldWithOverrides(Mesg mesg, byte fieldNumber)
        {
            Field profileField = Profile.GetField(mesg.Num, fieldNumber);
            bool nameWritten = false;

            if (null == profileField)
            {
                return;
            }

            IEnumerable<FieldBase> fields = mesg.GetOverrideField(fieldNumber);

            foreach (FieldBase field in fields)
            {
                if (!nameWritten)
                {
                    Console.WriteLine("   {0}", profileField.GetName());
                    nameWritten = true;
                }

                if (field is Field)
                {
                    Console.WriteLine("      native: {0}", field.GetValue());
                }
                else
                {
                    Console.WriteLine("      override: {0}", field.GetValue());
                }
            }
        }

        #endregion
    }
}