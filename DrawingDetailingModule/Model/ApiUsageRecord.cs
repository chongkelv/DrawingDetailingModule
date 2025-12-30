using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrawingDetailingModule.Model
{
    public class ApiUsageRecord
    {
        public string ApiName { get; set; }
        public string EngineerName { get; set; }
        public string Version { get; set; }
        public DateTime UsedTime { get; set; }
        public string ComputerName { get; set; }
        public string SessionId { get; set; }
        public TimeSpan Duration { get; set; }
        public string Status { get; set; } // "Success" or "Error"
        public string Message { get; set; }
        public int NumberOfDescriptionsGenerated { get; set; }

        public string ToCsvLine()
        {
            string escapedErrorMessage = EscapeCsvField(Message ?? "");
            return $"{ApiName},{EngineerName},{Version},{UsedTime:yyyy-MM-dd HH:mm:ss},{ComputerName},{SessionId},{Duration.TotalSeconds:F2},{NumberOfDescriptionsGenerated},{Status},{escapedErrorMessage}";
        }

        public static string CsvHeader()
        {
            return "ApiName,EngineerName,Version,UsedTime,ComputerName,SessionId,DurationSeconds,NumberOfDescriptionsGenerated,Status,Message";
        }

        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "";

            // Escape quotes and wrap in quotes if contains comma, quote, or newline
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }
            return field;
        }
    }
}
