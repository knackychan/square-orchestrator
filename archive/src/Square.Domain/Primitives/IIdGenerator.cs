namespace Square.Domain.Primitives;

public interface IIdGenerator
{
    TId New<TId>() where TId : struct, IStrongId<TId>;
}
