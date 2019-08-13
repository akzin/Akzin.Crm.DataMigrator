using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Akzin.Crm.DataMigrator.Services;

namespace Akzin.Crm.DataMigrator.Helpers
{
    public class DisabledWorkflowsScope : IDisposable
    {
        private readonly ICrmService crmService;
        private readonly List<Entity> workflows = new List<Entity>();
        public DisabledWorkflowsScope(ICrmService crmService, IEnumerable<string> entityLogicalnames)
        {
            this.crmService = crmService;

            var entityLogicalnamesArray = entityLogicalnames.ToArray();

            if (entityLogicalnamesArray.Any())
            {

                workflows.AddRange(crmService.GetEntities(new QueryExpression("sdkmessageprocessingstep")
                {
                    LinkEntities =
                    {
                        new LinkEntity("sdkmessageprocessingstep", "sdkmessagefilter", "sdkmessagefilterid", "sdkmessagefilterid", JoinOperator.Inner)
                        {
                            Columns = new ColumnSet("secondaryobjecttypecode", "primaryobjecttypecode"),
                            LinkCriteria = new FilterExpression(LogicalOperator.And)
                            {
                                Conditions =
                                {
                                    new ConditionExpression("primaryobjecttypecode", ConditionOperator.In,
                                        entityLogicalnamesArray)
                                }
                            }
                        }
                    },
                    ColumnSet = new ColumnSet("name", "statuscode", "statecode"),
                    Criteria = new FilterExpression(LogicalOperator.And)
                    {
                        Conditions =
                        {
                            new ConditionExpression("statuscode", ConditionOperator.Equal, 1), // active only
                            new ConditionExpression("ishidden", ConditionOperator.Equal, false)
                        }
                    }
                }));

                workflows.AddRange(crmService.GetEntities(new QueryExpression("workflow")
                {
                    ColumnSet = new ColumnSet("primaryentity", "type", "statecode", "statuscode", "name", "triggeronupdateattributelist", "triggeroncreate", "triggerondelete"),
                    Criteria = new FilterExpression(LogicalOperator.And)
                    {
                        Conditions =
                        {
                            new ConditionExpression("primaryentity", ConditionOperator.In, entityLogicalnamesArray),
                            new ConditionExpression("statuscode", ConditionOperator.Equal, 1), // active only
                            new ConditionExpression("type", ConditionOperator.Equal, 1), // activation only (no old snapshots)
                        },
                        Filters =
                        {
                            new FilterExpression(LogicalOperator.Or)
                            {
                                Conditions =
                                {
                                    new ConditionExpression("triggeronupdateattributelist", ConditionOperator.NotNull),
                                    new ConditionExpression("triggeroncreate", ConditionOperator.Equal, true),
                                    new ConditionExpression("triggerondelete", ConditionOperator.Equal, true)
                                }
                            }
                        }
                    }
                }));
            }
            Disable();
        }

        private void Disable()
        {
            Parallel.ForEach(workflows, entity =>
            {
                crmService.ExecuteRequest(new SetStateRequest
                {
                    EntityMoniker = entity.ToEntityReference(),
                    State = new OptionSetValue(1),
                    Status = new OptionSetValue(2)
                });
            });
        }

        private void RestoreEnableState()
        {
            Parallel.ForEach(workflows, entity =>
            {
                crmService.ExecuteRequest(new SetStateRequest
                {
                    EntityMoniker = entity.ToEntityReference(),
                    State = new OptionSetValue(0),
                    Status = new OptionSetValue(1)
                });
            });
        }

        public void Dispose()
        {
            RestoreEnableState();
        }
    }
}
