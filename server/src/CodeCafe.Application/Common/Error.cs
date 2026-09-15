
namespace CodeCafe.Application.Common;

public sealed record Error(string Code, string Message, ErrorKind Kind);
