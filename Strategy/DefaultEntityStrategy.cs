using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Akzin.Crm.DataMigrator.Migration;

namespace Akzin.Crm.DataMigrator.Strategy
{
    public class DefaultEntityStrategy : IEntityStrategy
    {
        protected readonly EntityMetadata metadata;
        protected readonly List<AttributeMetadata> attributes;

        public DefaultEntityStrategy(EntityMetadata metadata)
        {
            this.metadata = metadata;
            attributes = metadata.Attributes.Where(ShouldExportAttribute).OrderBy(SortOrder).ToList();
        }

        public virtual string SortColumn => metadata.PrimaryNameAttribute;

        public virtual string[] Columns => attributes.Select(x => x.LogicalName).ToArray();

        public virtual string PrimaryIdAttribute => metadata.PrimaryIdAttribute;
        public virtual string EqualityColumn => metadata.PrimaryIdAttribute;

        public virtual QueryExpression QueryExpressionForEntitiesList
        {
            get
            {
                var q = new QueryExpression
                {
                    EntityName = metadata.LogicalName,
                    ColumnSet = new ColumnSet(attributes.Select(x => x.LogicalName).ToArray())
                };

                q.AddOrder(SortColumn, OrderType.Ascending);
                return q;
            }
        }

        public virtual string EntityLogicalName => metadata.LogicalName;

        public virtual AttributeMetadata GetAttribute(string columnLogicalName)
        {
            var attribute = attributes.FirstOrDefault(x => x.LogicalName == columnLogicalName);
            if (attribute == null)
            {
                throw new InvalidOperationException($"Attribute {columnLogicalName} does not exist in {metadata.LogicalName}");
            }
            return attribute;
        }

        public virtual void OnUpsertEntityRecord(CrmCommandsQueue requests, Entity crmEntity, Entity stamdataEntity, Dictionary<string, object> diff)
        {
            var diffExceptStatus = diff.Where(x => x.Key != "statecode" && x.Key != "statuscode").ToArray();
            if (diffExceptStatus.Any())
            {
                var upsert = new Entity(crmEntity?.LogicalName ?? stamdataEntity.LogicalName, crmEntity?.Id ?? stamdataEntity.Id);
                foreach (var item in diffExceptStatus)
                {
                    upsert[item.Key] = item.Value;
                }

                var isNew = crmEntity == null;
                if (isNew)
                    requests.AddUpdateStageStage(new CreateRequest { Target = upsert });
                else
                    requests.AddUpdateStageStage(new UpdateRequest { Target = upsert });
            }

            if (diff.ContainsKey("statecode") || diff.ContainsKey("statuscode"))
            {
                requests.AddUpdateStageStage(new SetStateRequest
                {
                    EntityMoniker = new EntityReference(crmEntity?.LogicalName ?? stamdataEntity.LogicalName, crmEntity?.Id ?? stamdataEntity.Id),
                    State = stamdataEntity["statecode"] as OptionSetValue,
                    Status = stamdataEntity["statuscode"] as OptionSetValue
                });
            }
        }

        public virtual void OnDeleteEntityRecord(CrmCommandsQueue requests, Entity excessCrmRow)
        {
            // disable first
            requests.AddDeleteStageStep(new SetStateRequest
            {
                EntityMoniker = excessCrmRow.ToEntityReference(),
                State = new OptionSetValue(1),
                Status = new OptionSetValue(2)
            });

            requests.AddDeleteStageStep(new DeleteRequest
            {
                Target = excessCrmRow.ToEntityReference()
            });
        }

        protected virtual bool ShouldExportAttribute(AttributeMetadata a)
        {
            if (a.AttributeType == AttributeTypeCode.Owner || a.LogicalName == "owneridtype")
            {
                return false;
            }

            if (a.IsValidForUpdate == false && a.IsPrimaryId == false)
                return false;

            var exclude = new[]
            {
                "timezoneruleversionnumber",
                "utcconversiontimezonecode",
            };
            if (exclude.Contains(a.LogicalName))
                return false;

            if (a is LookupAttributeMetadata lookup)
            {
                var targets = lookup.Targets;

                var excludedLookups = new[]
                {
                    "systemuser",
                    "calendar",
                    "mailbox",
                    "queue",
                    "territory",
                    "mobileofflineprofile",
                    "position",
                    "transactioncurrency",
                    "site",
                    "bookableresource",
                    "equipment",
                    "pricelevel",
                    "service",
                    "msdyn_taxcode",
                    "sla"
                };

                if (targets.All(x => excludedLookups.Contains(x)))
                    return false;
            }

            return true;
        }

        protected virtual string SortOrder(AttributeMetadata a)
        {
            int order = 0;
            switch (a.AttributeType)
            {
                case AttributeTypeCode.Uniqueidentifier:
                case AttributeTypeCode.BigInt:
                case AttributeTypeCode.DateTime:

                case AttributeTypeCode.String:
                case AttributeTypeCode.Virtual:
                case AttributeTypeCode.Integer:
                case AttributeTypeCode.EntityName:
                case AttributeTypeCode.Double:
                case AttributeTypeCode.Boolean:
                case AttributeTypeCode.Decimal:
                case AttributeTypeCode.Memo:
                case AttributeTypeCode.Picklist:
                case AttributeTypeCode.Money:
                    order = 50;
                    break;
                case AttributeTypeCode.Lookup:
                case AttributeTypeCode.Owner:
                    order = 60;
                    break;
                case AttributeTypeCode.Status:
                case AttributeTypeCode.State:
                    order = 70;
                    break;
                default:
                    order = 80;
                    break;
            }

            if (a.LogicalName == metadata.PrimaryIdAttribute)
            {
                order = 30;
            }
            else if (a.LogicalName == metadata.PrimaryNameAttribute)
            {
                order = 31;
            }

            return order + a.LogicalName;
        }
    }
}