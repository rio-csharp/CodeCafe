using CodeCafe.Application.Common;

namespace CodeCafe.Application.Blocks.Shared;

public static class BlockErrors
{
    // Also used for blocks the caller may not see, so existence is not leaked.
    public static readonly Error NotFound = new(
        "block_not_found",
        "The block was not found.",
        ErrorKind.NotFound
    );

    public static readonly Error InvalidBlockPayload = new(
        "invalid_block_payload",
        "The block payload does not match its type's contract.",
        ErrorKind.Validation
    );

    // Deserialization and domain-invariant failures carry the underlying reason so clients can
    // show users exactly what was rejected, e.g. a non-absolute link href.
    public static Error InvalidBlockPayloadReason(string reason) => new(
        "invalid_block_payload",
        $"The block payload does not match its type's contract: {reason}",
        ErrorKind.Validation
    );

    // Writes are strict: unknown types are rejected instead of being stored opaquely.
    public static readonly Error UnsupportedBlockType = new(
        "unsupported_block_type",
        "The block type is not supported.",
        ErrorKind.Validation
    );

    public static readonly Error VersionConflict = new(
        "block_version_conflict",
        "The block was modified since the base version; reload and retry.",
        ErrorKind.Conflict
    );

    public static readonly Error InvalidBlockPosition = new(
        "invalid_block_position",
        "The requested position does not identify a sibling in the target chain.",
        ErrorKind.Validation
    );

    // TempIds are batch-scoped, so a collision means the batch itself is malformed.
    public static readonly Error DuplicateTempId = new(
        "duplicate_temp_id",
        "The temp id was already used by an earlier op in this batch.",
        ErrorKind.Validation
    );
}
