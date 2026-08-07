namespace Square.Domain.Primitives;

public sealed record DomainProblem(string Code, string Message, IReadOnlyDictionary<string, string>? Details = null);
