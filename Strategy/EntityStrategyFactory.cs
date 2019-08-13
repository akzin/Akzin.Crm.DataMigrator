using Akzin.Crm.DataMigrator.Services;

namespace Akzin.Crm.DataMigrator.Strategy
{
    public class EntityStrategyFactory : IEntityStrategyFactory
    {
        private readonly ICrmService crmService;

        public EntityStrategyFactory(ICrmService crmService)
        {
            this.crmService = crmService;
        }

        public IEntityStrategy Create(string entityLogicalName)
        {
            var metadata = crmService.GetEntityMetadata(entityLogicalName);

            switch (entityLogicalName)
            {
                case "duplicaterule":
                    return new DuplicateRuleStrategy(metadata);
                case "duplicaterulecondition":
                    return new DuplicateRuleConditionStrategy(metadata);
                default:
                    return new DefaultEntityStrategy(metadata);
            }
        }
    }
}