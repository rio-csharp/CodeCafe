namespace CodeCafe.Application.Ai;

/// <summary>
/// Why an AI use case failed, independent of any transport. Each transport owns exactly one mapping
/// from this to its own error representation, so the same failure cannot surface as one status code on
/// the REST endpoints and a different one elsewhere.
/// </summary>
public enum AiFailureKind
{
    /// <summary>Caller sent something invalid. Maps to 400.</summary>
    Validation,

    /// <summary>Caller may not perform this operation on this notebook. Maps to 403.</summary>
    Forbidden,

    /// <summary>Notebook, page or proposal does not exist. Maps to 404.</summary>
    NotFound,

    /// <summary>Concurrent change lost the race. Maps to 409.</summary>
    Conflict,

    /// <summary>
    /// The request was well-formed but the model's output could not be used — unparseable JSON, an
    /// empty draft, an edit referring to a block that is not there. Maps to 422.
    /// </summary>
    Unprocessable,

    /// <summary>Caller exceeded a rate limit. Maps to 429.</summary>
    RateLimited,

    /// <summary>The AI provider failed or was unreachable. Maps to 502.</summary>
    Upstream,

    /// <summary>The AI provider did not answer in time. Maps to 504.</summary>
    Timeout
}
