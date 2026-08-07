using System.Text;

namespace Square.TerminalProof.Native;

public sealed record TerminalOutputSnapshot(byte[] Bytes, TimeSpan? FirstByteLatency)
{
    public long Length => Bytes.LongLength;

    public string Utf8Text => Encoding.UTF8.GetString(Bytes);
}
