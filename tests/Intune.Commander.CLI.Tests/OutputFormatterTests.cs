using Intune.Commander.CLI.Helpers;

namespace Intune.Commander.CLI.Tests;

public class OutputFormatterTests
{
    [Fact]
    public void SerializeJson_UsesCamelCase()
    {
        var json = OutputFormatter.SerializeJson(new { DisplayName = "Policy A", ItemCount = 2 });

        Assert.Contains("\"displayName\"", json);
        Assert.Contains("\"itemCount\"", json);
    }

    [Fact]
    public void WriteTable_WritesHeadersAndRows()
    {
        var writer = new StringWriter();
        var original = Console.Out;
        Console.SetOut(writer);

        try
        {
            OutputFormatter.WriteTable(["Name", "Id"], [["Policy A", "1"]]);
        }
        finally
        {
            Console.SetOut(original);
        }

        var output = writer.ToString();
        Assert.Contains("Name\tId", output);
        Assert.Contains("Policy A\t1", output);
    }
}
