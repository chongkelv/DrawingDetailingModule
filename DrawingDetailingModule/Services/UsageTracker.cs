using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrawingDetailingModule.Services
{
    /// <summary>
    /// Static utility for easy API usage tracking
    /// </summary>
    public class UsageTracker
    {
        private static readonly Lazy<IUsageTrackingService> _service
            = new Lazy<IUsageTrackingService>(() => new CsvUsageTrackingService());

        /// <summary>
        /// Logs API error with exception details
        /// </summary>
        /// <param name="apiName">Name of the API</param>
        /// <param name="exception">Exception that occurred</param>
        public static void LogError(string apiName, Exception exception)
        {
            try
            {
                _service.Value.LogError(apiName, exception);
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
