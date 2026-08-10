namespace Square.Domain.Primitives;

/// <summary>A stable, type-safe identifier whose textual form is version-independent.</summary>
public interface IStrongId<TSelf>
    where TSelf : struct, IStrongId<TSelf>
{
    static abstract string Prefix { get; }
    string Value { get; }
    static abstract bool TryParse(string? text, out TSelf value);
    static abstract TSelf Parse(string text);
    static abstract TSelf FromCanonical(string canonicalValue);
}
