using SilverSpires.Tactics.Srd.Rules;

namespace SilverSpires.Tactics.Srd.Characters
{
    public sealed class SrdSkill
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public AbilityScoreType Ability { get; set; } = AbilityScoreType.Wisdom;
        public string Description { get; set; } = string.Empty;
    }

    public sealed class SrdLanguage
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsStandard { get; set; }
        public string Script { get; set; } = string.Empty;
        public string TypicalSpeakers { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
