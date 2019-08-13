using System;
using System.Collections.Generic;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;

namespace Akzin.Crm.DataMigrator.Services
{
    public class CrmService : ICrmService
    {
        private readonly CrmServiceClient service;

        public CrmService(string connectionString)
        {
            service = new CrmServiceClient("RequireNewInstance=True; " + connectionString);
            if (!service.IsReady)
            {
                throw new Exception($"Could not connect: {connectionString}");
            }
        }

        public EntityMetadata GetEntityMetadata(string entityLogicalname)
        {
            try
            {
                var response = ((RetrieveEntityResponse) service.Execute(new RetrieveEntityRequest
                {
                    LogicalName = entityLogicalname,
                    EntityFilters = EntityFilters.Attributes
                }));

                return response.EntityMetadata;
            }
            catch (FaultException<OrganizationServiceFault> e) when (e.Detail.ErrorCode == -2147220969)//"Could not find entity"
            {
                throw new Exception($"Could not find entity {entityLogicalname}");
            }
        }

        public List<Entity> GetEntities(QueryExpression queryExpression)
        {
            var entityList = new List<Entity>();

            while (true)
            {
                var result = service.RetrieveMultiple(queryExpression);

                entityList.AddRange(result.Entities);

                if (!result.MoreRecords)
                    break;
                throw new NotImplementedException("paging");
            }

            return entityList;
        }

        public OrganizationResponse ExecuteRequest(OrganizationRequest request)
        {
            return service.Execute(request);
        }
    }
}
