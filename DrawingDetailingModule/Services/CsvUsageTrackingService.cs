using DrawingDetailingModule.Constants;
using DrawingDetailingModule.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrawingDetailingModule.Services
{
    /// <summary>
    /// CSV-based implementation of usage tracking
    /// </summary>
    public class CsvUsageTrackingService : IUsageTrackingService
    {
        private static readonly string SessionId = Guid.NewGuid().ToString();
        private static readonly Stopwatch SessionStopwatch = Stopwatch.StartNew();

        public void LogUsage(ApiUsageRecord record)
        {
            try
            {
                string filePath = GetCsvFilePath(record.ApiName);
                EnsureDirectoryExists(filePath);

                bool fileExists = File.Exists(filePath);

                using (var writer = new StreamWriter(filePath, true))
                {
                    if (!fileExists)
                    {
                        writer.WriteLine(ApiUsageRecord.CsvHeader());
                    }

                    writer.WriteLine(record.ToCsvLine());
                }
            }
            catch (Exception ex)
            {
                // Silent fail - don't disrupt main API functionality
                LogToFallback(record, ex);
            }
        }

        public void LogError(string apiName, Exception exception)
        {
            var record = new ApiUsageRecord
            {
                ApiName = apiName,
                EngineerName = Environment.UserName,
                Version = GetApiVersion(),
                UsedTime = DateTime.Now,
                ComputerName = Environment.MachineName,
                SessionId = SessionId,
                Duration = SessionStopwatch.Elapsed,
                Status = "Error",
                Message = exception?.Message ?? "Unknown error"
            };
            LogUsage(record);
        }

        public string GetCsvFilePath(string apiName)
        {
            string basePath = Directory.Exists(UsageTrackingConstants.BASE_PATH)
                ? UsageTrackingConstants.BASE_PATH
                : UsageTrackingConstants.FALLBACK_PATH;
            string monthlyFolder = UsageTrackingConstants.GetMonthlyFolder();
            string fileName = UsageTrackingConstants.GetCsvFileName(apiName);

            return Path.Combine(basePath, monthlyFolder, fileName);
        }

        private void EnsureDirectoryExists(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private string GetApiVersion()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                return assembly.GetName().Version?.ToString() ?? "1.1.0.0";
            }
            catch
            {
                return "1.1.0.0";
            }
        }

        private void LogToFallback(ApiUsageRecord record, Exception ex)
        {
            try
            {
                // Try fallback local path
                string fallbackPath = Path.Combine(UsageTrackingConstants.FALLBACK_PATH,
                    UsageTrackingConstants.GetMonthlyFolder(),
                    $"error-{UsageTrackingConstants.GetCsvFileName(record.ApiName)}");

                EnsureDirectoryExists(fallbackPath);


                using (var writer = new StreamWriter(fallbackPath, append: true))
                {
                    writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss},Error logging usage: {ex.Message}");
                    writer.WriteLine(record.ToCsvLine());
                }
            }
            catch
            {
                // Ultimate fallback - do nothing to avoid disrupting main functionality
            }
        }
    }
}
