namespace SilverSpires.Tactics.Srd.Ingestion.Abstractions;

public sealed class MappingResult<T>
{
    public T? Entity { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    public bool IsSuccess => Entity is not null && Errors.Count == 0;

    public static MappingResult<T> Success(T entity, params string[] warnings)
        => new() { Entity = entity, Warnings = warnings };

    public static MappingResult<T> Failure(params string[] errors)
        => new() { Entity = default, Errors = errors };
}
