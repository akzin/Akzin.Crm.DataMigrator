using System.Collections.Generic;
using System.Linq;
using Akzin.Crm.DataMigrator.Helpers;
using Akzin.Crm.DataMigrator.Services;
using Akzin.Crm.DataMigrator.Strategy;

namespace Akzin.Crm.DataMigrator.Migration
{
    public class EntityImporter
    {
        private readonly ICrmService crmService;
        private readonly IFileService fileService;
        private readonly IEntityStrategyFactory entityStrategyFactory;

        public EntityImporter(ICrmService crmService, IFileService fileService, IEntityStrategyFactory entityStrategyFactory)
        {
            this.crmService = crmService;
            this.fileService = fileService;
            this.entityStrategyFactory = entityStrategyFactory;
        }

        public void Import(CrmCommandsQueue requestsQueue, string entityLogicalName)
        {
            Log.Debug($"Importing {entityLogicalName}");

            var strategy = entityStrategyFactory.Create(entityLogicalName);
            var converter = new EntityAndJsonConverter(strategy);

            var dataFileContent = fileService.ReadData(entityLogicalName);

            var jsonEntities = dataFileContent.Select(converter.ToEntity).ToList();
            var crmEntities = crmService.GetEntities(strategy.QueryExpressionForEntitiesList);
            var equalityColumn = strategy.EqualityColumn;

            foreach (var dataEntity in jsonEntities)
            {
                var currentDataEntity = crmEntities.FirstOrDefault(x => Equals(x[equalityColumn], dataEntity[equalityColumn]));

                var diff = new Dictionary<string, object>();

                foreach (var key in dataEntity.Attributes.Keys)
                {
                    var dataAttribute = dataEntity[key];
                    object currentAttribute = null;

                    if (currentDataEntity?.Contains(key) == true)
                        currentAttribute = currentDataEntity[key];

                    if (!Equals(currentAttribute, dataAttribute))
                    {
                        diff[key] = dataAttribute;
                    }
                }

                if (diff.Any())
                {
                    strategy.OnUpsertEntityRecord(requestsQueue, currentDataEntity, dataEntity, diff);
                }
            }

            var excessRows = crmEntities.Where(x => !jsonEntities.Any(y => Equals(x[equalityColumn], y[equalityColumn]))).ToList();
            foreach (var excessRow in excessRows)
            {
                strategy.OnDeleteEntityRecord(requestsQueue, excessRow);
            }
            Log.Debug($"Finished importing {entityLogicalName}");
        }
    }
}