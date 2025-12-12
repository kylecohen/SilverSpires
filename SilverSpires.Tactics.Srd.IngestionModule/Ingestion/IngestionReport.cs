namespace SilverSpires.Tactics.Srd.Ingestion.Ingestion;

public sealed class IngestionReport
{
    public int Read { get; set; }
    public int Upserted { get; set; }
    public int Skipped { get; set; }

    public List<string> Warnings { get; } = new();
    public List<string> Errors { get; } = new();

    public override string ToString()
        => $"Read={Read}, Upserted={Upserted}, Skipped={Skipped}, Warnings={Warnings.Count}, Errors={Errors.Count}";
}
