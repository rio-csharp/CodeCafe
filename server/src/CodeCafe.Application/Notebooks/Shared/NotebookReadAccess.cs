using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Common;
using CodeCafe.Domain.Notebooks;

namespace CodeCafe.Application.Notebooks.Shared;

// The single gate every notebook read path must pass: details today, tree/export/search once
// they leave their skeletons. Returns null when the read is allowed, else the error to return.
public static class NotebookReadAccess
{
    public static Error? Check(Notebook notebook, Guid? userId, string? accessCode, IPasswordHasher passwordHasher)
    {
        var isPrivilegedReader =
            userId is not null && (notebook.OwnerId == userId || notebook.IsSharedWith(userId.Value));

        return notebook.Visibility switch
        {
            // Strangers get NotFound so existence is not leaked.
            NotebookVisibility.Private => isPrivilegedReader ? null : NotebookErrors.NotFound,

            // The access code only gates Unlisted notebooks; owners and collaborators bypass it.
            NotebookVisibility.Unlisted when notebook.AccessCodeHash is not null && !isPrivilegedReader =>
                accessCode is not null && passwordHasher.Verify(accessCode, notebook.AccessCodeHash)
                    ? null
                    : NotebookErrors.AccessCodeRequired,

            _ => null,
        };
    }
}
