using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Hyperscale.Common.Contracts.Exceptions;
using Hyperscale.Microcore.Interfaces.Configuration;
using Hyperscale.Microcore.Interfaces.Logging;
using Hyperscale.Microcore.Interfaces.SystemWrappers;
using Hyperscale.ServiceContract.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Hyperscale.Microcore.SharedLogic.Exceptions
{
    public class StackTraceEnhancer : IStackTraceEnhancer
    {
        private Func<StackTraceEnhancerSettings> GetConfig { get; }
        private static readonly JsonSerializer Serializer = new JsonSerializer
        {
            TypeNameHandling = TypeNameHandling.All,
            SerializationBinder = new ExceptionHierarchySerializationBinder(),
            Formatting = Formatting.Indented,
            DateParseHandling = DateParseHandling.DateTimeOffset,
            Converters = { new StripHttpRequestExceptionConverter() }
        };

        public StackTraceEnhancer(Func<StackTraceEnhancerSettings> getConfig)
        {
            GetConfig = getConfig;
        }

        public JObject ToJObjectWithBreadcrumb(Exception exception)
        {
            var jobject = JObject.FromObject(exception, Serializer);

            if (GetConfig().Enabled == false)
                return jobject;

            var breadcrumbTarget = jobject.Property("RemoteStackTraceString");
            breadcrumbTarget = breadcrumbTarget?.Value.Type == JTokenType.String ? breadcrumbTarget : jobject.Property("StackTraceString");

            if (breadcrumbTarget == null)
            {
                jobject.Add("StackTraceString", null);
                breadcrumbTarget = jobject.Property("StackTraceString");
            }

            var breadcrumb = new Breadcrumb
            {
                ServiceName = CurrentApplicationInfo.Name,
                ServiceVersion = CurrentApplicationInfo.Version.ToString(),
                HostName = CurrentApplicationInfo.HostName
            };

            if (exception is SerializableException serEx)
                serEx.AddBreadcrumb(breadcrumb);

            breadcrumbTarget.Value = $"\r\n--- End of stack trace from {breadcrumb} ---\r\n{breadcrumbTarget.Value}";

            return jobject;
        }

        public string Clean(string stackTrace)
        {
            var config = GetConfig();

            if (config.Enabled == false || string.IsNullOrEmpty(stackTrace))
                return stackTrace;

            var replacements = config.RegexReplacements.Values.ToArray();
            var frames = stackTrace
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(f => f.StartsWith("   at System.Runtime") == false && f.StartsWith("Exception rethrown at ") == false && f.StartsWith("Server stack trace:") == false && f != "--- End of stack trace from previous location where exception was thrown ---")
                .Select(f => ApplyRegexs(f, replacements));

            return string.Join("\r\n", frames);
        }

        private string ApplyRegexs(string frame, RegexReplace[] replacements)
        {
            string updatedFrame = frame;

            foreach (var regexReplace in replacements)
                updatedFrame = Regex.Replace(updatedFrame, regexReplace.Pattern, regexReplace.Replacement);

            return updatedFrame;
        }
    }

    public class StackTraceEnhancerSettings : IConfigObject
    {
        public bool Enabled { get; set; } = true;
        public Dictionary<string, RegexReplace> RegexReplacements { get; set; } = new Dictionary<string, RegexReplace>();
    }

    public class RegexReplace
    {
        public string Pattern { get; set; }
        public string Replacement { get; set; }
    }
}