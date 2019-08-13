using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Akzin.Crm.DataMigrator.Migration;

namespace Akzin.Crm.DataMigrator.Strategy
{
    public interface IEntityStrategy
    {
        string EntityLogicalName { get; }
        string[] Columns { get; }
        string PrimaryIdAttribute { get; }
        string EqualityColumn { get; }


        QueryExpression QueryExpressionForEntitiesList { get; }

        AttributeMetadata GetAttribute(string columnLogicalName);

        void OnUpsertEntityRecord(CrmCommandsQueue requests, Entity crmEntity, Entity stamdataEntity, Dictionary<string, object> diff);

        void OnDeleteEntityRecord(CrmCommandsQueue requests, Entity excessCrmRow);
    }
}