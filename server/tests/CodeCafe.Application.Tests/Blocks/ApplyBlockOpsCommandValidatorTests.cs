using System.Text.Json;
using CodeCafe.Application.Blocks.ApplyBlockOps;
using CodeCafe.Application.Blocks.Shared;

namespace CodeCafe.Application.Tests.Blocks;

public sealed class ApplyBlockOpsCommandValidatorTests
{
    private readonly ApplyBlockOpsCommandValidator _validator = new();

    [Fact]
    public void Valid_Batch_Passes()
    {
        var result = _validator.Validate(
            new ApplyBlockOpsCommand(
                Guid.NewGuid(),
                [
                    Insert(tempId: "t1"),
                    new BlockOp(BlockOpKind.Update, BlockId: "t1", TempId: null, Type: null, After: null, Content: Content(), BaseRevision: 1),
                    new BlockOp(BlockOpKind.Move, BlockId: Guid.NewGuid().ToString(), TempId: null, Type: null, After: "t1", Content: null, BaseRevision: null),
                    new BlockOp(BlockOpKind.Delete, BlockId: Guid.NewGuid().ToString(), TempId: null, Type: null, After: null, Content: null, BaseRevision: null),
                ]
            )
        );

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Update_WithoutBaseRevision_Fails()
    {
        var result = _validator.Validate(
            new ApplyBlockOpsCommand(
                Guid.NewGuid(),
                [new BlockOp(BlockOpKind.Update, BlockId: Guid.NewGuid().ToString(), TempId: null, Type: null, After: null, Content: Content(), BaseRevision: null)]
            )
        );

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName.EndsWith(nameof(BlockOp.BaseRevision)));
    }

    [Fact]
    public void Update_WithBaseRevisionBelowOne_Fails()
    {
        var result = _validator.Validate(
            new ApplyBlockOpsCommand(
                Guid.NewGuid(),
                [new BlockOp(BlockOpKind.Update, BlockId: Guid.NewGuid().ToString(), TempId: null, Type: null, After: null, Content: Content(), BaseRevision: 0)]
            )
        );

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(BlockOpKind.Update)]
    [InlineData(BlockOpKind.Delete)]
    [InlineData(BlockOpKind.Move)]
    public void NonInsert_WithoutBlockId_Fails(BlockOpKind kind)
    {
        var result = _validator.Validate(
            new ApplyBlockOpsCommand(
                Guid.NewGuid(),
                [new BlockOp(kind, BlockId: null, TempId: null, Type: null, After: null, Content: null, BaseRevision: kind == BlockOpKind.Update ? 1 : null)]
            )
        );

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName.EndsWith(nameof(BlockOp.BlockId)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Insert_WithoutType_Fails(string? type)
    {
        var result = _validator.Validate(
            new ApplyBlockOpsCommand(
                Guid.NewGuid(),
                [new BlockOp(BlockOpKind.Insert, BlockId: null, TempId: null, Type: type, After: null, Content: Content(), BaseRevision: null)]
            )
        );

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName.EndsWith(nameof(BlockOp.Type)));
    }

    [Fact]
    public void Empty_Batch_Fails()
    {
        var result = _validator.Validate(new ApplyBlockOpsCommand(Guid.NewGuid(), []));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Batch_BeyondTheCap_Fails()
    {
        var ops = Enumerable.Range(0, ApplyBlockOpsCommandValidator.MaxOpsPerBatch + 1).Select(_ => Insert()).ToList();

        var result = _validator.Validate(new ApplyBlockOpsCommand(Guid.NewGuid(), ops));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "Ops.Count");
    }

    [Fact]
    public void Batch_AtTheCap_Passes()
    {
        var ops = Enumerable.Range(0, ApplyBlockOpsCommandValidator.MaxOpsPerBatch).Select(_ => Insert()).ToList();

        var result = _validator.Validate(new ApplyBlockOpsCommand(Guid.NewGuid(), ops));

        Assert.True(result.IsValid);
    }

    private static BlockOp Insert(string? tempId = null)
        => new(BlockOpKind.Insert, BlockId: null, TempId: tempId, Type: BlockTypes.Paragraph, After: null, Content: Content(), BaseRevision: null);

    private static JsonElement Content() => JsonSerializer.Deserialize<JsonElement>("""{"spans":[]}""");
}
