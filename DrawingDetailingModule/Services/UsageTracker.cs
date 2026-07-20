using DrawingDetailingModule.Model;
using System;

namespace DrawingDetailingModule.Services
{
    /// <summary>
    /// Static utility for easy API usage tracking
    /// </summary>
    public class UsageTracker
    {
        private static readonly Lazy<IUsageTrackingService> _service
            = new Lazy<IUsageTrackingService>(() => new CsvUsageTrackingService());

        public static string SessionId => CsvUsageTrackingService.SessionId;

        /// <summary>
        /// Logs API usage. Never throws - failures here must not disrupt main functionality.
        /// </summary>
        public static void LogUsage(DrawingDetailingUsageRecord record)
        {
            try
            {
                _service.Value.LogUsage(record);
            }
            catch
            {
                // Silent fail - never disrupt main functionality
            }
        }

        /// <summary>
        /// For testing or custom service injection
        /// </summary>
        internal static IUsageTrackingService Service => _service.Value;
    }
}
