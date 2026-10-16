using FluentValidation;

namespace CodeCafe.Application.Blocks.ApplyBlockOps;

public sealed class ApplyBlockOpsCommandValidator : AbstractValidator<ApplyBlockOpsCommand>
{
    // One batch stays small enough for a single transaction and a single response.
    public const int MaxOpsPerBatch = 100;

    public ApplyBlockOpsCommandValidator()
    {
        RuleFor(command => command.Ops.Count).InclusiveBetween(1, MaxOpsPerBatch);
        RuleForEach(command => command.Ops).ChildRules(op =>
        {
            op.RuleFor(blockOp => blockOp.Type).NotEmpty().When(blockOp => blockOp.Kind == BlockOpKind.Insert);
            op.RuleFor(blockOp => blockOp.BlockId)
                .NotEmpty()
                .When(blockOp => blockOp.Kind is BlockOpKind.Update or BlockOpKind.Delete or BlockOpKind.Move);
            // Updates are optimistic-locked; a missing BaseVersion must fail validation rather
            // than silently degrade to last-write-wins. Versions start at 1.
            op.RuleFor(blockOp => blockOp.BaseVersion)
                .NotNull()
                .GreaterThanOrEqualTo(1)
                .When(blockOp => blockOp.Kind == BlockOpKind.Update);
        });
    }
}
