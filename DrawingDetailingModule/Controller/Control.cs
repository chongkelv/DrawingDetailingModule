using DrawingDetailingModule.Model;
using DrawingDetailingModule.Services;
using DrawingDetailingModule.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrawingDetailingModule.Controller
{
    public class Control
    {
        NXDrawing drawing;
        FormDrawingDetailing myForm;
        private DateTime sessionStartTime;
        private int numberOfDescriptionsGenerated = 0;

        public NXDrawing GetDrawing => drawing;
        public FormDrawingDetailing GetForm => myForm;

        public Control()
        {
            sessionStartTime = DateTime.Now;
            drawing = new NXDrawing(this);

            if (!drawing.IsDrawingOpen())
            {
                return;
            }

            myForm = new FormDrawingDetailing(this);
            myForm.Show();
        }

        public void Start()
        {
            try
            {                
                double currentTextSize = GetDrawing.GetCurrentTextSize();
                GetDrawing.SetTextSize(myForm.FontSize);
                List<MachiningDescriptionModel> descriptionModels = new List<MachiningDescriptionModel>();
                try
                {
                    descriptionModels = GetDrawing.IterateFeatures();
                    numberOfDescriptionsGenerated = descriptionModels.Count;
                    GetDrawing.CreateTable(GetDrawing.LocatedPoint[0], descriptionModels);
                    GetDrawing.GenerateWCStartPoints(descriptionModels);
                }
                catch (Exception err)
                {
                    NXDrawing.ShowMessageBox($"Error detail: {err.Message}", "Error", NXOpen.NXMessageBox.DialogType.Error);
                }
                GetDrawing.SetTextSize(currentTextSize);

                // Log sucessful usage with meaningful data
                LogSuccessfulUsage();
            }
            catch (Exception ex)
            {
                // Log error usage
                UsageTracker.LogError("DrawingDetailingModule", ex);
                throw; // Re-throw to maintain original behavior                    
            }
        }

        private void LogSuccessfulUsage()
        {
            try
            {
                var duration = DateTime.Now - sessionStartTime;
                string partName = GetDrawing.PartName ?? "Unknown";

                var record = new ApiUsageRecord
                {
                    ApiName = "DrawingDetailingModule",
                    EngineerName = GetEngineerName(),
                    Version = GetApiVersion(),
                    UsedTime = DateTime.Now,
                    ComputerName = Environment.MachineName,
                    SessionId = GetSessionId(),
                    Duration = duration,
                    Status = "Success",
                    Message = $"Drawings name: {partName}",
                    NumberOfDescriptionsGenerated = numberOfDescriptionsGenerated
                };

                UsageTracker.Service.LogUsage(record);
            }
            catch
            {
                // Silent fail - don't disrupt main functionality
            }
        }

        private string GetEngineerName()
        {
            try
            {
                // Try to get designer name from project info file first
                var projectInfo = ProjectInfoService.ReadFromFile();
                if (!string.IsNullOrEmpty(projectInfo.Designer))
                {
                    return projectInfo.Designer;
                }
            }
            catch
            {
                // Fallback to environment if project info unavailable
            }

            return Environment.UserName;
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

        private string GetSessionId()
        {
            // Use the same session ID as the tracking service for consistency
            return UsageTracker.Service.GetType()
                .GetField("SessionId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                ?.GetValue(null)?.ToString() ?? Guid.NewGuid().ToString();
        }

        public int GetDimensionTextSize()
        {
            return drawing.GetDimensionTextSize();
        }
    }
}
