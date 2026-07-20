using System;

namespace DrawingDetailingModule.Model
{
    /// <summary>
    /// One usage-tracking record for a single successful Control.Start() run.
    /// </summary>
    public class DrawingDetailingUsageRecord
    {
        // Standard fields
        public string ApiName { get; set; }
        public string EngineerName { get; set; }
        public string Version { get; set; }
        public DateTime UsedTime { get; set; }
        public string ComputerName { get; set; }
        public string SessionId { get; set; }
        public string Status { get; set; }

        // Time-saving calculation
        public double ManualTimeSeconds { get; set; }
        public double ActualDurationSeconds { get; set; }
        public double TimeSavingSeconds { get; set; }

        // App-specific fields
        public int DescriptionsGenerated { get; set; }
        public string ModelName { get; set; }
        public string PartName { get; set; }

        public string ToCsvLine()
        {
            return $"{ApiName},{EngineerName},{Version},{UsedTime:yyyy-MM-dd HH:mm:ss},{ComputerName}," +
                   $"{SessionId},{Status}," +
                   $"{ManualTimeSeconds:F2},{ActualDurationSeconds:F2},{TimeSavingSeconds:F2}," +
                   $"{DescriptionsGenerated}," +
                   $"{EscapeCsvField(ModelName ?? "")},{EscapeCsvField(PartName ?? "")}";
        }

        public static string CsvHeader()
        {
            return "ApiName,EngineerName,Version,UsedTime,ComputerName,SessionId,Status," +
                   "ManualTimeSeconds,ActualDurationSeconds,TimeSavingSeconds," +
                   "DescriptionsGenerated," +
                   "ModelName,PartName";
        }

        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "";
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            return field;
        }
    }
}
