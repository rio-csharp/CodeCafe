using CodeCafe.Application.Notes;
using CodeCafe.Application.Notes.Commands.CreateNotebook;
using CodeCafe.Application.Notes.Commands.CreateNotebookItem;
using CodeCafe.Application.Notes.Commands.UpdateNotebook;
using CodeCafe.Application.Notes.Commands.UpdateNotebookItem;
using System.Text.Json;

namespace CodeCafe.Application.Tests;

public sealed class NotebookInputAndValidatorTests
{
    [Fact]
    public void NormalizeOptionalText_Trims_And_Nulls_Whitespace()
    {
        Assert.Equal("hello", NotebookInput.NormalizeOptionalText("  hello  "));
        Assert.Null(NotebookInput.NormalizeOptionalText("   "));
    }

    [Fact]
    public void TryParseOptionalGuid_Accepts_Null_And_Guid_String()
    {
        Guid? guid;
        using var nullDocument = JsonDocument.Parse("null");
        Assert.True(NotebookInput.TryParseOptionalGuid(nullDocument.RootElement, out guid));
        Assert.Null(guid);

        var expected = Guid.NewGuid();
        using var guidDocument = JsonDocument.Parse($"\"{expected}\"");
        Assert.True(NotebookInput.TryParseOptionalGuid(guidDocument.RootElement, out guid));
        Assert.Equal(expected, guid);
    }

    [Fact]
    public void TryParseOptionalGuid_Rejects_Invalid_Value()
    {
        using var invalidDocument = JsonDocument.Parse("123");

        Assert.False(NotebookInput.TryParseOptionalGuid(invalidDocument.RootElement, out _));
    }

    [Fact]
    public void CreateNotebookValidator_Rejects_Invalid_Visibility()
    {
        var validator = new CreateNotebookCommandValidator();
        var command = new CreateNotebookCommand(Guid.NewGuid(), "Title", null, "secret");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void UpdateNotebookValidator_Rejects_Invalid_Visibility()
    {
        var validator = new UpdateNotebookCommandValidator();
        var command = new UpdateNotebookCommand(Guid.NewGuid(), Guid.NewGuid(), "Title", null, "secret");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void CreateNotebookItemValidator_Rejects_Invalid_ItemType()
    {
        var validator = new CreateNotebookItemCommandValidator();
        var command = new CreateNotebookItemCommand(Guid.NewGuid(), Guid.NewGuid(), null, "article", "Title", 0, null);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void UpdateNotebookItemValidator_Rejects_Invalid_ParentId()
    {
        var validator = new UpdateNotebookItemCommandValidator();
        using var parentDocument = JsonDocument.Parse("123");
        using var contentDocument = JsonDocument.Parse("null");
        var command = new UpdateNotebookItemCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Title",
            parentDocument.RootElement.Clone(),
            null,
            contentDocument.RootElement.Clone());

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }
}
