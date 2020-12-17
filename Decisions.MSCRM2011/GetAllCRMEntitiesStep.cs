using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Flow.Mapping;
using DecisionsFramework.ServiceLayer.Services.ContextData;
using DecisionsFramework.Utilities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Decisions.MSCRM2011
{
    [Writable]
    public class GetAllCRMEntitiesStep : BaseCRMEntityStep, ISyncStep, IDataConsumer
    {
        public GetAllCRMEntitiesStep()
        {

        }
        public GetAllCRMEntitiesStep(string entityId)
        {
            EntityId = entityId;
        }

        public const string PATH_ERROR = "Error";
        public const string PATH_SUCCESS = "Success";

        public override string StepName
        {
            get
            {
                return string.Format("Get All {0} Entities", CRMEntity.CRMEntityDisplayName);
            }
        }


        public override OutcomeScenarioData[] OutcomeScenarios
        {
            get
            {
                Type type = GetMSCRMType();
                return new[]
                    {
                        new OutcomeScenarioData(PATH_SUCCESS, new DataDescription[] { new DataDescription(type, string.Format("{0} Entities",CRMEntity.CRMEntityDisplayName),true)}),
                        new OutcomeScenarioData(PATH_ERROR, new DataDescription[] { new DataDescription(typeof(string), "Error Message") })
                    };
            }
        }

        public DataDescription[] InputData
        {
            get
            {
                List<DataDescription> inputData = new List<DataDescription>();
                return inputData.ToArray();
            }
        }

        public ResultData Run(StepStartData data)
        {
            try
            {
                IOrganizationService serviceProxy = GetServiceProxy();

                EntityCollection entityCollection = serviceProxy.RetrieveMultiple(new QueryExpression()
                {
                    EntityName = CRMEntity.CRMEntityName,
                    ColumnSet = new ColumnSet(true)
                });
                Type type = GetMSCRMType();

                ArrayList entities = new ArrayList();
                foreach (var entity in entityCollection.Entities)
                {
                    var obj = CreateObjectFromAttributes(type, entity.Attributes);
                    entities.Add(obj);
                }
                Array value = Array.CreateInstance(type, entities.Count);
                entities.CopyTo(value);
                return new ResultData(PATH_SUCCESS, new DataPair[] { new DataPair(string.Format("{0} Entities", CRMEntity.CRMEntityDisplayName), value) });
            }
            catch (Exception ex)
            {
                return new ResultData(PATH_ERROR, new KeyValuePair<string, object>[] { new KeyValuePair<string, object>("Error Message", ex.Message) });
            }
        }
    }
}
