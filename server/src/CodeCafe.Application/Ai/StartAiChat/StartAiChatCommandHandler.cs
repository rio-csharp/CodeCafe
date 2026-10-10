using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Notebooks.GetNotebookTree;
using CodeCafe.Application.Notebooks.Shared;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Revisions;
using MediatR;
using Microsoft.Extensions.AI;

namespace CodeCafe.Application.Ai.StartAiChat;

public sealed class StartAiChatCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IPasswordHasher passwordHasher,
    ISender sender,
    IAiChatClientFactory chatClientFactory,
    IChangeSourceAccessor changeSource,
    AiOptions options
) : IStreamCommandHandler<StartAiChatCommand, AiChatEvent>
{
    public async IAsyncEnumerable<AiChatEvent> Handle(
        StartAiChatCommand request,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        // Pre-stream failures surface as an error event: once SSE headers are sent, no status
        // code can be changed, so the stream itself is the error channel.
        if (!options.Enabled)
        {
            yield return ErrorEvent(AiErrors.Disabled);
            yield break;
        }

        if (string.IsNullOrWhiteSpace(options.Model) || string.IsNullOrWhiteSpace(options.ApiKey))
        {
            yield return ErrorEvent(AiErrors.NotConfigured);
            yield break;
        }

        var userId = currentUserAccessor.User?.Id;
        var notebook = userId is null
            ? null
            : await notebooks.FindByIdOrSlugAsync(request.NotebookIdOrSlug, cancellationToken);
        if (notebook is null)
        {
            yield return ErrorEvent(NotebookErrors.NotFound);
            yield break;
        }

        // Same existence-hiding rule as every other read endpoint.
        if (NotebookAccess.CheckRead(notebook, userId, accessCode: null, passwordHasher) is { } accessError)
        {
            yield return ErrorEvent(accessError);
            yield break;
        }

        if (request.Messages.Sum(message => message.Content.Length) > options.MaxHistoryChars)
        {
            yield return ErrorEvent(AiErrors.HistoryTooLong);
            yield break;
        }

        // The assistant acts as the user; every edit it makes is attributed to Ai.
        changeSource.Source = RevisionSource.Ai;

        var outline = await LoadOutline(request, cancellationToken);
        var chatOptions = new ChatOptions
        {
            Tools = [.. AssistantTools.Create(sender, request.NotebookIdOrSlug, options)],
            MaxOutputTokens = options.MaxOutputTokens,
        };

        var messages = BuildMessages(notebook, outline, request);
        var client = chatClientFactory
            .Create()
            .AsBuilder()
            .UseFunctionInvocation(configure: functions => functions.MaximumIterationsPerRequest = options.MaxToolIterations)
            .Build();

        await foreach (var aiEvent in StreamChat(client, messages, chatOptions, cancellationToken))
        {
            yield return aiEvent;
        }
    }

    private async IAsyncEnumerable<AiChatEvent> StreamChat(
        IChatClient client,
        List<ChatMessage> messages,
        ChatOptions chatOptions,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        IAsyncEnumerable<ChatResponseUpdate>? updates = null;
        try
        {
            updates = client.GetStreamingResponseAsync(messages, chatOptions, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // Fall through to the guarded return below.
        }

        if (updates is null)
        {
            yield return ErrorEvent(AiErrors.ProviderFailed);
            yield break;
        }

        // Iteration, not acquisition, is where provider failures actually surface.
        await using var enumerator = updates.GetAsyncEnumerator(cancellationToken);
        while (true)
        {
            ChatResponseUpdate? update = null;
            var failed = false;
            try
            {
                if (await enumerator.MoveNextAsync())
                {
                    update = enumerator.Current;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                failed = true;
            }

            if (failed)
            {
                yield return ErrorEvent(AiErrors.ProviderFailed);
                yield break;
            }

            if (update is null)
            {
                break;
            }

            foreach (var content in update.Contents)
            {
                switch (content)
                {
                    case TextContent text:
                        yield return new AiChatTextEvent(text.Text);
                        break;
                    case FunctionCallContent call:
                        yield return new AiChatToolCallEvent(
                            call.Name,
                            call.CallId,
                            JsonSerializer.SerializeToElement(call.Arguments)
                        );
                        break;
                    case FunctionResultContent result:
                        yield return new AiChatToolResultEvent(
                            result.CallId,
                            JsonSerializer.SerializeToElement(result.Result)
                        );
                        break;
                    // A provider-side failure (an overloaded upstream, a rate limit) arrives as an
                    // ErrorContent update and the SDK then ends the stream NORMALLY. Without this
                    // the turn would look like a successful, empty reply.
                    case ErrorContent error:
                        yield return new AiChatErrorEvent(
                            string.IsNullOrWhiteSpace(error.ErrorCode)
                                ? AiErrors.ProviderFailed.Code
                                : error.ErrorCode,
                            string.IsNullOrWhiteSpace(error.Message)
                                ? AiErrors.ProviderFailed.Message
                                : error.Message
                        );
                        yield break;
                }
            }
        }

        yield return new AiChatDoneEvent();
    }

    private static List<ChatMessage> BuildMessages(Notebook notebook, string outline, StartAiChatCommand request)
    {
        // The system prompt is a canonical system message; relays that cannot accept one are
        // handled at the transport boundary (see AiOptions.Relay and the compat adapters).
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, string.Format(AssistantSystemPrompt.Template, notebook.Title, outline)),
        };
        messages.AddRange(
            request.Messages.Select(message => new ChatMessage(
                message.Role == AiChatRole.User ? ChatRole.User : ChatRole.Assistant,
                message.Content
            ))
        );
        return messages;
    }

    // Fresh on every turn: the tree may have changed since the last one. The deterministic
    // rendering keeps it byte-identical when it has not, which is what prefix caching needs.
    private async Task<string> LoadOutline(StartAiChatCommand request, CancellationToken cancellationToken)
    {
        var tree = await sender.Send(
            new GetNotebookTreeQuery(request.NotebookIdOrSlug),
            cancellationToken
        );
        if (!tree.IsSuccess)
        {
            return "(page tree unavailable; use search_pages to find pages)";
        }

        var builder = new StringBuilder();
        var truncated = false;
        AppendLevel(tree.Value!.Roots, depth: 0, builder, options.MaxOutlineChars, ref truncated);
        if (truncated)
        {
            builder.AppendLine("- …(outline truncated; explore with get_notebook_tree or get_page_by_path)");
        }

        return builder.ToString();
    }

    private static void AppendLevel(
        IReadOnlyList<PageTreeNodeDto> nodes,
        int depth,
        StringBuilder builder,
        int maxChars,
        ref bool truncated
    )
    {
        foreach (var node in nodes)
        {
            if (builder.Length >= maxChars)
            {
                truncated = true;
                return;
            }

            builder.Append(new string(' ', depth * 2)).Append("- ").Append(node.Title).Append("  (").Append(node.Path).AppendLine(")");
            AppendLevel(node.Children, depth + 1, builder, maxChars, ref truncated);
            if (truncated)
            {
                return;
            }
        }
    }

    private static AiChatErrorEvent ErrorEvent(Error error) => new(error.Code, error.Message);
}
