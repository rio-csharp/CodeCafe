using CodeCafe.Application.Common.Abstractions;

namespace CodeCafe.Application.Common;

// Wraps the save that can lose a slug race, so every handler does not repeat the same loop.
// The boundary between the two policies is the caller: a slug the caller picked is theirs to fix,
// while a slug generated for them is worth retrying with a fresh candidate.
internal static class SlugConflict
{
    // Bounded retries: each attempt draws a fresh random suffix, so a couple of tries is plenty.
    public const int MaxAttempts = 3;

    // reKeyAsync is null when the caller picked the slug: the conflict is then theirs to fix and
    // surfaces on the first save. Otherwise it moves the aggregate to a fresh slug, and false
    // means no candidate is left.
    public static async Task<Result> SaveAsync(
        IUnitOfWork unitOfWork,
        Func<CancellationToken, Task<bool>>? reKeyAsync,
        Error conflict,
        CancellationToken cancellationToken
    )
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Success();
            }
            catch (UniqueConstraintViolationException)
            {
                if (reKeyAsync is null || attempt == MaxAttempts || !await reKeyAsync(cancellationToken))
                {
                    return Result.Failure(conflict);
                }
            }
        }
    }
}
