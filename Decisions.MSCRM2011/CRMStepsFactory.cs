using Decisions.MSCRM2011;
using DecisionsFramework;
using DecisionsFramework.Data.ORMapper;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.ServiceLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Decisions.MSCRM2011
{

    public class CRM2011StepsFactory : BaseFlowEntityFactory
    {
        // Changed folder node and step IDs to avoid duplicated steps:
        const string PARENT_NODE = "MSCRM2011";

        const string GET_ALL_STEP_INFO = "getAllEntities2011";
        const string GET_STEP_INFO = "getEntityById2011";
        const string ADD_STEP_INFO = "addCRMEntity2011";
        const string UPDATE_STEP_INFO = "updateCRMEntity2011";
        const string DELETE_STEP_INFO = "deleteEntity2011";

        public override string[] GetRootCategories(string flowId, string folderId)
        {
            // return root folders for each node
            return new string[] { "Data" };

        }

        public override FlowStepToolboxInformation[] GetFavoriteSteps(string flowId, string folderId)
        {
            return new FlowStepToolboxInformation[0];
        }
        public override string[] GetSubCategories(string[] nodes, string flowId, string folderId)
        {
            if (nodes == null || nodes.Length == 0) return new string[0];
            if (nodes[0] == "Data")
            {
                if(nodes.Length == 1) return new string[] { PARENT_NODE };
                if(nodes[1] == PARENT_NODE)
                {
                    if (nodes.Length == 2)
                    {
                        return ModuleSettingsAccessor<CRM2011Settings>.Instance.Connections.Select(x => x.ConnectionName).ToArray();
                    }
                    else if (nodes.Length == 3)
                    {
                        CRM2011Connection connection = CRM2011Connection.GetCRMConnectionForName(nodes[2]);
                        if (connection == null) return new string[0];
                        ORM<CRM2011Entity> orm = new ORM<CRM2011Entity>();
                        CRM2011Entity[] crmEntities = orm.Fetch(new WhereCondition[]
                        {
                        new FieldWhereCondition("connection_id", QueryMatchType.Equals, connection.connectionId)
                        });
                        return crmEntities.Select(t => t.CRMEntityDisplayName).ToArray();
                    }
                }
            }
            return new string[0];

        }
        public override FlowStepToolboxInformation[] GetStepsInformation(string[] nodes, string flowId, string folderId)
        {
            // return step info
            if (nodes == null || nodes.Length != 4 || nodes[0] != "Data" || nodes[1] != PARENT_NODE)
                return new FlowStepToolboxInformation[0];

            List<FlowStepToolboxInformation> list = new List<FlowStepToolboxInformation>();
            CRM2011Connection connection = CRM2011Connection.GetCRMConnectionForName(nodes[2]);
            if(connection == null) return new FlowStepToolboxInformation[0];

            ORM<CRM2011Entity> orm = new ORM<CRM2011Entity>();
            CRM2011Entity[] crmEntities = orm.Fetch(new WhereCondition[] {
                new FieldWhereCondition("connection_id", QueryMatchType.Equals, connection.connectionId),
                new FieldWhereCondition("crm_entity_display_name", QueryMatchType.Equals, nodes[3])
            });

            foreach (CRM2011Entity entity in crmEntities)
            {
                list.Add(new FlowStepToolboxInformation("Get All Entities", nodes, string.Format(GET_ALL_STEP_INFO + "${0}", entity.entityId)));
                list.Add(new FlowStepToolboxInformation("Get Entity By Id", nodes, string.Format(GET_STEP_INFO + "${0}", entity.entityId)));
                list.Add(new FlowStepToolboxInformation("Add Entity", nodes, string.Format(ADD_STEP_INFO + "${0}", entity.entityId)));
                list.Add(new FlowStepToolboxInformation("Update Entity", nodes, string.Format(UPDATE_STEP_INFO + "${0}", entity.entityId)));
                list.Add(new FlowStepToolboxInformation("Delete Entity", nodes, string.Format(DELETE_STEP_INFO + "${0}", entity.entityId)));
            }
            return list.ToArray();
        }

        public override IFlowEntity CreateStep(string[] nodes, string stepId, StepCreationInfo additionalInfo)
        {
            string[] parts = (stepId ?? string.Empty).Split('$');

            string crmEntityId = parts[1];
            // create steps
            if (stepId.StartsWith(ADD_STEP_INFO))
            {
                return new AddCRMEntityStep(crmEntityId);
            }
            if (stepId.StartsWith(UPDATE_STEP_INFO))
            {
                return new UpdateCRMEntityStep(crmEntityId);
            }
            if (stepId.StartsWith(GET_ALL_STEP_INFO))
            {
                return new GetAllCRMEntitiesStep(crmEntityId);
            }
            if (stepId.StartsWith(DELETE_STEP_INFO))
            {
                return new DeleteCRMEntityStep(crmEntityId);
            }
            if (stepId.StartsWith(GET_STEP_INFO))
            {
                return new GetCRMEntityByIdStep(crmEntityId);
            }
            return null;
        }

        public override FlowStepToolboxInformation[] SearchSteps(string flowId, string folderId, string searchString, int maxRecords)
        {
            // return null;
            return new FlowStepToolboxInformation[0];
        }
    }
}
