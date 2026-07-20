using DrawingDetailingModule.Constants;
using DrawingDetailingModule.Model;
using DrawingDetailingModule.Services;
using DrawingDetailingModule.View;
using NXOpen;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DrawingDetailingModule.Controller
{
    public class Control
    {
        NXDrawing drawing;
        FormDrawingDetailing myForm;

        public NXDrawing GetDrawing => drawing;
        public FormDrawingDetailing GetForm => myForm;

        public Control()
        {
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
            double currentTextSize = GetDrawing.GetCurrentTextSize();
            GetDrawing.SetTextSize(myForm.FontSize);
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                List<MachiningDescriptionModel> descriptionModels;
                try
                {
                    descriptionModels = GetDrawing.IterateFeatures();
                    GetDrawing.CreateTable(GetDrawing.LocatedPoint[0], descriptionModels);
                    GetDrawing.GenerateWCStartPoints(descriptionModels);
                }
                catch (Exception err)
                {
                    NXDrawing.WriteToListingWindow("=== DrawingDetailingModule error ===");
                    NXDrawing.WriteToListingWindow(err.ToString());
                    NXDrawing.ShowMessageBox($"Error detail: {err.Message}", "Error", NXOpen.NXMessageBox.DialogType.Error);
                    return; // Do not log usage on failure
                }

                stopwatch.Stop();
                LogSuccessfulUsage(descriptionModels.Count, stopwatch.Elapsed.TotalSeconds);
            }
            finally
            {
                GetDrawing.SetTextSize(currentTextSize);
            }
        }

        private void LogSuccessfulUsage(int descriptionsGenerated, double actualDurationSeconds)
        {
            try
            {
                Part workPart = GetDrawing.WorkPart;
                string designer = AttributeManagerService.GetDesignBy(workPart);
                double manualTime = descriptionsGenerated * UsageTrackingConstants.MANUAL_TIME_PER_DESCRIPTION_SECONDS;

                var record = new DrawingDetailingUsageRecord
                {
                    ApiName = UsageTrackingConstants.API_NAME,
                    EngineerName = !string.IsNullOrEmpty(designer) ? designer : Environment.UserName,
                    Version = GetApiVersion(),
                    UsedTime = DateTime.Now,
                    ComputerName = Environment.MachineName,
                    SessionId = UsageTracker.SessionId,
                    Status = "Success",
                    ManualTimeSeconds = manualTime,
                    ActualDurationSeconds = actualDurationSeconds,
                    TimeSavingSeconds = manualTime - actualDurationSeconds,
                    DescriptionsGenerated = descriptionsGenerated,
                    ModelName = AttributeManagerService.GetModelName(workPart) ?? "",
                    PartName = AttributeManagerService.GetPartName(workPart) ?? ""
                };

                UsageTracker.LogUsage(record);
            }
            catch
            {
                // Silent fail - don't disrupt main functionality
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

        public int GetDimensionTextSize()
        {
            return drawing.GetDimensionTextSize();
        }
    }
}
