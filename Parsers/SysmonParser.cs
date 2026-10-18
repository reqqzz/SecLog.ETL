using System.Globalization;
using System.Xml.Linq;
using SecLog.ETL.Models;

namespace SecLog.ETL.Parsers;

public class SysmonParser
{
    private static readonly XNamespace Ns = "http://schemas.microsoft.com/win/2004/08/events/event";

    public EcsEvent? Parse(string xmlContent)
    {
        try
        {
            var doc = XDocument.Parse(xmlContent);
            var root = doc.Root;
            if (root == null) return null;

            var system = root.Element(Ns + "System");
            var eventData = root.Element(Ns + "EventData");
            if (system == null || eventData == null) return null;

            var eventId = system.Element(Ns + "EventID")?.Value ?? "0";
            var data = eventData.Elements(Ns + "Data")
                .Where(e => e.Attribute("Name") != null)
                .ToDictionary(
                    e => e.Attribute("Name")!.Value,
                    e => e.Value);

            var ecs = new EcsEvent
            {
                Event = new EcsEventDetails { Code = eventId, Module = "Sysmon" },
                Host = new EcsHost
                {
                    Name = system.Element(Ns + "Computer")?.Value ?? Environment.MachineName
                }
            };

            if (data.TryGetValue("UtcTime", out var utcTime) &&
                DateTime.TryParse(utcTime, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var timestamp))
            {
                ecs.Timestamp = timestamp;
            }

            if (eventId == "1")
            {
                ecs.Process = new EcsProcess
                {
                    Executable = data.GetValueOrDefault("Image"),
                    CommandLine = data.GetValueOrDefault("CommandLine"),
                    ParentExecutable = data.GetValueOrDefault("ParentImage")
                };
            }

            return ecs;
        }
        catch
        {
            return null;
        }
    }
}