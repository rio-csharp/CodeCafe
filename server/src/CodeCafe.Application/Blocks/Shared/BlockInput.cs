using System.Text.Json;

namespace CodeCafe.Application.Blocks.Shared;

// One block to create. Content is an untyped JsonElement because Type decides the payload shape, so
// the strict form only exists once BlockPayloads.ValidateAndNormalize has run.
//
// Deliberately no TempId: a batch inserts one flat run at a single position, so there is nothing
// inside it that a temp id could reference. Batch-relative references -- a temp id, or Parent --
// belong to BlockOp in ApplyBlockOps, which is where callers needing them should go.
public sealed record BlockInput(string Type, JsonElement Content);
