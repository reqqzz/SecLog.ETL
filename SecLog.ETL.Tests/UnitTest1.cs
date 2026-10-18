using Xunit;
using SecLog.ETL.Parsers;

namespace SecLog.ETL.Tests;

public class SysmonParserTests
{
    [Fact]
    public void Parse_ValidSysmonEvent1_ReturnsEcsEvent()
    {
        var xml = @"<Event xmlns=""http://schemas.microsoft.com/win/2004/08/events/event"">
                      <System><EventID>1</EventID><Computer>TEST-PC</Computer></System>
                      <EventData>
                        <Data Name=""Image"">C:\test.exe</Data>
                        <Data Name=""CommandLine"">test.exe -run</Data>
                      </EventData>
                    </Event>";
        var parser = new SysmonParser();

        var result = parser.Parse(xml);

        Assert.NotNull(result);
        Assert.Equal("1", result.Event.Code);
        Assert.Equal("TEST-PC", result.Host.Name);
        Assert.Equal(@"C:\test.exe", result.Process?.Executable);
    }

    [Fact]
    public void Parse_InvalidXml_ReturnsNull()
    {
        var parser = new SysmonParser();
        var result = parser.Parse("not a valid xml");
        Assert.Null(result);
    }
}