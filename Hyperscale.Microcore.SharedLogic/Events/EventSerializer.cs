using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

using Hyperscale.Common.Contracts.Exceptions;
using Hyperscale.Microcore.Interfaces.Configuration;
using Hyperscale.Microcore.Interfaces.Events;
using Hyperscale.Microcore.Interfaces.Logging;
using Hyperscale.Microcore.Interfaces.SystemWrappers;

namespace Hyperscale.Microcore.SharedLogic.Events
{

    public class EventSerializer: IEventSerializer
    {
        [DebuggerDisplay("{" + nameof(Name) + "}")]
        private class MemberToSerialize
        {
            public string Name;
            public FieldInfo Field;
            public PropertyInfo Property;
            public EventFieldAttribute Attribute;
        }


        private Func<EventConfiguration> LoggingConfigFactory { get; }
        private IStackTraceEnhancer StackTraceEnhancer { get; }
        private Func<EventConfiguration> EventConfig { get; }


        public EventSerializer(Func<EventConfiguration> loggingConfigFactory, IStackTraceEnhancer stackTraceEnhancer, Func<EventConfiguration> eventConfig)
        {
            LoggingConfigFactory = loggingConfigFactory;
            StackTraceEnhancer = stackTraceEnhancer;
            EventConfig = eventConfig;
        }



        public IEnumerable<SerializedEventField> Serialize(IEvent evt, Func<EventFieldAttribute, bool> predicate = null)
        {
            evt.Configuration = LoggingConfigFactory();
            evt.StackTraceEnhancer = StackTraceEnhancer;

            foreach (var member in GetMembersToSerialize(evt.GetType()))
                if (predicate == null || predicate(member.Attribute) == true)
                    foreach (var field in SerializeEventFieldAndSubfields(evt, member))
                        yield return field;
        }



        /// <summary>The list of members in this <see cref="Event"/> that are decorated with <see cref="EventFieldAttribute"/>.
        /// Initialized once; saves us doing expensive reflection per event.</summary>
        private ConcurrentDictionary<Type, List<MemberToSerialize>> membersToSerialize = new ConcurrentDictionary<Type, List<MemberToSerialize>>();


        List<MemberToSerialize> GetMembersToSerialize(Type t)
        {
            List<MemberToSerialize> res;
            if (membersToSerialize.TryGetValue(t, out res))
                return res;
            else
            {
                res = new List<MemberToSerialize>();
                var members = t.GetMembers(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.GetField | BindingFlags.GetProperty);

                foreach (var member in members)
                {
                    var fieldAttr = (EventFieldAttribute)member.GetCustomAttribute(typeof(EventFieldAttribute), true);
                    if (fieldAttr != null)
                        res.Add(new MemberToSerialize
                        {
                            Name = member.Name,
                            Field = member as FieldInfo,
                            Property = member as PropertyInfo,
                            Attribute = fieldAttr,
                        });
                }

                membersToSerialize.TryAdd(t, res);
                return res;
            }
        }



        IEnumerable<SerializedEventField> SerializeEventFieldAndSubfields(IEvent evt, MemberToSerialize member)
        {
            if (member.Attribute.Encrypt && member.Attribute.AppendTypeSuffix)
                throw new ProgrammaticException($"Event field '{evt.GetType().FullName}.{member.Name}' cannot be marked both as {nameof(member.Attribute.Encrypt)} and {nameof(member.Attribute.AppendTypeSuffix)}");

            // Get field/property value; can throw!
            object value = member.Field != null ? member.Field.GetValue(evt) : member.Property.GetValue(evt);

            // Some values should be treated as nulls
            if (IsEmpty(value))
                value = null;

            // If we got no value, use a default if provided
            if (value == null && member.Attribute.DefaultValue != null && !IsEmpty(member.Attribute.DefaultValue))
                value = member.Attribute.DefaultValue;

            // If we got a value (or default), expand it and write it to flume and/or audit events
            if (value != null)
                foreach (var kvp in SerializeSubFields(member.Attribute.Name, member.Attribute.AppendTypeSuffix, value))
                    if (!IsEmpty(kvp.Value))
                        yield return new SerializedEventField
                        {
                            Name          = kvp.Key,
                            Value         = !member.Attribute.TruncateIfLong ? kvp.Value : kvp.Value.Substring(0, Math.Min(kvp.Value.Length, EventConfig().ParamTruncateLength)),
                            Attribute     = member.Attribute,
                            ShouldEncrypt = member.Attribute.Encrypt,
                        };
        }



        static IEnumerable<KeyValuePair<string, string>> SerializeSubFields(string key, bool appendTypeSuffix, object value)
        {
            if (value is IEnumerable<KeyValuePair<string, string>>)
                foreach (var kvp in ((IEnumerable<KeyValuePair<string, string>>)value).Where(_ => _.Value != null))
                    yield return new KeyValuePair<string, string>(key + '.' + kvp.Key, kvp.Value);

            else if (value is IEnumerable<KeyValuePair<string, object>>)
                foreach (var kvp in ((IEnumerable<KeyValuePair<string, object>>)value).Where(_ => _.Value != null))
                {
                    string subValue, typeSuffix = string.Empty;
                    if (appendTypeSuffix)
                        EventFieldFormatter.SerializeFieldValueAndTypeSuffix(kvp.Value, out subValue, out typeSuffix);
                    else subValue = EventFieldFormatter.SerializeFieldValue(kvp.Value);
                    yield return new KeyValuePair<string, string>(key + '.' + kvp.Key + typeSuffix, subValue);
                }

            else yield return new KeyValuePair<string, string>(key, EventFieldFormatter.SerializeFieldValue(value));
        }



        private static bool IsEmpty(object val)
        {
            if (null == val) return true;
            return val is string && string.IsNullOrEmpty(val as string);
        }

    }
}
