using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Dauer.Data.Fit
{
    public class Writer
    {
        public void Write(FitFile fitFile, string destination)
        {
            var encoder = new Encoder(ProtocolVersion.V20);
            using var dest = new FileStream(destination, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);

            encoder.Open(dest);

            foreach (var definition in fitFile.MessageDefinitions)
            {
                encoder.Write(definition);
            }

            foreach (var message in fitFile.Messages)
            {
                encoder.Write(message);
            }

            encoder.Close();
        }

        static void EncodeActivityFile()
        {
            var now = System.DateTime.Now;
            
            // Generate some FIT messages
            var fileIdMesg = new FileIdMesg(); // Every FIT file MUST contain a 'File ID' message as the first message
            var developerIdMesg = new DeveloperDataIdMesg();
            //var fieldDescMesg = new FieldDescriptionMesg();

            var records = new List<RecordMesg>();

            byte[] appId = {
                162, 110, 83, 88,
                117, 38, 69, 130,
                175, 126, 134, 6,
                136, 77, 150, 88
            };

            fileIdMesg.SetType(Dauer.Data.Fit.File.Activity);
            fileIdMesg.SetManufacturer(Manufacturer.Garmin);  // Types defined in the profile are available
            fileIdMesg.SetProduct(3113);
            fileIdMesg.SetSerialNumber(3998947172);
            fileIdMesg.SetTimeCreated(new DateTime(now));

            for (int i = 0; i < appId.Length; i++)
            {
                developerIdMesg.SetDeveloperId(i, 0xFF);
                developerIdMesg.SetApplicationId(i, appId[i]);
            }
            
            developerIdMesg.SetApplicationVersion(9);
            developerIdMesg.SetDeveloperDataIndex(0);

            // fieldDescMesg.SetDeveloperDataIndex(0);
            // fieldDescMesg.SetFieldDefinitionNumber(0);
            // fieldDescMesg.SetFitBaseTypeId(FitBaseType.Sint8);
            // fieldDescMesg.SetFieldName(0, "doughnuts_earned");
            // fieldDescMesg.SetUnits(0, "doughnuts");

            for (int i = 0; i < 1000; i++)
            {
                var newRecord = new RecordMesg();
                //var doughnutsEarnedField = new DeveloperField(fieldDescMesg, developerIdMesg);
                //newRecord.SetDeveloperField(doughnutsEarnedField);

                newRecord.SetTimestamp(new DateTime(now + TimeSpan.FromSeconds(i)));
                newRecord.SetHeartRate(140);
                newRecord.SetCadence(90);
                newRecord.SetDistance(i);
                newRecord.SetSpeed(1);
                //doughnutsEarnedField.SetValue(i + 1);

                records.Add(newRecord);
            }

            // Create file encode object
            Encoder encoderDemo = new Encoder(ProtocolVersion.V20);

            FileStream fitDest = new FileStream("ExampleActivity.fit", FileMode.Create, FileAccess.ReadWrite, FileShare.Read);

            // Write our header
            encoderDemo.Open(fitDest);

            // Encode each message, a definition message is automatically generated and output if necessary
            encoderDemo.Write(fileIdMesg);
            encoderDemo.Write(developerIdMesg);
            //encodeDemo.Write(fieldDescMesg);
            encoderDemo.Write(records);

            // Update header datasize and file CRC
            encoderDemo.Close();
            fitDest.Close();

            Console.WriteLine("Encoded FIT file ExampleActivity.fit");
        }

        /// <summary>
        /// Demonstrate the encoding of a 'Settings File' by writing a 'Settings File' containing a 'User Profile' Message.
        /// This example is simpler than the 'Monitoring File' example.
        /// </summary>
        static void EncodeSettingsFile()
        {
            // Generate some FIT messages
            FileIdMesg fileIdMesg = new FileIdMesg(); // Every FIT file MUST contain a 'File ID' message as the first message
            fileIdMesg.SetType(Dauer.Data.Fit.File.Settings);
            fileIdMesg.SetManufacturer(Manufacturer.Garmin);  // Types defined in the profile are available
            fileIdMesg.SetProduct(3113);
            fileIdMesg.SetSerialNumber(3998947172);

            UserProfileMesg myUserProfile = new UserProfileMesg();
            myUserProfile.SetGender(Gender.Female);
            float myWeight = 63.1F;
            myUserProfile.SetWeight(myWeight);
            myUserProfile.SetAge(99);
            myUserProfile.SetFriendlyName(Encoding.UTF8.GetBytes("TestUser"));

            FileStream fitDest = new FileStream("ExampleSettings.fit", FileMode.Create, FileAccess.ReadWrite, FileShare.Read);

            // Create file encode object
            Encoder encoderDemo = new Encoder(ProtocolVersion.V10);

            // Write our header
            encoderDemo.Open(fitDest);

            // Encode each message, a definition message is automatically generated and output if necessary
            encoderDemo.Write(fileIdMesg);
            encoderDemo.Write(myUserProfile);

            // Update header datasize and file CRC
            encoderDemo.Close();
            fitDest.Close();

            Console.WriteLine("Encoded FIT file ExampleSettings.fit");
            return;
        }

        /// <summary>
        /// Demonstrates encoding a 'MonitoringB File' of a made up device which counts steps and reports the battery status of the device.
        /// </summary>
        static void EncodeMonitoringFile()
        {
            System.DateTime systemStartTime = System.DateTime.Now;
            System.DateTime systemTimeNow = systemStartTime;

            FileStream fitDest = new FileStream("ExampleMonitoringFile.fit", FileMode.Create, FileAccess.ReadWrite, FileShare.Read);

            // Create file encode object
            Encoder encoder = new Encoder(ProtocolVersion.V10);

            // Write our header
            encoder.Open(fitDest);

            // Generate some FIT messages
            FileIdMesg fileIdMesg = new FileIdMesg(); // Every FIT file MUST contain a 'File ID' message as the first message
            fileIdMesg.SetSerialNumber(3998947172);
            fileIdMesg.SetTimeCreated(new Dauer.Data.Fit.DateTime(systemTimeNow));
            fileIdMesg.SetManufacturer(Manufacturer.Garmin);
            fileIdMesg.SetProduct(1752);
            fileIdMesg.SetNumber(0);
            fileIdMesg.SetType(Dauer.Data.Fit.File.MonitoringB); // See the 'FIT FIle Types Description' document for more information about this file type.
            encoder.Write(fileIdMesg); // Write the 'File ID Message'

            DeviceInfoMesg deviceInfoMesg = new DeviceInfoMesg();
            deviceInfoMesg.SetTimestamp(new Dauer.Data.Fit.DateTime(systemTimeNow));
            deviceInfoMesg.SetSerialNumber(5180063);
            deviceInfoMesg.SetManufacturer(Manufacturer.Garmin);
            deviceInfoMesg.SetBatteryStatus(Dauer.Data.Fit.BatteryStatus.Good);
            encoder.Write(deviceInfoMesg);

            MonitoringMesg monitoringMesg = new MonitoringMesg();

            // By default, each time a new message is written the Local Message Type 0 will be redefined to match the new message.
            // In this case,to avoid having a definition message each time there is a DeviceInfoMesg, we can manually set the Local Message Type of the MonitoringMessage to '1'.
            // By doing this we avoid an additional 7 definition messages in our FIT file.
            monitoringMesg.LocalNum = 1;

            // Simulate some data
            Random numberOfCycles = new Random(); // Fake a number of cycles
            for (int i = 0; i < 4; i++) // Each of these loops represent a quarter of a day
            {
                for (int j = 0; j < 6; j++) // Each of these loops represent 1 hour
                {
                    monitoringMesg.SetTimestamp(new Dauer.Data.Fit.DateTime(systemTimeNow));
                    monitoringMesg.SetActivityType(Dauer.Data.Fit.ActivityType.Walking); // Setting this to walking will cause Cycles to be interpretted as steps.
                    monitoringMesg.SetCycles(monitoringMesg.GetCycles() + numberOfCycles.Next(0, 1000)); // Cycles are accumulated (i.e. must be increasing)
                    encoder.Write(monitoringMesg);
                    systemTimeNow = systemTimeNow.AddHours(1); // Add an hour to our contrieved timestamp
                }

                deviceInfoMesg.SetTimestamp(new Dauer.Data.Fit.DateTime(systemTimeNow));
                deviceInfoMesg.SetBatteryStatus(Dauer.Data.Fit.BatteryStatus.Good); // Report the battery status every quarter day
                encoder.Write(deviceInfoMesg);
            }

            // Update header datasize and file CRC
            encoder.Close();
            fitDest.Close();

            Console.WriteLine("Encoded FIT file ExampleMonitoringFile.fit");
        }
    }
}