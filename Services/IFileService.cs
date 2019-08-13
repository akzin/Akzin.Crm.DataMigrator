using System.Collections.Generic;

namespace Akzin.Crm.DataMigrator.Services
{
    public interface IFileService
    {
        void WriteData(string entityLogicalName, List<Dictionary<string, object>> content);

        List<Dictionary<string, object>> ReadData(string entityLogicalName);
    }
}