using DecisionsFramework;
using DecisionsFramework.Data.ORMapper;
using DecisionsFramework.Design.Flow.Service;
using DecisionsFramework.ServiceLayer;
using DecisionsFramework.ServiceLayer.Actions;
using DecisionsFramework.ServiceLayer.Actions.Common;
using DecisionsFramework.ServiceLayer.Services.Folder;
using DecisionsFramework.ServiceLayer.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Decisions.MSCRM2011
{
   public class CRM2011FolderBehavior :  SystemFolderBehavior
    {
        public override BaseActionType[] GetFolderActions(Folder folder, BaseActionType[] proposedActions, EntityActionType[] types)
        {
            List<BaseActionType> list = new List<BaseActionType>(base.GetFolderActions(folder, proposedActions, types) ?? new BaseActionType[0]);
            list.Add(new EditObjectAction(typeof(CRM2011Entity), "Add CRM Entity", "", "", null,
                                                  new CRM2011Entity() { EntityFolderID = folder.FolderID },
                                                  new SetValueDelegate(AddCRMEntity))
            {
                ActionAddsType = typeof(CRM2011Entity),
                RefreshScope = ActionRefreshScope.OwningFolder
            });
            return list.ToArray();
        }


        private void AddCRMEntity(AbstractUserContext usercontext, object obj)
        {
            CRM2011Entity field = (CRM2011Entity)obj;

            new DynamicORM().Store(field);
        }

    }

    public class CRM2011StepsInitializer : IInitializable
    {
        const string CRM_LIST_FOLDER_ID = "CRM_2011_ENTITY_FOLDER";
        const string CRM_PAGE_ID = "6ba31c2e-715d-4d1c-b4c8-7e39f194c258";

        public void Initialize()
        {
            ORM<Folder> orm = new ORM<Folder>();
            Folder folder = (Folder)orm.Fetch(typeof(Folder), CRM_LIST_FOLDER_ID);
            if (folder == null)
            {

                Log log = new Log("MSCRM 2011 Folder Behavior");
                log.Debug("Creating System Folder '" + CRM_LIST_FOLDER_ID + "'");
                folder = new Folder(CRM_LIST_FOLDER_ID, "MSCRM 2011", Constants.INTEGRATIONS_FOLDER_ID);
                folder.FolderBehaviorType = typeof(CRM2011FolderBehavior).FullName;

                orm.Store(folder);
            }

            ORM<PageData> pageDataOrm = new ORM<PageData>();
            PageData pageData = pageDataOrm.Fetch(new WhereCondition[] {
                new FieldWhereCondition("configuration_storage_id", QueryMatchType.Equals, CRM_PAGE_ID),
                new FieldWhereCondition("entity_folder_id", QueryMatchType.Equals, CRM_LIST_FOLDER_ID)
            }).FirstOrDefault();

            if(pageData == null)
            {
                pageData = new PageData {
                    EntityFolderID = CRM_LIST_FOLDER_ID,
                    ConfigurationStorageID = CRM_PAGE_ID,
                    EntityName = "MSCRM 2011 Entities",
                    Order = -1
                };
                pageDataOrm.Store(pageData);
            }

            // This generated types folder will be shared with the 2016 module:
            Folder typesFolder = orm.Fetch(CRM2011Entity.CRM_GENERATED_TYPES_FOLDER_ID);
            if(typesFolder == null)
            {
                Log log = new Log("MSCRM 2011 Folder Behavior");
                log.Debug("Creating System Folder '" + CRM2011Entity.CRM_GENERATED_TYPES_FOLDER_ID + "'");
                typesFolder = new Folder(CRM2011Entity.CRM_GENERATED_TYPES_FOLDER_ID, "MSCRM", Constants.DATA_STRUCTURES_FOLDER_ID);
                orm.Store(typesFolder);
            }

            FlowEditService.RegisterModuleBasedFlowStepFactory(new CRM2011StepsFactory());


        }
    }
}
