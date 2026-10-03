using CodeCafe.Application.Common;

namespace CodeCafe.Application.Ai;

public static class AiErrors
{
    public static readonly Error Disabled = new(
        "ai_disabled",
        "The AI assistant is not enabled on this server.",
        ErrorKind.Validation
    );

    public static readonly Error NotConfigured = new(
        "ai_not_configured",
        "The AI assistant is enabled but Model or ApiKey is missing from the Ai configuration section.",
        ErrorKind.Unexpected
    );

    public static readonly Error HistoryTooLong = new(
        "history_too_long",
        "The chat history exceeds the server's size limit. Shorten the conversation and try again.",
        ErrorKind.Validation
    );

    public static readonly Error ProviderFailed = new(
        "ai_provider_failed",
        "The AI provider request failed. Check the server logs and the Ai configuration.",
        ErrorKind.Unexpected
    );
}
