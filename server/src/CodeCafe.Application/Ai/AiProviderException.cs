namespace CodeCafe.Application.Ai;

/// <summary>
/// A call to the AI provider failed in a way the use case can act on. Thrown by the Infrastructure
/// adapters so handlers never have to name the provider SDK's exception types: those handlers used to
/// catch System.ClientModel.ClientResultException directly, which put an OpenAI implementation detail
/// in the application layer and meant swapping providers would have required editing use cases.
/// </summary>
public sealed class AiProviderException(
    AiFailureKind kind,
    string message,
    Exception? innerException = null
) : Exception(message, innerException)
{
    /// <summary>
    /// <see cref="AiFailureKind.Upstream"/> when the provider errored or was unreachable,
    /// <see cref="AiFailureKind.Timeout"/> when it did not answer in time, and
    /// <see cref="AiFailureKind.Unprocessable"/> when it answered with something unusable.
    /// </summary>
    public AiFailureKind Kind { get; } = kind;
}
