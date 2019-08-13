using System.Collections.Generic;
using System.Linq;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Akzin.Crm.DataMigrator.Migration;

namespace Akzin.Crm.DataMigrator.Strategy
{
    public class DuplicateRuleStrategy : DefaultEntityStrategy
    {
        public DuplicateRuleStrategy(EntityMetadata metadata) : base(metadata)
        {
        }

        protected override bool ShouldExportAttribute(AttributeMetadata a)
        {
            var cols = new[]{
                "description",
                "baseentityname",
                "name",
                "matchingentityname",
                "duplicateruleid",
                "iscasesensitive",
                "excludeinactiverecords"
            };

            if (cols.Contains(a.LogicalName))
                return true;
            return base.ShouldExportAttribute(a);
        }

        public override void OnUpsertEntityRecord(CrmCommandsQueue requests, Entity crmEntity, Entity stamdataEntity, Dictionary<string, object> diff)
        {
            var upsert = new Entity(crmEntity?.LogicalName ?? stamdataEntity.LogicalName, crmEntity?.Id ?? stamdataEntity.Id);
            foreach (var item in diff)
            {
                upsert[item.Key] = item.Value;
            }

            var isNew = crmEntity == null;
            if (isNew)
            {
                requests.AddUpdateStageStage(new CreateRequest { Target = upsert });
                requests.AddDeleteStageStep(new PublishDuplicateRuleRequest { DuplicateRuleId = upsert.Id });
            }
            else
            {
                requests.AddUpdateStageStage(new UpdateRequest {Target = upsert});
            }
        }

        public override void OnDeleteEntityRecord(CrmCommandsQueue requests, Entity excessCrmRow)
        {
            requests.AddDeleteStageStep(new DeleteRequest
            {
                Target = excessCrmRow.ToEntityReference()
            });
        }

    }
}
