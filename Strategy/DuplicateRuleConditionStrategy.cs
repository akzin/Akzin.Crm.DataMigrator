using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Akzin.Crm.DataMigrator.Migration;

namespace Akzin.Crm.DataMigrator.Strategy
{
    public class DuplicateRuleConditionStrategy : DefaultEntityStrategy
    {
        public DuplicateRuleConditionStrategy(EntityMetadata metadata) : base(metadata)
        {
        }

        protected override bool ShouldExportAttribute(AttributeMetadata a)
        {
            if (a.IsValidForCreate == true)
                return true;

            return base.ShouldExportAttribute(a);
        }

        public override void OnDeleteEntityRecord(CrmCommandsQueue requests, Entity excessCrmRow)
        {
            requests.AddDeleteStageStep(new DeleteRequest
            {
                Target = excessCrmRow.ToEntityReference()
            });
        }

        public override string SortColumn => "baseattributename";
    }
}
