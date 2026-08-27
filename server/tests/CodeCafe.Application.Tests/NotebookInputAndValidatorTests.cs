using System.Text.Json;
using CodeCafe.Application.Notes;
using CodeCafe.Application.Notes.Commands.CreateNotebook;
using CodeCafe.Application.Notes.Commands.CreateNotebookItem;
using CodeCafe.Application.Notes.Commands.UpdateNotebook;
using CodeCafe.Application.Notes.Commands.UpdateNotebookItem;

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

    [Theory]
    [InlineData("public", true)]
    [InlineData("Private", true)]
    [InlineData("unlisted", true)]
    public void TryParseVisibility_Accepts_DefinedNames(string value, bool expected)
    {
        Assert.Equal(expected, NotebookInput.TryParseVisibility(value, out _));
    }

    [Theory]
    [InlineData("5")]
    [InlineData("99")]
    [InlineData("nonsense")]
    public void TryParseVisibility_Rejects_NumericOrUndefinedValues(string value)
    {
        Assert.False(NotebookInput.TryParseVisibility(value, out _));
    }

    [Theory]
    [InlineData("7")]
    [InlineData("42")]
    [InlineData("section")]
    public void TryParseItemType_Rejects_NumericOrUndefinedValues(string value)
    {
        Assert.False(NotebookInput.TryParseItemType(value, out _));
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
        var command = new UpdateNotebookCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Title",
            null,
            "secret"
        );

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void UpdateNotebookValidator_Rejects_Missing_Visibility()
    {
        var validator = new UpdateNotebookCommandValidator();
        var command = new UpdateNotebookCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Title",
            null,
            string.Empty
        );

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void CreateNotebookItemValidator_Rejects_Invalid_ItemType()
    {
        var validator = new CreateNotebookItemCommandValidator();
        var command = new CreateNotebookItemCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            "article",
            "Title",
            0,
            null
        );

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
            contentDocument.RootElement.Clone()
        );

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }
}
