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
        public const string API_NAME = "DrawingDetailingModule";

        // Manual (pre-automation) time to compose one machining-description table row by hand. The
        // auto-side time is NOT a fixed constant - it is measured live per run via Stopwatch
        // (ActualDurationSeconds on DrawingDetailingUsageRecord).
        public const double MANUAL_TIME_PER_DESCRIPTION_SECONDS = 120.0;

        // Network share path
        public const string BASE_PATH = @"\\SPL-SMB\DE\Common\SGA\3DA setup folder\logs\usage_logs";

        // Fallback local path if network unavailable
        public static readonly string FALLBACK_PATH = Path.Combine(@"D:\NXCUSTOM\logs\usage_logs", API_NAME);

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
