using Hyperscale.Microcore.Interfaces.Events;
using Hyperscale.Microcore.Interfaces.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hyperscale.Microcore.Logging.Serilog
{
    /// <summary>
    /// Publishes distributed tracing events to the log. Should only be used when no other event publishing system is available.
    /// </summary>
    public class LogEventPublisher : IEventPublisher
    {
        private ILog Log { get; }
        private IEventSerializer Serializer { get; }

        public LogEventPublisher(ILog log, IEventSerializer serializer)
        {
            Log = log;
            Serializer = serializer;
        }

        public PublishingTasks TryPublish(IEvent evt)
        {
            var fields = Serializer.Serialize(evt);

            Log.Debug(l => l("Tracing event",
                unencryptedTags: fields.Where(_ => !_.Attribute.Encrypt).Select(_ => new KeyValuePair<string, object>(_.Name, _.Value)),
                encryptedTags: fields.Where(_ => _.Attribute.Encrypt).Select(_ => new KeyValuePair<string, object>(_.Name, _.Value))));

            return new PublishingTasks
            {
                PublishEvent = Task.FromResult(true),
                PublishAudit = Task.FromResult(false),
                SerializedEventFields = fields,
            };
        }

    }
}
