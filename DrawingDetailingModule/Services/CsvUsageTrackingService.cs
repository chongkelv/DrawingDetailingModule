using DrawingDetailingModule.Constants;
using DrawingDetailingModule.Model;
using System;
using System.IO;

namespace DrawingDetailingModule.Services
{
    /// <summary>
    /// CSV-based implementation of usage tracking
    /// </summary>
    public class CsvUsageTrackingService : IUsageTrackingService
    {
        public static readonly string SessionId = Guid.NewGuid().ToString();

        public void LogUsage(DrawingDetailingUsageRecord record)
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
                        writer.WriteLine(DrawingDetailingUsageRecord.CsvHeader());
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

        private void LogToFallback(DrawingDetailingUsageRecord record, Exception ex)
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
