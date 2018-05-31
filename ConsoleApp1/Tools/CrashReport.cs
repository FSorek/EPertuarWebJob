using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ConsoleApp1.Tools
{
     class CrashReport
    {
        private static string reportPath = Directory.GetCurrentDirectory();
        private static string fileName = "CrashLog.txt";
        private static int errorIndex = 0;
        private static CrashReport _instance;
        public static CrashReport Instance { get
            {
                if (_instance == null)
                    _instance = new CrashReport();
                return _instance;
            }
        }
        public  void Report(string message)
        {
            if (!File.Exists(reportPath + @"/" + fileName))
                File.Create(reportPath + @"/" + fileName);
            try
            {
                using (var writer = new StreamWriter(reportPath + @"/" + fileName))
                {
                    writer.WriteLine(message);
                }               
            }
            catch(Exception e)
            {
                
                File.Create(reportPath + @"/" + "CrashLog" + errorIndex++ + ".txt");
                var writer = new StreamWriter(reportPath + @"/" + "CrashLog" + errorIndex + ".txt");
                writer.WriteLine(message);
                writer.Flush();
                writer.Close();
            }
            
        }
    }
}
