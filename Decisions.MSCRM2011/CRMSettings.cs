using DecisionsFramework;
using DecisionsFramework.Data.ORMapper;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.DataStructure;
using DecisionsFramework.Design.Flow.Mapping;
using DecisionsFramework.Design.Properties;
using DecisionsFramework.Design.Properties.Attributes;
using DecisionsFramework.ServiceLayer;
using DecisionsFramework.ServiceLayer.Actions;
using DecisionsFramework.ServiceLayer.Actions.Common;
using DecisionsFramework.ServiceLayer.Utilities;
using DecisionsFramework.Utilities.CodeGeneration;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Decisions.MSCRM2011
{

    public class CRM2011Settings : AbstractModuleSettings
    {
        public CRM2011Settings()
        {
            EntityName = "MSCRM Settings (2011)";
            Connections = new CRM2011Connection[0];
        }

        [ORMField(typeof(ORMXmlSerializedFieldConverter))]
        [PropertyClassification("Settings")]
        public CRM2011Connection[] Connections { get; set; }

        public override BaseActionType[] GetActions(AbstractUserContext userContext, EntityActionType[] types)
        {
            return new BaseActionType[]
            {
                    new EditEntityAction(GetType(), "Edit", null) { IsDefaultGridAction = true },
                    new ConfirmAction("Refresh Entity Lists", null, "Refresh Entity Lists", "Refresh all entity lists now?", RefreshEntityLists)
            };
        }
        public override void BeforeSave()
        {
            base.BeforeSave();
            if(Connections != null)
            {
                foreach(CRM2011Connection connection in Connections)
                {
                    connection.BeforeSave();
                }
            }
        }
        private void RefreshEntityLists()
        {
            foreach(CRM2011Connection c in Connections)
            {
                c.RetrieveEntityList();
            }
        }
    }
}
