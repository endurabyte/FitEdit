using System;
using System.Linq;
using Dauer.Data.Fit;
using Newtonsoft.Json;

namespace Dauer.App
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 1)
            {
                DumpToJson(args[0]);
                return;
            }
            
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: dauer <input.fit> [<output.fit>]");
                return;
            }

            Copy(args[0], args[1]);
        }

        static void DumpToJson(string source)
        {
            var fitFile = new Reader().Read(source);
                
            Console.WriteLine(JsonConvert.SerializeObject(fitFile, Formatting.Indented));
        }
        
        static void Copy(string sourceFile, string destFile)
        {
            var fitFile = new Reader().Read(sourceFile);
            new Writer().Write(fitFile, destFile);
        }
        
        static void ApplyLaps(string sourceFile, string destFile)
        {
            var fitFile = new Reader().Read(sourceFile);
            
            var laps = fitFile.Messages.Where(message => message.Num == MesgNum.Lap);
            var records = fitFile.Messages.Where(message => message.Num == MesgNum.Record);

            new Writer().Write(fitFile, destFile);
        }
    }
}