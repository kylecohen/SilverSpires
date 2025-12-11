using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using SilverSpires.Tactics.Srd.Characters;
using SilverSpires.Tactics.Srd.Items;
using SilverSpires.Tactics.Srd.Monsters;
using SilverSpires.Tactics.Srd.Rules;
using SilverSpires.Tactics.Srd.Spells;

namespace SilverSpires.Tactics.Srd.Data
{
    public sealed class JsonSrdCatalog : ISrdCatalog
    {
        private readonly JsonSerializerOptions _options;

        public IReadOnlyList<SrdClass> Classes { get; }
        public IReadOnlyList<SrdRace> Races { get; }
        public IReadOnlyList<SrdBackground> Backgrounds { get; }
        public IReadOnlyList<SrdFeat> Feats { get; }
        public IReadOnlyList<SrdSkill> Skills { get; }
        public IReadOnlyList<SrdLanguage> Languages { get; }
        public IReadOnlyList<SrdSpell> Spells { get; }
        public IReadOnlyList<SrdMonster> Monsters { get; }
        public IReadOnlyList<SrdMagicItem> MagicItems { get; }
        public IReadOnlyList<SrdEquipment> Equipment { get; }
        public IReadOnlyList<SrdWeapon> Weapons { get; }
        public IReadOnlyList<SrdArmor> Armor { get; }
        public IReadOnlyList<GameEffect> Effects { get; }

        public JsonSrdCatalog(string jsonDirectory)
        {
            if (!Directory.Exists(jsonDirectory))
                throw new DirectoryNotFoundException(jsonDirectory);

            _options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            Classes = LoadList<SrdClass>(jsonDirectory, "classes.json");
            Races = LoadList<SrdRace>(jsonDirectory, "races.json");
            Backgrounds = LoadList<SrdBackground>(jsonDirectory, "backgrounds.json");
            Feats = LoadList<SrdFeat>(jsonDirectory, "feats.json");
            Skills = LoadList<SrdSkill>(jsonDirectory, "skills.json");
            Languages = LoadList<SrdLanguage>(jsonDirectory, "languages.json");
            Spells = LoadList<SrdSpell>(jsonDirectory, "spells.json");
            Monsters = LoadList<SrdMonster>(jsonDirectory, "monsters.json");
            MagicItems = LoadList<SrdMagicItem>(jsonDirectory, "magicitems.json");
            Equipment = LoadList<SrdEquipment>(jsonDirectory, "equipment.json");
            Weapons = LoadList<SrdWeapon>(jsonDirectory, "weapons.json");
            Armor = LoadList<SrdArmor>(jsonDirectory, "armor.json");
            Effects = LoadList<GameEffect>(jsonDirectory, "effects.json");
        }

        private IReadOnlyList<T> LoadList<T>(string dir, string fileName)
        {
            var path = Path.Combine(dir, fileName);
            if (!File.Exists(path)) return Array.Empty<T>();

            var json = File.ReadAllText(path);
            var list = JsonSerializer.Deserialize<List<T>>(json, _options);
            return list ?? new List<T>();
        }

        public SrdMonster? GetMonsterById(string id)
            => Monsters.FirstOrDefault(m => string.Equals(m.Id, id, StringComparison.OrdinalIgnoreCase));

        public SrdWeapon? GetWeaponById(string id)
            => Weapons.FirstOrDefault(w => string.Equals(w.Id, id, StringComparison.OrdinalIgnoreCase));

        public SrdArmor? GetArmorById(string id)
            => Armor.FirstOrDefault(a => string.Equals(a.Id, id, StringComparison.OrdinalIgnoreCase));
    }
}
