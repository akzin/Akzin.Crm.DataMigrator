using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Newtonsoft.Json.Linq;
using Akzin.Crm.DataMigrator.Strategy;

namespace Akzin.Crm.DataMigrator.Migration
{
    public class EntityAndJsonConverter
    {
        private readonly IEntityStrategy strategy;

        public EntityAndJsonConverter(IEntityStrategy strategy)
        {
            this.strategy = strategy;
        }

        public Dictionary<string, object> ToJson(Entity entity)
        {
            var dict = new Dictionary<string, object>();

            foreach (var columnLogicalName in strategy.Columns)
            {
                object value = null;
                if (entity.Attributes.Contains(columnLogicalName))
                {
                    value = ToJsonValue(entity.Attributes[columnLogicalName]);
                }

                dict[columnLogicalName] = value;
            }

            return dict;
        }

        private object ToJsonValue(object o)
        {
            switch (o)
            {
                case Guid guid:
                    return guid.ToString("D");
                case OptionSetValue optionSetValue:
                    return optionSetValue.Value;
                case DateTime dateTime:
                    {
                        return dateTime.ToString("u");
                    }

                case Money money:
                    return money.Value;
                case EntityReference reference:
                    {
                        var dict = new Dictionary<string, object>
                        {
                            ["entity"] = reference.LogicalName,
                            ["id"] = reference.Id,
                            ["name"] = reference.Name
                        };

                        return dict;
                    }

                case string _:
                case bool _:
                case int _:
                case decimal _:
                case double _:
                case long _:
                    return o;
            }

            return o;
        }


        public Entity ToEntity(Dictionary<string, object> jsEntity)
        {
            if (strategy.PrimaryIdAttribute == null)
            {
                var jsonEntity = new Entity(strategy.EntityLogicalName);

                foreach (var attribute in jsEntity)
                {
                    jsonEntity.Attributes[attribute.Key] = ConvertToCrm(attribute);
                }

                return jsonEntity;
            }
            else
            {
                var idName = jsEntity.Keys.First();
                var id = Guid.Parse((string)jsEntity[idName]);

                var jsonEntity = new Entity(strategy.EntityLogicalName, id)
                {
                    Attributes = {[strategy.PrimaryIdAttribute] = id}
                };

                foreach (var attribute in jsEntity.Skip(1))
                {
                    jsonEntity.Attributes[attribute.Key] = ConvertToCrm(attribute);
                }

                return jsonEntity;
            }
        }

        private object ConvertToCrm(KeyValuePair<string, object> attribute)
        {
            var attributeMetadata = strategy.GetAttribute(attribute.Key);
            var value = attribute.Value;

            switch (attributeMetadata.AttributeType)
            {
                case AttributeTypeCode.BigInt:
                case AttributeTypeCode.String:
                case AttributeTypeCode.Virtual:
                case AttributeTypeCode.EntityName:
                case AttributeTypeCode.Boolean:

                case AttributeTypeCode.Memo:
                    return attribute.Value;
                case AttributeTypeCode.DateTime:
                    if (attribute.Value is string dateString)
                    {
                        var date = DateTime.Parse(dateString).ToUniversalTime();
                        return date;
                    }
                    return attribute.Value;
                case AttributeTypeCode.State:
                case AttributeTypeCode.Status:
                    return new OptionSetValue((int)(long)attribute.Value);
                case AttributeTypeCode.Integer:
                    return (int?)(long?)attribute.Value;
                case AttributeTypeCode.Uniqueidentifier:
                    if (attribute.Value != null)
                    {
                        return Guid.Parse((string)attribute.Value);
                    }
                    return null;
                case AttributeTypeCode.Picklist:
                    if (attribute.Value != null)
                    {
                        var i = (int)(long)attribute.Value;
                        return new OptionSetValue(i);
                    }
                    return attribute.Value;
                case AttributeTypeCode.Decimal:
                case AttributeTypeCode.Money:
                    switch (attribute.Value)
                    {
                        case int attributeValue:
                            return (decimal)attributeValue;
                        case double d:
                            return (decimal)d;
                        default:
                            if (attribute.Value != null)
                                throw new NotImplementedException();
                            return null;
                    }
                case AttributeTypeCode.Double:
                    if (attribute.Value is double)
                        return attribute.Value;

                    if (attribute.Value != null)
                        throw new NotImplementedException();
                    return null;
                case AttributeTypeCode.Lookup:
                case AttributeTypeCode.Owner:
                    if (value == null)
                        return null;
                    var jobject = (JObject)value;
                    var logicalName = (string)jobject["entity"];
                    var name = (string)jobject["name"];
                    var id = Guid.Parse((string)jobject["id"]);
                    return new EntityReference { LogicalName = logicalName, Id = id, Name = name };
            }
            return value;
        }
    }
}