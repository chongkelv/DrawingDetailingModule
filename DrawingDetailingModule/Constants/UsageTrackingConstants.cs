using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrawingDetailingModule.Constants
{
    public class UsageTrackingConstants
    {
        // Network share path
        public const string BASE_PATH = @"\\SPL-SMB\DE\Common\SGA\3DA setup folder\logs\usage_logs";

        // Fallback local path if network unavailable
        public static readonly string FALLBACK_PATH = Path.Combine(@"D:\NXCUSTOM\logs\usage_logs", "NXAPIUsage");

        public const string CSV_EXTENSION = ".csv";

        public static string GetMonthlyFolder()
        {
            return DateTime.Now.ToString("yyyy-MM");
        }

        public static string GetCsvFileName(string apiName)
        {
            return $"{GetMonthlyFolder()}-{apiName}{CSV_EXTENSION}";
        }
    }
}
