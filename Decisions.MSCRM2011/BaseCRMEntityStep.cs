using DecisionsFramework;
using DecisionsFramework.Data.ORMapper;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow.CoreSteps;
using DecisionsFramework.Design.Flow.Interface;
using DecisionsFramework.Design.Flow.Service;
using DecisionsFramework.Design.Properties;
using DecisionsFramework.Utilities;
using Microsoft.Xrm.Client.Services;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Decisions.MSCRM2011
{
    [Writable]
    public abstract class BaseCRMEntityStep : BaseFlowAwareStep, IAddedToFlow
    {
        protected static Log log = new Log("BaseCRMEntityStep");

        [WritableValue]
        private string entityId;

        [PropertyHidden]
        public string EntityId
        {
            get
            {
                return this.entityId;
            }
            set
            {
                this.entityId = value;
            }
        }

        [PropertyHidden]
        public CRMEntityField[] CRMEntityFields
        {
            get
            {
                if (CRMEntity != null)
                {
                    return CRMEntity.CRMEntityFields;
                }
                return null;
            }
        }

        private CRM2011Entity crmEntity;

        [PropertyHidden]
        public CRM2011Entity CRMEntity
        {
            get
            {
                if (crmEntity == null)
                {
                    ORM<CRM2011Entity> orm = new ORM<CRM2011Entity>();
                    crmEntity = orm.Fetch(EntityId);
                }
                return crmEntity;
            }
        }

        public abstract string StepName { get; }

        public Type GetMSCRMType()
        {
            return TypeUtilities.FindTypeByFullName(CRMEntity.GetFullTypeName());
        }

        protected string GetConnectionString() => CRMEntity.Connection.GetConnectionString();

        protected IOrganizationService GetServiceProxy()
        {
            return CRM2011Connection.GetServiceProxy(GetConnectionString());
        }

        public object CreateObjectFromAttributes(Type type, AttributeCollection attributes)
        {
            var obj = Activator.CreateInstance(type);
            foreach (var attribute in attributes)
            {
                PropertyInfo pinfo = type.GetProperty(attribute.Key);
                if (pinfo != null)
                {
                    object attributeValueObj;
                    if (attribute.Value is OptionSetValue)
                    {
                        int i = ((OptionSetValue)attribute.Value).Value;
                        Type enumType = pinfo.PropertyType.IsNullable() ?
                            Nullable.GetUnderlyingType(pinfo.PropertyType)
                            : pinfo.PropertyType;
                        attributeValueObj = Enum.ToObject(enumType, i);
                    }
                    else if (attribute.Value is Money)
                    {
                        attributeValueObj = ((Money)attribute.Value).Value;
                    }
                    else if (attribute.Value is EntityReference)
                    {
                        EntityReference lookFieldValue = attribute.Value as EntityReference;
                        attributeValueObj = new CRM2011LookUpTypeField() { LookUpEntityName = lookFieldValue.LogicalName, Id = lookFieldValue.Id.ToString() };
                    }
                    else
                    {
                        attributeValueObj = attribute.Value;
                    }

                    pinfo.SetValue(obj, attributeValueObj);

                    // Then wrap it in Nullable<> if necessary:
                    /*object propValueObj;

                    if (pinfo.PropertyType.IsNullable())
                    {
                        propValueObj = Activator.CreateInstance(pinfo.PropertyType);
                        PropertyInfo pinfoValue = pinfo.PropertyType.GetProperty("Value");
                        pinfoValue.SetValue(propValueObj, attributeValueObj);
                    }
                    else
                    {
                        propValueObj = attributeValueObj;
                    }

                    pinfo.SetValue(obj, propValueObj);*/

                    /*object valueObj;
                    if (type.IsNullable()) {
                        Type nullableType = typeof(Nullable<>).MakeGenericType(type);
                        valueObj = Activator.CreateInstance(nullableType);
                        PropertyInfo pinfoValue = nullableType.GetProperty("Value");
                    }
                    if (attribute.Value is OptionSetValue) {
                        Enum.ToObject
                        pinfo.SetValue(obj, (type)((OptionSetValue)attribute.Value).Value);
                    }
                    else if (attribute.Value is Money) {
                        pinfo.SetValue(obj, ((Money)attribute.Value).Value);
                    }
                    else if (attribute.Value is EntityReference)
                    {
                        EntityReference lookFieldValue = attribute.Value as EntityReference;
                        pinfo.SetValue(obj, new CRMLookUpTypeField() { LookUpEntityName = lookFieldValue.LogicalName, Id = lookFieldValue.Id.ToString() });
                    }
                    else {
                        pinfo.SetValue(obj, attribute.Value);
                    }
                    pinfo.SetValue(obj, valueObj);*/
                }
            }
            return obj;
        }

        public void AddedToFlow()
        {
            if ((this.Flow != null) && (this.FlowStep != null))
                FlowEditService.SetDefaultStepName(this.Flow, this.FlowStep, StringUtils.SplitCamelCaseString(StepName) + " {0}");
        }

        public void RemovedFromFlow()
        {

        }
    }
}
