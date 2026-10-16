using CodeCafe.Application.Common;
using CodeCafe.Host.Mcp;
using ModelContextProtocol.Protocol;
using Result = CodeCafe.Application.Common.Result;

namespace CodeCafe.Host.Tests.Mcp;

public sealed class McpToolResultsTests
{
    [Fact]
    public void Failure_MarksTheCallAsError_AndExposesStructuredError()
    {
        var error = new Error(
            "duplicate_temp_id",
            "The temp id was already used by an earlier op in this batch.",
            ErrorKind.Validation);

        var result = McpToolResults.From(Result.Failure<object>(error));

        Assert.True(result.IsError);
        var structured = result.StructuredContent!.Value;
        Assert.Equal("Validation", structured.GetProperty("kind").GetString());
        Assert.Equal("duplicate_temp_id", structured.GetProperty("code").GetString());
        Assert.Equal(error.Message, structured.GetProperty("message").GetString());
    }

    [Fact]
    public void Failure_KeepsTheHumanReadableTextBlock()
    {
        var error = new Error("block_not_found", "The block was not found.", ErrorKind.NotFound);

        var result = McpToolResults.From(Result.Failure<object>(error));

        var text = Assert.IsType<TextContentBlock>(Assert.Single(result.Content));
        Assert.Equal("NotFound: block_not_found: The block was not found.", text.Text);
    }
}
