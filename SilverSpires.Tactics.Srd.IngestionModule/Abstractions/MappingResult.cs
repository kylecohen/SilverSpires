namespace SilverSpires.Tactics.Srd.Ingestion.Abstractions;

public sealed class MappingResult<T>
{
    public bool IsSuccess { get; set; }
    public T? Entity { get; set; }
    public List<string> Warnings { get; } = new();
    public List<string> Errors { get; } = new();
}
