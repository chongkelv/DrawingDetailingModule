using DrawingDetailingModule.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrawingDetailingModule.Services
{
    /// <summary>
    /// Service for tracking API usage
    /// </summary>
    public interface IUsageTrackingService
    {
        /// <summary>
        /// Logs API usage to storage
        /// </summary>
        /// <param name="record">Usage record to log</param>
        void LogUsage(ApiUsageRecord record);

        /// <summary>
        /// Logs API error with exception details
        /// </summary>
        /// <param name="apiName">Name of the API</param>
        /// <param name="exception">Exception that occurred</param>
        void LogError(string apiName, Exception exception);
    }
}
