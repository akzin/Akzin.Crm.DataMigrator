using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Akzin.Crm.DataMigrator.Helpers;
using Akzin.Crm.DataMigrator.Services;

namespace Akzin.Crm.DataMigrator.Migration
{
    public class CrmCommandsQueue
    {
        private readonly List<OrganizationRequest> updateStageSteps = new List<OrganizationRequest>();
        private readonly List<OrganizationRequest> deleteStageSteps = new List<OrganizationRequest>();

        public CrmCommandsQueue(string entityLogicalName)
        {
            this.EntityLogicalName = entityLogicalName;
        }

        public int Count => updateStageSteps.Count + deleteStageSteps.Count;

        public bool IsEmpty => updateStageSteps.Count == 0 && deleteStageSteps.Count == 0;

        public string EntityLogicalName { get; }

        public void AddUpdateStageStage(OrganizationRequest request)
        {
            updateStageSteps.Add(request);
        }

        public void AddDeleteStageStep(OrganizationRequest request)
        {
            deleteStageSteps.Add(request);
        }

        public void ExecuteDeleteSteps(ICrmService crmService)
        {
            var requestsQueue = new Queue<OrganizationRequest>(deleteStageSteps);

            while (requestsQueue.Count > 0)
            {
                var request = requestsQueue.Dequeue();
                try
                {
                    var res = crmService.ExecuteRequest(request);
                }
                catch (Exception e)
                {
                    string message = e.Message;

                    if (request is DeleteRequest deleteRequest)
                    {
                        var target = deleteRequest.Target;
                        message = $"Error while trying to delete {target.LogicalName}/{target.Id}: {message}";
                    }

                    Log.Error(message, e);
                    Debugger.Break();
                }
            }
        }

        public void ExecuteUpdateSteps(ICrmService crmService)
        {
            var requestsQueue = new Queue<OrganizationRequest>(updateStageSteps);

            do
            {
                var requestsChunk = ToOrganizationRequestCollection(DequeueChunk(requestsQueue, 1000));

                if (requestsChunk.Count == 0)
                    break;


                var transactionalRequest = new ExecuteTransactionRequest
                {
                    ReturnResponses = true,
                    Requests = requestsChunk
                };

                var res = crmService.ExecuteRequest(transactionalRequest);
            } while (true);
        }

        private static IEnumerable<T> DequeueChunk<T>(Queue<T> queue, int chunkSize)
        {
            for (int i = 0; i < chunkSize && queue.Count > 0; i++)
            {
                yield return queue.Dequeue();
            }
        }

        private static OrganizationRequestCollection ToOrganizationRequestCollection(IEnumerable<OrganizationRequest> requests)
        {
            var collection = new OrganizationRequestCollection();

            foreach (var request in requests)
            {
                collection.Add(request);
            }

            return collection;
        }
    }
}
