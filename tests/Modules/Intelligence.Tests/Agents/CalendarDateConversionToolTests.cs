using System.Text.Json;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Intelligence.Tests.Agents;

public sealed class CalendarDateConversionToolTests
{
    private readonly CalendarDateConversionTool _tool = new();
    private readonly AgentExecutionContext _context = new("test", null);

    [Theory]
    [InlineData("2026-07-30", "1405-05-08")]
    [InlineData("2024-03-20", "1403-01-01")]
    public async Task ConvertsGregorianToPersian(string gregorian, string expectedPersian)
    {
        AgentToolResult result = await ExecuteAsync(gregorian, "gregorian");

        using JsonDocument output = JsonDocument.Parse(result.Output);
        Assert.False(result.IsError);
        Assert.Equal(gregorian, output.RootElement.GetProperty("gregorian").GetProperty("date").GetString());
        Assert.Equal(expectedPersian, output.RootElement.GetProperty("persian").GetProperty("date").GetString());
    }

    [Theory]
    [InlineData("۱۴۰۵-۰۵-۰۸", "2026-07-30")]
    [InlineData("1403-01-01", "2024-03-20")]
    public async Task ConvertsPersianToGregorian(string persian, string expectedGregorian)
    {
        AgentToolResult result = await ExecuteAsync(persian, "persian");

        using JsonDocument output = JsonDocument.Parse(result.Output);
        Assert.False(result.IsError);
        Assert.Equal(expectedGregorian, output.RootElement.GetProperty("gregorian").GetProperty("date").GetString());
    }

    [Fact]
    public async Task RejectsInvalidPersianDate()
    {
        AgentToolResult result = await ExecuteAsync("1402-12-30", "persian");

        using JsonDocument output = JsonDocument.Parse(result.Output);
        Assert.True(result.IsError);
        Assert.Equal("invalid_calendar_date", output.RootElement.GetProperty("error").GetProperty("code").GetString());
    }

    private Task<AgentToolResult> ExecuteAsync(string date, string sourceCalendar) =>
        _tool.ExecuteAsync(
            new AgentToolCall("call", "convert_calendar_date", JsonSerializer.Serialize(new { date, sourceCalendar })),
            _context);
}
