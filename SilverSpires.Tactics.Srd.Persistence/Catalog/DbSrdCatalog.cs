using SilverSpires.Tactics.Srd.Data;
using SilverSpires.Tactics.Srd.Characters;
using SilverSpires.Tactics.Srd.Items;
using SilverSpires.Tactics.Srd.Monsters;
using SilverSpires.Tactics.Srd.Rules;
using SilverSpires.Tactics.Srd.Spells;
using SilverSpires.Tactics.Srd.Persistence.Storage;

namespace SilverSpires.Tactics.Srd.Persistence.Catalog;

public sealed class DbSrdCatalog : ISrdCatalog
{
    private readonly ISrdRepository _repo;

    private readonly Dictionary<string, SrdMonster> _monstersById = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SrdWeapon> _weaponsById = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SrdArmor> _armorById = new(StringComparer.OrdinalIgnoreCase);

    public DbSrdCatalog(ISrdRepository repo)
    {
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
    }

    public IReadOnlyList<SrdClass> Classes { get; private set; } = Array.Empty<SrdClass>();
    public IReadOnlyList<SrdRace> Races { get; private set; } = Array.Empty<SrdRace>();
    public IReadOnlyList<SrdBackground> Backgrounds { get; private set; } = Array.Empty<SrdBackground>();
    public IReadOnlyList<SrdFeat> Feats { get; private set; } = Array.Empty<SrdFeat>();
    public IReadOnlyList<SrdSkill> Skills { get; private set; } = Array.Empty<SrdSkill>();
    public IReadOnlyList<SrdLanguage> Languages { get; private set; } = Array.Empty<SrdLanguage>();
    public IReadOnlyList<SrdSpell> Spells { get; private set; } = Array.Empty<SrdSpell>();
    public IReadOnlyList<SrdMonster> Monsters { get; private set; } = Array.Empty<SrdMonster>();
    public IReadOnlyList<SrdMagicItem> MagicItems { get; private set; } = Array.Empty<SrdMagicItem>();
    public IReadOnlyList<SrdEquipment> Equipment { get; private set; } = Array.Empty<SrdEquipment>();
    public IReadOnlyList<SrdWeapon> Weapons { get; private set; } = Array.Empty<SrdWeapon>();
    public IReadOnlyList<SrdArmor> Armor { get; private set; } = Array.Empty<SrdArmor>();
    public IReadOnlyList<GameEffect> Effects { get; private set; } = Array.Empty<GameEffect>();

    public async Task LoadAsync(CancellationToken ct = default)
    {
        Classes = await _repo.GetAllClassesAsync(ct);
        Races = await _repo.GetAllRacesAsync(ct);
        Backgrounds = await _repo.GetAllBackgroundsAsync(ct);
        Feats = await _repo.GetAllFeatsAsync(ct);
        Skills = await _repo.GetAllSkillsAsync(ct);
        Languages = await _repo.GetAllLanguagesAsync(ct);
        Spells = await _repo.GetAllSpellsAsync(ct);
        Monsters = await _repo.GetAllMonstersAsync(ct);
        MagicItems = await _repo.GetAllMagicItemsAsync(ct);
        Equipment = await _repo.GetAllEquipmentAsync(ct);
        Weapons = await _repo.GetAllWeaponsAsync(ct);
        Armor = await _repo.GetAllArmorAsync(ct);
        Effects = await _repo.GetAllEffectsAsync(ct);

        _monstersById.Clear();
        foreach (var m in Monsters)
            if (!string.IsNullOrWhiteSpace(m.Id))
                _monstersById[m.Id] = m;

        _weaponsById.Clear();
        foreach (var w in Weapons)
            if (!string.IsNullOrWhiteSpace(w.Id))
                _weaponsById[w.Id] = w;

        _armorById.Clear();
        foreach (var a in Armor)
            if (!string.IsNullOrWhiteSpace(a.Id))
                _armorById[a.Id] = a;
    }

    public SrdMonster? GetMonsterById(string id)
        => _monstersById.TryGetValue(id, out var m) ? m : null;

    public SrdWeapon? GetWeaponById(string id)
        => _weaponsById.TryGetValue(id, out var w) ? w : null;

    public SrdArmor? GetArmorById(string id)
        => _armorById.TryGetValue(id, out var a) ? a : null;
}
