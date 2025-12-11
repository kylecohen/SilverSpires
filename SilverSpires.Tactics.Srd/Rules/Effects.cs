using System.Text.Json;

namespace SilverSpires.Tactics.Srd.Rules
{
    public sealed class GameEffect
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Trigger { get; set; } = string.Empty;
        public string TargetScope { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Arbitrary components; SRD effects can embed different structures here.
        public JsonElement[] Components { get; set; } = System.Array.Empty<JsonElement>();
    }
}
