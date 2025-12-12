using System.Text.Json;
using System.Text.Json.Serialization;
using SilverSpires.Tactics.Srd.Ingestion.Storage;

namespace SilverSpires.Tactics.Srd.Ingestion.Export;

public sealed class JsonSrdExporter
{
    private readonly ISrdRepository _repo;
    private readonly JsonSerializerOptions _json;

    public JsonSrdExporter(ISrdRepository repo)
    {
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _json = new JsonSerializerOptions { WriteIndented = true };
        _json.Converters.Add(new JsonStringEnumConverter());
    }

    public async Task ExportAsync(string jsonFolderPath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(jsonFolderPath))
            throw new ArgumentNullException(nameof(jsonFolderPath));

        Directory.CreateDirectory(jsonFolderPath);

        await Write("classes.json", await _repo.GetAllClassesAsync(ct), ct);
        await Write("races.json", await _repo.GetAllRacesAsync(ct), ct);
        await Write("backgrounds.json", await _repo.GetAllBackgroundsAsync(ct), ct);
        await Write("feats.json", await _repo.GetAllFeatsAsync(ct), ct);
        await Write("skills.json", await _repo.GetAllSkillsAsync(ct), ct);
        await Write("languages.json", await _repo.GetAllLanguagesAsync(ct), ct);
        await Write("spells.json", await _repo.GetAllSpellsAsync(ct), ct);
        await Write("monsters.json", await _repo.GetAllMonstersAsync(ct), ct);
        await Write("magicitems.json", await _repo.GetAllMagicItemsAsync(ct), ct);
        await Write("equipment.json", await _repo.GetAllEquipmentAsync(ct), ct);
        await Write("weapons.json", await _repo.GetAllWeaponsAsync(ct), ct);
        await Write("armor.json", await _repo.GetAllArmorAsync(ct), ct);
        await Write("effects.json", await _repo.GetAllEffectsAsync(ct), ct);

        async Task Write<T>(string file, IReadOnlyList<T> data, CancellationToken token)
        {
            var path = Path.Combine(jsonFolderPath, file);
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(data, _json), token);
        }
    }
}
