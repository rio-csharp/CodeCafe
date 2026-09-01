using System.Text;
using CodeCafe.Domain.Uploads;

namespace CodeCafe.Domain.Tests;

public sealed class UploadSessionTests
{
    [Fact]
    public void AppendChunk_Assigns_Increasing_Sequence_Numbers()
    {
        var session = CreateSession();

        var first = session.AppendChunk("# Title\n", DateTimeOffset.UtcNow);
        var second = session.AppendChunk("body", DateTimeOffset.UtcNow);

        Assert.Equal(1, first.SequenceNumber);
        Assert.Equal(2, second.SequenceNumber);
        Assert.Equal(2, session.ChunkCount);
        Assert.Equal(2, session.Chunks.Count);
    }

    [Fact]
    public void AppendChunk_Accumulates_Utf8_Byte_Count()
    {
        var session = CreateSession();

        session.AppendChunk("ab", DateTimeOffset.UtcNow);
        session.AppendChunk("中", DateTimeOffset.UtcNow);

        var expected = Encoding.UTF8.GetByteCount("ab") + Encoding.UTF8.GetByteCount("中");
        Assert.Equal(expected, session.BytesReceived);
    }

    [Fact]
    public void Chunks_Is_ReadOnly_From_Outside()
    {
        var session = CreateSession();
        session.AppendChunk("x", DateTimeOffset.UtcNow);

        Assert.IsAssignableFrom<IReadOnlyCollection<UploadChunk>>(session.Chunks);
    }

    private static UploadSession CreateSession()
    {
        return UploadSession.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "notes.md",
            "text/markdown",
            DateTimeOffset.UtcNow
        );
    }
}
