using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Akzin.Crm.DataMigrator.Helpers;

namespace Akzin.Crm.DataMigrator.Services
{
    public class FileService : IFileService
    {
        private readonly string outputFolder;

        public FileService(string outputFolder)
        {
            this.outputFolder = outputFolder;
        }

        public void WriteData(string entityLogicalName, List<Dictionary<string, object>> content)
        {
            var path = Path.Combine(outputFolder, GetFileName(entityLogicalName));

            var sw = new StringWriter();
            using (var jtw = new JsonTextWriter(sw)
            {
                Formatting = Formatting.Indented,
                Indentation = 4,
                IndentChar = ' '
            })
            {
                new JsonSerializer().Serialize(jtw, content);
            }
            var contents = sw.ToString();

            FileEx.WriteAllText(path, contents);
        }

        public List<Dictionary<string, object>> ReadData(string entityLogicalName)
        {
            var path = Path.Combine(outputFolder, GetFileName(entityLogicalName));

            var contents = File.ReadAllText(path);
            var jsonContent = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(contents);
            return jsonContent;
        }

        private string GetFileName(string entityLogicalName)
        {
            return $"{entityLogicalName}.json";
        }
    }
}
