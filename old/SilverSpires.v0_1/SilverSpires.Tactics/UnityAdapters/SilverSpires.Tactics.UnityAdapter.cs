// SilverSpires Tactics – Unity Adapter Layer (v0.1)
// This file shows how to bridge the core rules library into a Unity project.
// - ScriptableObjects for data (races, classes, spells, encounters)
// - MonoBehaviours to adapt Unity scene objects to ICombatant and Encounter

using System.Collections.Generic;
using UnityEngine;
using SilverSpires.Tactics.Core;
using SilverSpires.Tactics.Rules;
using SilverSpires.Tactics.World;
using SilverSpires.Tactics.Characters;

namespace SilverSpires.Tactics.UnityAdapters
{
    #region ScriptableObject Data Definitions

    [CreateAssetMenu(menuName = "SilverSpires/Data/Race")]
    public class RaceData : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        [TextArea] public string Description;

        public int StrengthBonus;
        public int DexterityBonus;
        public int ConstitutionBonus;
        public int IntelligenceBonus;
        public int WisdomBonus;
        public int CharismaBonus;

        public CharacterRace ToModel()
        {
            var race = new CharacterRace
            {
                Id = string.IsNullOrWhiteSpace(Id) ? System.Guid.NewGuid().ToString() : Id,
                Name = DisplayName,
                Description = Description
            };

            if (StrengthBonus != 0) race.AbilityScoreBonuses[AbilityScoreType.Strength] = StrengthBonus;
            if (DexterityBonus != 0) race.AbilityScoreBonuses[AbilityScoreType.Dexterity] = DexterityBonus;
            if (ConstitutionBonus != 0) race.AbilityScoreBonuses[AbilityScoreType.Constitution] = ConstitutionBonus;
            if (IntelligenceBonus != 0) race.AbilityScoreBonuses[AbilityScoreType.Intelligence] = IntelligenceBonus;
            if (WisdomBonus != 0) race.AbilityScoreBonuses[AbilityScoreType.Wisdom] = WisdomBonus;
            if (CharismaBonus != 0) race.AbilityScoreBonuses[AbilityScoreType.Charisma] = CharismaBonus;

            return race;
        }
    }

    [CreateAssetMenu(menuName = "SilverSpires/Data/Class")]
    public class ClassData : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        [TextArea] public string Description;
        public int HitDie = 8;

        public CharacterClass ToModel()
        {
            return new CharacterClass
            {
                Id = string.IsNullOrWhiteSpace(Id) ? System.Guid.NewGuid().ToString() : Id,
                Name = DisplayName,
                Description = Description,
                HitDie = HitDie
            };
        }
    }

    [CreateAssetMenu(menuName = "SilverSpires/Data/Spell")]
    public class SpellData : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        [TextArea] public string Description;
        [Range(0, 9)] public int Level;
        public string School;
        public string CastingTime = "1 Action";
        public string Range = "Self";
        public string Components = "V,S";
        public string Duration = "Instantaneous";
        public bool RequiresConcentration;

        public Spell ToModel()
        {
            return new Spell
            {
                Id = string.IsNullOrWhiteSpace(Id) ? System.Guid.NewGuid().ToString() : Id,
                Name = DisplayName,
                Description = Description,
                Level = Level,
                School = School,
                CastingTime = CastingTime,
                Range = Range,
                Components = Components,
                Duration = Duration,
                RequiresConcentration = RequiresConcentration
            };
        }
    }

    [CreateAssetMenu(menuName = "SilverSpires/Data/Encounter")]
    public class EncounterData : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        [TextArea] public string Description;

        public List<CombatantSpawn> Combatants = new();

        [System.Serializable]
        public class CombatantSpawn
        {
            public string Name;
            public bool IsPlayerControlled;
            public Vector3 Position;
            public RaceData Race;
            public ClassData Class;
            public int Level = 1;
        }
    }


    [CreateAssetMenu(menuName = "SilverSpires/Data/Shatter")]
    public class ShatterData : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        [TextArea] public string Description;
        public List<string> Tags = new();
        public Shatter ToModel()
        {
            var model = new Shatter
            {
                Id = string.IsNullOrWhiteSpace(Id) ? System.Guid.NewGuid().ToString() : Id,
                Name = DisplayName,
                Description = Description
            };
            foreach (var t in Tags)
            {
                if (!string.IsNullOrWhiteSpace(t))
                    model.Tags.Add(t);
            }
            return model;
        }
    }

    #endregion

    #region MonoBehaviour Adapters

    /// <summary>
    /// Attach this to a Unity GameObject to bind it to an ICombatant instance.
    /// The core simulation uses the model; Unity drives visuals based on it.
    /// </summary>
    public class CombatantView : MonoBehaviour
    {
        public bool IsPlayerControlled;
        public RaceData RaceData;
        public ClassData ClassData;
        public int Level = 1;
        public string DisplayName;

        public PlayerCharacter? PlayerModel { get; private set; }
        public NonPlayerCharacter? NpcModel { get; private set; }

        public ICombatant Model => (ICombatant)(PlayerModel ?? (ICombatant)NpcModel!);

        public void InitializeAsPlayer()
        {
            var race = RaceData ? RaceData.ToModel() : new CharacterRace { Name = "Unknown Race" };
            var cls = ClassData ? ClassData.ToModel() : new CharacterClass { Name = "Unknown Class" };

            var pc = new PlayerCharacter
            {
                Name = string.IsNullOrWhiteSpace(DisplayName) ? gameObject.name : DisplayName,
                Level = Level,
                Race = race,
                Class = cls
            };

            // Example: default array of ability scores for testing
            pc.AbilityScores.SetScore(AbilityScoreType.Strength, 15);
            pc.AbilityScores.SetScore(AbilityScoreType.Dexterity, 14);
            pc.AbilityScores.SetScore(AbilityScoreType.Constitution, 14);
            pc.AbilityScores.SetScore(AbilityScoreType.Intelligence, 10);
            pc.AbilityScores.SetScore(AbilityScoreType.Wisdom, 10);
            pc.AbilityScores.SetScore(AbilityScoreType.Charisma, 10);

            pc.Position = new WorldPosition(transform.position.x, transform.position.y, transform.position.z);

            PlayerModel = pc;
            NpcModel = null;
        }

        public void InitializeAsNpc()
        {
            var npc = new NonPlayerCharacter
            {
                Name = string.IsNullOrWhiteSpace(DisplayName) ? gameObject.name : DisplayName,
                Level = Level
            };

            npc.AbilityScores.SetScore(AbilityScoreType.Strength, 13);
            npc.AbilityScores.SetScore(AbilityScoreType.Dexterity, 12);
            npc.AbilityScores.SetScore(AbilityScoreType.Constitution, 12);
            npc.AbilityScores.SetScore(AbilityScoreType.Intelligence, 10);
            npc.AbilityScores.SetScore(AbilityScoreType.Wisdom, 10);
            npc.AbilityScores.SetScore(AbilityScoreType.Charisma, 10);

            npc.Position = new WorldPosition(transform.position.x, transform.position.y, transform.position.z);

            PlayerModel = null;
            NpcModel = npc;
        }

        private void Awake()
        {
            if (IsPlayerControlled)
                InitializeAsPlayer();
            else
                InitializeAsNpc();
        }

        private void LateUpdate()
        {
            // Sync model position from Unity transform (or vice versa, depending on your architecture).
            if (PlayerModel != null)
                PlayerModel.Position = new WorldPosition(transform.position.x, transform.position.y, transform.position.z);
            else if (NpcModel != null)
                NpcModel.Position = new WorldPosition(transform.position.x, transform.position.y, transform.position.z);
        }
    }

    /// <summary>
    /// High-level controller that instantiates an Encounter from Unity scene data
    /// and wires it into the DefaultRulesEngine.
    /// </summary>
    public class EncounterController : MonoBehaviour
    {
        public EncounterData EncounterData;
        public List<CombatantView> CombatantViews = new();

        private Encounter? _encounter;
        private DefaultRulesEngine? _rulesEngine;
        private ICombatEngine? _combatEngine;

        private void Start()
        {
            BuildEncounterFromScene();
            StartCombat();
        }

        private void BuildEncounterFromScene()
        {
            if (EncounterData == null)
            {
                Debug.LogError("EncounterController: No EncounterData assigned.");
                return;
            }

            // Build World/Region/Location wrapper
            var world = new World { Name = "Unity Scene World" };
            var region = new Region { Name = "Unity Region" };
            world.Regions.Add(region);
            var location = new Location(region) { Name = EncounterData.DisplayName };
            region.Locations.Add(location);

            _encounter = new Encounter(location)
            {
                Name = EncounterData.DisplayName,
                Description = EncounterData.Description
            };

            foreach (var view in CombatantViews)
            {
                if (view == null) continue;
                _encounter.Combatants.Add(view.Model);
            }
        }

        private void StartCombat()
        {
            if (_encounter == null)
            {
                Debug.LogError("EncounterController: Encounter not built.");
                return;
            }

            _rulesEngine = new DefaultRulesEngine();
            _combatEngine = _rulesEngine.CreateCombatEngine(_encounter);

            _combatEngine.StartEncounter();
            Debug.Log($"Encounter started. Round: {_encounter.Round}, Combatants: {_encounter.Combatants.Count}");
        }
    }

    #endregion
}
