namespace Square.Domain.Primitives;

public sealed class Result<T>
{
    private readonly T? value;
    private Result(bool isSuccess, T? value, DomainProblem? problem)
    {
        IsSuccess = isSuccess;
        this.value = value;
        Problem = problem;
    }
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T Value => IsSuccess ? value! : throw new InvalidOperationException("A failed result has no value.");
    public DomainProblem? Problem { get; }
    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(DomainProblem problem)
    {
        ArgumentNullException.ThrowIfNull(problem);
        return new Result<T>(false, default, problem);
    }
}
