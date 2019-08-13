using System.Linq;
using Akzin.Crm.DataMigrator.Helpers;
using Akzin.Crm.DataMigrator.Services;
using Akzin.Crm.DataMigrator.Strategy;

namespace Akzin.Crm.DataMigrator.Migration
{
    public class EntityExporter
    {
        private readonly ICrmService crmService;
        private readonly IFileService fileService;
        private readonly IEntityStrategyFactory entityStrategyFactory;

        public EntityExporter(ICrmService crmService, IFileService fileService, IEntityStrategyFactory entityStrategyFactory)
        {
            this.crmService = crmService;
            this.fileService = fileService;
            this.entityStrategyFactory = entityStrategyFactory;
        }

        public void Export(string entityLogicalName)
        {
            Log.Debug($"Exporting data {entityLogicalName}");

            var strategy = entityStrategyFactory.Create(entityLogicalName);

            var data = crmService.GetEntities(strategy.QueryExpressionForEntitiesList);

            var jsonContent = data.Select(new EntityAndJsonConverter(strategy).ToJson).ToList();
            fileService.WriteData(entityLogicalName, jsonContent);
            Log.Debug($"Finished data {entityLogicalName}");
        }
    }
}