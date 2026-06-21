using System.Text;
using PulseBoard.Infrastructure.Analytics;

namespace PulseBoard.Tests;

public class CsvExporterTests
{
    private readonly CsvExporter _csv = new();

    private string Render(byte[] bytes) =>
        Encoding.UTF8.GetString(bytes).TrimStart('﻿'); // strip BOM

    [Fact]
    public void Writes_Header_And_Rows()
    {
        var bytes = _csv.Write(["region", "value"], [["Europe", 1861634.48], ["APAC", 901842.17]]);
        var text = Render(bytes).Replace("\r\n", "\n").Trim();

        Assert.Equal("region,value\nEurope,1861634.48\nAPAC,901842.17", text);
    }

    [Fact]
    public void Escapes_Fields_With_Commas_And_Quotes()
    {
        var bytes = _csv.Write(["name"], [["Karlsson, Inc"], ["He said \"hi\""]]);
        var text = Render(bytes).Replace("\r\n", "\n");

        Assert.Contains("\"Karlsson, Inc\"", text);
        Assert.Contains("\"He said \"\"hi\"\"\"", text);
    }

    [Fact]
    public void Starts_With_Utf8_Bom()
    {
        var bytes = _csv.Write(["a"], [["x"]]);
        Assert.Equal([0xEF, 0xBB, 0xBF], bytes.Take(3).ToArray());
    }
}
