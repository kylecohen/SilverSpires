namespace SilverSpires.Tactics.Srd.Ingestion.Abstractions;

public enum SrdSourceKind
{
    FileJson,
    HttpJson
}

public enum SrdEntityType
{
    Class,
    Race,
    Background,
    Feat,
    Skill,
    Language,
    Spell,
    Monster,
    MagicItem,
    Equipment,
    Weapon,
    Armor,
    Effect
}
