using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;

namespace Akzin.Crm.DataMigrator.Services
{
    public interface ICrmService
    {
        EntityMetadata GetEntityMetadata(string entityLogicalname);

        List<Entity> GetEntities(QueryExpression queryExpression);

        OrganizationResponse ExecuteRequest(OrganizationRequest request);
    }
}