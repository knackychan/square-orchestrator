namespace Square.TerminalProof.Native;

public readonly record struct TerminalSize
{
    public TerminalSize(int columns, int rows)
    {
        if (columns is < 1 or > short.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(columns), columns, "Terminal columns must fit a positive Win32 COORD value.");
        }

        if (rows is < 1 or > short.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(rows), rows, "Terminal rows must fit a positive Win32 COORD value.");
        }

        Columns = columns;
        Rows = rows;
    }

    public int Columns { get; }

    public int Rows { get; }

    internal NativeMethods.Coord ToNative() => new((short)Columns, (short)Rows);

    public override string ToString() => $"{Columns}x{Rows}";
}
