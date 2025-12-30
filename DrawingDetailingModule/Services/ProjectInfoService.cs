using DrawingDetailingModule.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrawingDetailingModule.Services
{
    public class ProjectInfoService
    {
        const string DIRECTORY = "D:/NXCUSTOM/temp";
        const string PROJ_INFO_FILENAME = "project_info.data";
        public static ProjectInfoModel ReadFromFile()
        {
            string fullPathFileName = Path.Combine(DIRECTORY, PROJ_INFO_FILENAME);
            if (!File.Exists(fullPathFileName))
            {
                string message = $"File not found: {fullPathFileName}";
                throw new FileNotFoundException(message);
            }

            Dictionary<string, string> result = new Dictionary<string, string>();
            string[] keys = new string[] { "Model", "Part", "CodePrefix", "Designer" };

            try
            {
                using (StreamReader reader = new StreamReader(fullPathFileName))
                {
                    string value;
                    foreach (string key in keys)
                    {
                        value = reader.ReadLine();
                        result.Add(key, value);
                    }
                }
            }
            catch (FileNotFoundException ex)
            {
                string message = $"File not found: {ex.Message}";
                string title = "Error file not found";
                NXDrawing.ShowMessageBox(message, title, NXOpen.NXMessageBox.DialogType.Error);
            }

            return new ProjectInfoModel
            {
                Model = result["Model"],
                Part = result["Part"],
                CodePrefix = result["CodePrefix"],
                Designer = result["Designer"]
            };
        }
    }
}
