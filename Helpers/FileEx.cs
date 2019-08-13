using System;
using System.IO;
using System.Text;

namespace Akzin.Crm.DataMigrator.Helpers
{
    public static class FileEx
    {
        public static void WriteAllText(string path, string contents)
        {
            string existing = null;
            if (File.Exists(path))
            {
                existing = File.ReadAllText(path);
            }

            if (existing != contents)
            {
                File.WriteAllText(path, contents);
            }
        }
    }
}
