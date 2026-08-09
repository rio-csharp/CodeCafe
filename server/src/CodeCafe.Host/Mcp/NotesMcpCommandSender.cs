using CodeCafe.Application.Common.Uploads;
using CodeCafe.Application.Notes;
using CodeCafe.Application.Common.Messaging;
using FluentValidation;
using MediatR;

namespace CodeCafe.Host.Mcp;

internal static class NotesMcpCommandSender
{
    public static async Task<NotesResult<T>> SendAsync<T>(
        ISender sender,
        ICommand<NotesResult<T>> command,
        CancellationToken cancellationToken)
    {
        try
        {
            return await sender.Send(command, cancellationToken);
        }
        catch (ValidationException exception)
        {
            return NotesResult<T>.Failure(
                NotesFailureKind.Validation,
                "validation_error",
                BuildMessage(exception));
        }
    }

    public static async Task<NotesResult> SendAsync(
        ISender sender,
        ICommand<NotesResult> command,
        CancellationToken cancellationToken)
    {
        try
        {
            return await sender.Send(command, cancellationToken);
        }
        catch (ValidationException exception)
        {
            return NotesResult.Failure(
                NotesFailureKind.Validation,
                "validation_error",
                BuildMessage(exception));
        }
    }

    private static string BuildMessage(ValidationException exception)
        => string.Join(" ", exception.Errors.Select(error => error.ErrorMessage).Distinct());
}
