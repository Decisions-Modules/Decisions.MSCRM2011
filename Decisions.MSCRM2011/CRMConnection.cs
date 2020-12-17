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
using DecisionsFramework.ServiceLayer.Services.Folder;
using DecisionsFramework.ServiceLayer.Utilities;
using DecisionsFramework.Utilities.CodeGeneration;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Decisions.MSCRM2011
{
    [Writable]
    [ORMEntity]
    [ValidationRules]
    public class CRM2011Connection : BaseORMEntity //: INotifyPropertyChanged
    {
        #region Fields
        [WritableValue]
        [ORMPrimaryKeyField]
        [PropertyHidden]
        public string connectionId;


        [WritableValue]
        [ORMField] // todo: this needs to be unique.
        private string connectionName;
        
        [WritableValue]
        [ORMField]
        private string organisationUrl;

        [WritableValue]
        [ORMField]
        private string domain;

        [WritableValue]
        [ORMField]
        private string userName;

        [WritableValue]
        [ORMField]
        private string password;

        [WritableValue]
        [ORMField(typeof(ORMXmlSerializedFieldConverter))]
        private string[] allEntityNames;

        [WritableValue]
        [ORMField(typeof(ORMXmlSerializedFieldConverter))]
        private string[] allEntityDisplayNames;

        #endregion
        #region Properties

        [PropertyClassification("Connection Name", 0)]
        [RequiredProperty("Connection Name is required.")]
        public string ConnectionName
        {
            get
            {
                return connectionName;
            }
            set
            {
                connectionName = value;
            }
        }

        [PropertyClassification("Organisation Url", 1)]
        [RequiredProperty("Organisation Url is required.")]
        public string OrganisationUrl
        {
            get
            {
                return organisationUrl;
            }
            set
            {
                organisationUrl = value;
            }
        }

        [PropertyClassification("Domain", 2)]
        [RequiredProperty("Domain is required.")]
        public string Domain
        {
            get
            {
                return domain;
            }
            set
            {
                domain = value;
            }
        }

        [PropertyClassification("Username", 3)]
        [RequiredProperty("Username is required.")]
        public string UserName
        {
            get
            {
                return userName;
            }
            set
            {
                userName = value;
            }
        }
        [PropertyClassification("Password", 4)]
        [RequiredProperty("Password is required.")]
        [PasswordText]
        [ExcludeInDescription]
        public string Password
        {
            get
            {
                return password;
            }
            set
            {
                password = value;
            }
        }

        [PropertyHidden]
        public string[] AllEntityNames
        {
            get
            {
                return allEntityNames;
            }
            set
            {
                allEntityNames = value;
            }
        }

        [PropertyHidden]
        public string[] AllEntityDisplayNames
        {
            get
            {
                return allEntityDisplayNames;
            }
            set
            {
                allEntityDisplayNames = value;
            }
        }

        #endregion
        internal string GetConnectionString()
        {
            return string.Format("Url={0}; domain={1}; Username={2}; Password={3}; RequireNewInstance=True;", OrganisationUrl, Domain, UserName, Password);
        }

        internal static CRM2011Connection GetCRMConnectionById(string connectionId)
        {
            CRM2011Connection connection = ModuleSettingsAccessor<CRM2011Settings>.Instance.Connections.FirstOrDefault(x => x.connectionId == connectionId);
            return connection;
        }

        internal static CRM2011Connection GetCRMConnectionForName(string connectionName)
        {
            CRM2011Connection connection = ModuleSettingsAccessor<CRM2011Settings>.Instance.Connections.FirstOrDefault(x => x.connectionName == connectionName);
            return connection;
        }

        public override string ToString()
        {
            string cName = ConnectionName ?? "(no name)";
            string uName = UserName ?? "(no user)";
            string url = OrganisationUrl ?? "(no url)";
            return string.Format("{0}  ({1} at {2})", cName, uName, url);
        }

        public override void BeforeSave()
        {
            CRM2011Connection otherConn = ModuleSettingsAccessor<CRM2011Settings>.Instance.Connections
                .FirstOrDefault(x => x.connectionId != this.connectionId && x.connectionName == this.connectionName);
            if (otherConn != null) throw new InvalidOperationException("Another connection already exists with this name, please choose another name.");
            base.BeforeSave();
            RetrieveEntityList();
        }

        internal static IOrganizationService GetServiceProxy(string connectionString)
        {
            Microsoft.Xrm.Client.CrmConnection connection = Microsoft.Xrm.Client.CrmConnection.Parse(connectionString);
            return new Microsoft.Xrm.Client.Services.OrganizationService(connection);
        }

        public void RetrieveEntityList()
        {
            try
            {
                Log log = new Log("CRMConnection");
                string trimmedConnectionString = GetConnectionString();
                trimmedConnectionString = trimmedConnectionString.Substring(0, trimmedConnectionString.IndexOf("Password"));
                log.Debug($"Making connection for '{this.connectionName}' ({this.connectionId}) with connection info \"{trimmedConnectionString}\".");

                IOrganizationService serviceProxy = CRM2011Connection.GetServiceProxy(GetConnectionString());

                if (serviceProxy != null)
                {
                    RetrieveAllEntitiesRequest req = new RetrieveAllEntitiesRequest()
                    {
                        EntityFilters = EntityFilters.Entity
                    };
                    RetrieveAllEntitiesResponse res = (RetrieveAllEntitiesResponse)serviceProxy.Execute(req);
                    this.allEntityNames = res.EntityMetadata.Select(x => x.LogicalName).ToArray();
                    this.allEntityDisplayNames = res.EntityMetadata.Select(x =>
                    {
                        string displayName;
                        if (x.DisplayName.LocalizedLabels != null && x.DisplayName.LocalizedLabels.Count > 0)
                        {
                            displayName = x.DisplayName.LocalizedLabels[0].Label;
                        }
                        else displayName = x.LogicalName;
                        return displayName;
                    }).ToArray();
                    /*log.Debug($"Checksum for all entity names: {allEntityNames.SelectMany(x => x).Select(x => (int)x).Sum()}");
                    log.Debug($"All entity names: {string.Join(", ", allEntityNames)}");*/
                }
            }
            catch(System.IO.FileNotFoundException)
            {
                // FileNotFound is sometimes thrown from within Microsoft.Xrm.Tooling.Connector, probably when the connection info is invalid.
                this.allEntityNames = null;
                this.allEntityDisplayNames = null;
                throw new InvalidOperationException("Connection could not be made - check connection info or retry.");
            }
        }

        internal string GetCrmConnectionFolderId()
        {
            if (connectionId == null) throw new InvalidOperationException("This connection has no ID and cannot be used to create a folder.");
            return CRM2011Entity.CRM_GENERATED_TYPES_FOLDER_ID + connectionId;
        }

        internal void EnsureCrmConnectionFolderExists()
        {
            if (connectionId == null) throw new InvalidOperationException("Cannot create a folder for an incomplete connection.");
            ORM<Folder> folderORM = new ORM<Folder>();
            string crmConnectionFolderId = GetCrmConnectionFolderId();
            Folder crmEntityFolder = folderORM.Fetch(crmConnectionFolderId);
            if (crmEntityFolder == null)
            {
                crmEntityFolder = new Folder(crmConnectionFolderId, connectionName, CRM2011Entity.CRM_GENERATED_TYPES_FOLDER_ID);
                folderORM.Store(crmEntityFolder);
            }
        }
    }
}
