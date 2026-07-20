using DrawingDetailingModule.Model;

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
        void LogUsage(DrawingDetailingUsageRecord record);
    }
}
