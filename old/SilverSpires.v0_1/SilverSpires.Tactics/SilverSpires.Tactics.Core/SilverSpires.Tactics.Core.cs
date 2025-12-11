
// SilverSpires Tactics – Core Class Library (v0.1)
// This is an initial high-level skeleton for the rules, character, world, and story engines.
// It is intentionally abstract and incomplete, meant as a foundation to extend.
//
// Suggested assembly name: SilverSpires.Tactics.Core
// Target: .NET 6+ / C# 10+

using System;
using System.Collections.Generic;

namespace SilverSpires.Tactics.Core
{
    #region Common Types & Enums

    public enum AbilityScoreType
    {
        Strength,
        Dexterity,
        Constitution,
        Intelligence,
        Wisdom,
        Charisma
    }

    public enum SkillType
    {
        Acrobatics,
        AnimalHandling,
        Arcana,
        Athletics,
        Deception,
        History,
        Insight,
        Intimidation,
        Investigation,
        Medicine,
        Nature,
        Perception,
        Performance,
        Persuasion,
        Religion,
        SleightOfHand,
        Stealth,
        Survival
    }

    public enum DamageType
    {
        Bludgeoning,
        Piercing,
        Slashing,
        Fire,
        Cold,
        Lightning,
        Thunder,
        Acid,
        Poison,
        Psychic,
        Radiant,
        Necrotic,
        Force
    }

    public enum ConditionType
    {
        Blinded,
        Charmed,
        Deafened,
        Frightened,
        Grappled,
        Incapacitated,
        Invisible,
        Paralyzed,
        Petrified,
        Poisoned,
        Prone,
        Restrained,
        Stunned,
        Unconscious,
        Exhaustion,
        Custom
    }

    public enum AdvantageState
    {
        Normal,
        Advantage,
        Disadvantage
    }

    public enum ActionCategory
    {
        Action,
        BonusAction,
        Reaction,
        FreeAction,
        Movement,
        LegendaryAction,
        LairAction
    }

    public enum ResourceType
    {
        HitPoints,
        TemporaryHitPoints,
        SpellSlot,
        ClassFeatureCharge,
        Custom
    }

    public readonly struct AbilityScore
    {
        public AbilityScoreType Type { get; }
        public int Score { get; }

        public AbilityScore(AbilityScoreType type, int score)
        {
            Type = type;
            Score = score;
        }

        public int Modifier => (Score - 10) / 2;
    }

    public class AbilityScores
    {
        private readonly Dictionary<AbilityScoreType, AbilityScore> _scores = new();

        public AbilityScore this[AbilityScoreType type] => _scores[type];

        public void SetScore(AbilityScoreType type, int score)
        {
            _scores[type] = new AbilityScore(type, score);
        }

        public IReadOnlyDictionary<AbilityScoreType, AbilityScore> AsReadOnly() => _scores;
    }

    public sealed class SkillProficiency
    {
        public SkillType Skill { get; init; }
        public bool IsProficient { get; init; }
        public bool HasExpertise { get; init; }
    }

    public sealed class ResourcePool
    {
        public ResourceType Type { get; init; }
        public string Key { get; init; } = string.Empty; // e.g., "HP", "SpellSlots_Level3"
        public int Current { get; private set; }
        public int Maximum { get; private set; }

        public ResourcePool(ResourceType type, string key, int maximum)
        {
            Type = type;
            Key = key;
            Maximum = maximum;
            Current = maximum;
        }

        public void Spend(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Current = Math.Max(0, Current - amount);
        }

        public void Gain(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Current = Math.Min(Maximum, Current + amount);
        }

        public void SetMaximum(int max, bool clampCurrent = true)
        {
            Maximum = max;
            if (clampCurrent)
                Current = Math.Min(Current, Maximum);
        }
    }

    public sealed class RollResult
    {
        public int BaseRoll { get; init; }
        public int Total { get; init; }
        public int Modifier { get; init; }
        public AdvantageState AdvantageState { get; init; }
        public bool IsCriticalSuccess { get; init; }
        public bool IsCriticalFailure { get; init; }
        public string? DebugInfo { get; init; }
    }

    #endregion
}

namespace SilverSpires.Tactics.Rules
{
    using SilverSpires.Tactics.Core;
    using SilverSpires.Tactics.World;

    #region Features, Effects, Conditions

    public abstract class GameEffect
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }

        /// <summary>
        /// Duration in rounds, or null for indefinite / until dispelled.
        /// </summary>
        public int? DurationRounds { get; init; }

        /// <summary>
        /// Applies initial effect (e.g., damage, buff, debuff).
        /// </summary>
        public abstract void Apply(ICombatant source, ICombatant? target, RulesContext context);

        /// <summary>
        /// Called at the start of affected combatant's turn if the effect is persistent.
        /// </summary>
        public virtual void OnRoundStart(ICombatant affected, RulesContext context) { }

        /// <summary>
        /// Called at the end of affected combatant's turn if the effect is persistent.
        /// </summary>
        public virtual void OnRoundEnd(ICombatant affected, RulesContext context) { }
    }

    public sealed class Condition
    {
        public ConditionType Type { get; init; }
        public string? CustomId { get; init; }   // for custom condition names
        public string? SourceId { get; init; }   // spell/feature that caused it
        public int? RemainingRounds { get; set; }
        public bool IsActive => RemainingRounds == null || RemainingRounds > 0;
    }

    public sealed class Feature
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public bool IsPassive { get; init; }
        public bool RequiresActivation => !IsPassive;

        // Hook for attaching mechanical behavior later
        public GameEffect? EffectTemplate { get; init; }
    }

    #endregion

    #region Items & Spells

    public abstract class ItemBase
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public int Weight { get; init; } // in some arbitrary unit
        public bool IsMagical { get; init; }
    }

    public sealed class Weapon : ItemBase
    {
        public string DamageDice { get; init; } = "1d6";
        public DamageType DamageType { get; init; }
        public bool IsFinesse { get; init; }
        public bool IsHeavy { get; init; }
        public bool IsTwoHanded { get; init; }
        public bool IsRanged { get; init; }
        public int RangeNormal { get; init; }
        public int RangeLong { get; init; }
    }

    public sealed class Armor : ItemBase
    {
        public int ArmorClass { get; init; }
        public bool AddsDexterityModifier { get; init; } = true;
        public int MaxDexterityModifier { get; init; } = int.MaxValue;
        public bool DisadvantageOnStealth { get; init; }
    }

    public sealed class Consumable : ItemBase
    {
        public GameEffect? EffectTemplate { get; init; }
    }

    public sealed class Inventory
    {
        public IList<ItemBase> Items { get; } = new List<ItemBase>();

        public void Add(ItemBase item) => Items.Add(item);
        public bool Remove(ItemBase item) => Items.Remove(item);
    }

    public sealed class Spell
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public int Level { get; init; }
        public string School { get; init; } = string.Empty;
        public string CastingTime { get; init; } = "1 Action";
        public string Range { get; init; } = "Self";
        public string Components { get; init; } = "V,S";
        public string Duration { get; init; } = "Instantaneous";
        public bool RequiresConcentration { get; init; }
        public GameEffect? EffectTemplate { get; init; }
    }

    #endregion

    #region Actors & Actions

    public interface ITargetable
    {
        Guid Id { get; }
        string Name { get; }
        WorldPosition Position { get; }
    }

    public interface ICombatant : ITargetable
    {
        int Level { get; }
        AbilityScores AbilityScores { get; }
        int ProficiencyBonus { get; }
        IReadOnlyList<SkillProficiency> SkillProficiencies { get; }
        IReadOnlyList<Condition> Conditions { get; }
        Inventory Inventory { get; }
        IReadOnlyList<ResourcePool> Resources { get; }

        void ApplyDamage(int amount, DamageType type, RulesContext context);
        void Heal(int amount, RulesContext context);
        void ApplyCondition(Condition condition, RulesContext context);
        void RemoveCondition(Condition condition, RulesContext context);
    }

    public abstract class GameAction
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public string Name { get; init; } = string.Empty;
        public ActionCategory Category { get; init; }

        public abstract bool CanExecute(ICombatant actor, RulesContext context);
        public abstract void Execute(ICombatant actor, RulesContext context);
    }

    public sealed class MoveAction : GameAction
    {
        public WorldPosition Destination { get; init; }
        public float MaxDistance { get; init; } // in game units

        public override bool CanExecute(ICombatant actor, RulesContext context)
        {
            // Placeholder: check distance, movement remaining, terrain, etc.
            return true;
        }

        public override void Execute(ICombatant actor, RulesContext context)
        {
            // Placeholder: integrate with navmesh / movement system
            throw new NotImplementedException();
        }
    }

    public sealed class AttackAction : GameAction
    {
        public ITargetable Target { get; init; }
        public Weapon Weapon { get; init; }

        public override bool CanExecute(ICombatant actor, RulesContext context)
        {
            // Check range, line of sight, etc.
            return true;
        }

public override void Execute(ICombatant actor, RulesContext context)
{
    if (Target is not ICombatant target)
        return;

    // Determine which ability to use: Dex for finesse/ranged, otherwise Str.
    var scores = actor.AbilityScores;
    int strMod = scores[AbilityScoreType.Strength].Modifier;
    int dexMod = scores[AbilityScoreType.Dexterity].Modifier;
    int abilityMod = (Weapon.IsRanged || Weapon.IsFinesse) ? dexMod : strMod;

    int attackModifier = abilityMod + actor.ProficiencyBonus;
    var roll = context.RollService.RollD20(AdvantageState.Normal, attackModifier,
        $"Attack: {actor.Name} -> {target.Name} with {Weapon.Name}");

    bool hit = !roll.IsCriticalFailure; // Placeholder: hook in target AC later.
    if (!hit)
        return;

    // Roll damage
    int baseDamage = context.RollService.RollDiceExpression(Weapon.DamageDice,
        $"Damage: {Weapon.DamageDice}");
    if (roll.IsCriticalSuccess)
    {
        baseDamage += context.RollService.RollDiceExpression(Weapon.DamageDice,
            $"Crit extra: {Weapon.DamageDice}");
    }

    target.ApplyDamage(baseDamage, Weapon.DamageType, context);
}
    }

    public sealed class CastSpellAction : GameAction
    {
        public Spell Spell { get; init; }
        public ITargetable? Target { get; init; }

        public override bool CanExecute(ICombatant actor, RulesContext context)
        {
            // Check resources (spell slots), conditions (silence, etc.), and line of sight.
            return true;
        }

        public override void Execute(ICombatant actor, RulesContext context)
        {
            // Spend slot, apply GameEffect, etc.
            throw new NotImplementedException();
        }
    }

    #endregion

    #region Rules Engine

    public sealed class RulesContext
    {
        public Encounter Encounter { get; init; }
        public IRollService RollService { get; init; }

        public RulesContext(Encounter encounter, IRollService rollService)
        {
            Encounter = encounter;
            RollService = rollService;
        }
    }

    public interface IRollService
    {
        RollResult RollD20(AdvantageState advantageState, int modifier = 0, string? debugContext = null);
        int RollDiceExpression(string expression, string? debugContext = null);
    }

    public interface ICombatEngine
    {
        Encounter Encounter { get; }

        void StartEncounter();
        void EndEncounter();
        void ProcessTurn(ICombatant activeCombatant);
        void ApplyOngoingEffects();
    }

    public interface IRulesEngine
    {
        IRollService RollService { get; }
        ICombatEngine CreateCombatEngine(Encounter encounter);
    }

    #endregion
}

namespace SilverSpires.Tactics.World
{
    using System.Numerics;
    using SilverSpires.Tactics.Characters;
    using SilverSpires.Tactics.Core;
    using SilverSpires.Tactics.Rules;

    #region World & Story

    public readonly struct WorldPosition
    {
        public Vector3 Value { get; }

        public WorldPosition(float x, float y, float z)
        {
            Value = new Vector3(x, y, z);
        }

        public float X => Value.X;
        public float Y => Value.Y;
        public float Z => Value.Z;
    }

    public sealed class World
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public string Name { get; init; } = "Silver Spires / Veilrend";
        public IList<Region> Regions { get; } = new List<Region>();
    }

    public sealed class Region
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public IList<Location> Locations { get; } = new List<Location>();
        public IList<Shatter> Shatters { get; } = new List<Shatter>();

        // Hooks for Shatter-related metadata can be added here later.
    }

    public sealed class Location
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public Region Region { get; init; }
        public IList<Encounter> Encounters { get; } = new List<Encounter>();

        public Location(Region region)
        {
            Region = region;
        }
    }

    public sealed class Encounter
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public Location Location { get; init; }
        public IList<ICombatant> Combatants { get; } = new List<ICombatant>();
        public IList<Shatter> ActiveShatters { get; } = new List<Shatter>();
        public int Round { get; set; } = 0;

        public Encounter(Location location)
        {
            Location = location;
        }
    }

    public sealed class DialogueNode
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public string SpeakerId { get; init; } = string.Empty; // actor/character ID
        public string Text { get; init; } = string.Empty;
        public IList<DialogueChoice> Choices { get; } = new List<DialogueChoice>();
    }

    public sealed class DialogueChoice
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public string Text { get; init; } = string.Empty;
        public string? RequiredFlag { get; init; }
        public string? SetFlag { get; init; }
        public string? NextNodeId { get; init; }
    }

    public sealed class Quest
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public QuestState State { get; set; } = QuestState.NotStarted;
        public IList<string> Flags { get; } = new List<string>();
    }

    public enum QuestState
    {
        NotStarted,
        InProgress,
        Completed,
        Failed
    }

    public sealed class StoryState
    {
        public string CampaignId { get; init; } = Guid.NewGuid().ToString();
        public IDictionary<string, bool> Flags { get; } = new Dictionary<string, bool>();
        public IDictionary<string, Quest> Quests { get; } = new Dictionary<string, Quest>();

        public bool GetFlag(string key) => Flags.TryGetValue(key, out var value) && value;
        public void SetFlag(string key, bool value) => Flags[key] = value;

        public void AddQuest(Quest quest) => Quests[quest.Id] = quest;
    }

    public sealed class Campaign
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public string Name { get; init; } = string.Empty;
        public World World { get; init; } = new World();
        public StoryState StoryState { get; init; } = new StoryState();
        public IList<ICombatant> Party { get; } = new List<ICombatant>();
    }

    #endregion
}

namespace SilverSpires.Tactics.Characters
{
    using SilverSpires.Tactics.Core;
    using SilverSpires.Tactics.Rules;
    using SilverSpires.Tactics.World;
    using System.Collections.ObjectModel;

    #region Characters & Creation

    public enum Alignment
    {
        LawfulGood,
        NeutralGood,
        ChaoticGood,
        LawfulNeutral,
        TrueNeutral,
        ChaoticNeutral,
        LawfulEvil,
        NeutralEvil,
        ChaoticEvil,
        Unaligned
    }

    public sealed class CharacterRace
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public IDictionary<AbilityScoreType, int> AbilityScoreBonuses { get; } = new Dictionary<AbilityScoreType, int>();
        public IList<Feature> Features { get; } = new List<Feature>();
    }

    public sealed class CharacterClass
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public int HitDie { get; init; } = 8;
        public IList<Feature> ClassFeatures { get; } = new List<Feature>();
    }

    public sealed class CharacterBackground
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public IList<SkillType> Proficiencies { get; } = new List<SkillType>();
        public IList<Feature> Features { get; } = new List<Feature>();
    }

    public sealed class PlayerCharacter : ICombatant
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; }
        public Alignment Alignment { get; set; }
        public CharacterRace Race { get; set; } = new CharacterRace();
        public CharacterClass Class { get; set; } = new CharacterClass();
        public CharacterBackground Background { get; set; } = new CharacterBackground();

        public AbilityScores AbilityScores { get; } = new AbilityScores();
        private readonly List<SkillProficiency> _skillProficiencies = new();
        private readonly List<Condition> _conditions = new();
        private readonly List<ResourcePool> _resources = new();

        public int ProficiencyBonus => Math.Max(2, 2 + (Level - 1) / 4);

        public IReadOnlyList<SkillProficiency> SkillProficiencies => new ReadOnlyCollection<SkillProficiency>(_skillProficiencies);
        public IReadOnlyList<Condition> Conditions => new ReadOnlyCollection<Condition>(_conditions);
        public IReadOnlyList<ResourcePool> Resources => new ReadOnlyCollection<ResourcePool>(_resources);

        public Inventory Inventory { get; } = new Inventory();

        public WorldPosition Position { get; set; } = new WorldPosition(0, 0, 0);

        public void ApplyDamage(int amount, DamageType type, RulesContext context)
        {
            // Placeholder: route to HP resource, consider resistances, etc.
            throw new NotImplementedException();
        }

        public void Heal(int amount, RulesContext context)
        {
            // Placeholder: restore HP resource
            throw new NotImplementedException();
        }

        public void ApplyCondition(Condition condition, RulesContext context)
        {
            _conditions.Add(condition);
        }

        public void RemoveCondition(Condition condition, RulesContext context)
        {
            _conditions.Remove(condition);
        }

        // Helpers for character creation
        public void AddSkillProficiency(SkillProficiency proficiency) => _skillProficiencies.Add(proficiency);
        public void AddResource(ResourcePool resource) => _resources.Add(resource);
    }

    public sealed class NonPlayerCharacter : ICombatant
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; }

        public AbilityScores AbilityScores { get; } = new AbilityScores();
        private readonly List<SkillProficiency> _skillProficiencies = new();
        private readonly List<Condition> _conditions = new();
        private readonly List<ResourcePool> _resources = new();

        public int ProficiencyBonus => Math.Max(2, 2 + (Level - 1) / 4);

        public IReadOnlyList<SkillProficiency> SkillProficiencies => new ReadOnlyCollection<SkillProficiency>(_skillProficiencies);
        public IReadOnlyList<Condition> Conditions => new ReadOnlyCollection<Condition>(_conditions);
        public IReadOnlyList<ResourcePool> Resources => new ReadOnlyCollection<ResourcePool>(_resources);

        public Inventory Inventory { get; } = new Inventory();
        public WorldPosition Position { get; set; } = new WorldPosition(0, 0, 0);

        public void ApplyDamage(int amount, DamageType type, RulesContext context)
        {
            // Placeholder for creatures / NPCs
            throw new NotImplementedException();
        }

        public void Heal(int amount, RulesContext context)
        {
            // Placeholder
            throw new NotImplementedException();
        }

        public void ApplyCondition(Condition condition, RulesContext context)
        {
            _conditions.Add(condition);
        }

        public void RemoveCondition(Condition condition, RulesContext context)
        {
            _conditions.Remove(condition);
        }

        public void AddSkillProficiency(SkillProficiency proficiency) => _skillProficiencies.Add(proficiency);
        public void AddResource(ResourcePool resource) => _resources.Add(resource);
    }


    public enum ShatterModifierType
    {
        DamageBonus,
        DamageResistance,
        MovementModifier,
        SpellEffectModifier,
        EnvironmentalHazard,
        Custom
    }

    public sealed class ShatterModifier
    {
        public ShatterModifierType Type { get; init; }
        /// <summary>
        /// A key indicating what this modifier applies to, e.g., "necrotic", "ranged-attacks", "movement-speed".
        /// </summary>
        public string Key { get; init; } = string.Empty;
        /// <summary>
        /// A numeric value; its meaning depends on the modifier type (percentage, flat bonus, etc.).
        /// </summary>
        public float Value { get; init; }
        /// <summary>
        /// Optional human-readable description of the modifier's rule.
        /// </summary>
        public string? RulesText { get; init; }
    }

    /// <summary>
    /// A Shatter is a Veil fracture that alters local rules, encounters, and environment.
    /// </summary>
    public sealed class Shatter
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }

        /// <summary>
        /// Tags used for encounter generation / biome logic (e.g., "gravity-thin", "void-flora").
        /// </summary>
        public IList<string> Tags { get; } = new List<string>();

        /// <summary>
        /// Concrete rule modifiers applied while this Shatter is active.
        /// </summary>
        public IList<ShatterModifier> Modifiers { get; } = new List<ShatterModifier>();
    }

    #endregion
}

namespace SilverSpires.Tactics.Rules
{
    using System.Linq;
    using SilverSpires.Tactics.Core;
    using SilverSpires.Tactics.World;

    /// <summary>
    /// Basic random roll service. Replace or extend with seeded RNG for deterministic multiplayer.
    /// </summary>
    public sealed class RandomRollService : IRollService
    {
        private readonly Random _rng;

        public RandomRollService(int? seed = null)
        {
            _rng = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public RollResult RollD20(AdvantageState advantageState, int modifier = 0, string? debugContext = null)
        {
            int RollSingle() => _rng.Next(1, 21);

            int roll1 = RollSingle();
            int roll2 = RollSingle();
            int baseRoll = roll1;
            bool critSuccess = false;
            bool critFailure = false;

            switch (advantageState)
            {
                case AdvantageState.Advantage:
                    baseRoll = Math.Max(roll1, roll2);
                    break;
                case AdvantageState.Disadvantage:
                    baseRoll = Math.Min(roll1, roll2);
                    break;
                default:
                    baseRoll = roll1;
                    break;
            }

            critSuccess = baseRoll == 20;
            critFailure = baseRoll == 1;

            int total = baseRoll + modifier;

            return new RollResult
            {
                BaseRoll = baseRoll,
                Total = total,
                Modifier = modifier,
                AdvantageState = advantageState,
                IsCriticalSuccess = critSuccess,
                IsCriticalFailure = critFailure,
                DebugInfo = debugContext
            };
        }

        public int RollDiceExpression(string expression, string? debugContext = null)
        {
            // Very simple dice parser: supports NdM+K, e.g., "2d6+3", "1d8", "3d10-1".
            // This is intentionally minimal; replace with a robust dice parser later.
            string expr = expression.Replace(" ", string.Empty).ToLowerInvariant();
            int flatBonus = 0;

            int plusIndex = expr.IndexOf('+');
            int minusIndex = expr.LastIndexOf('-');

            int signIndex = -1;
            int sign = 1;

            if (plusIndex > 0)
            {
                signIndex = plusIndex;
                sign = 1;
            }
            else if (minusIndex > 0)
            {
                signIndex = minusIndex;
                sign = -1;
            }

            if (signIndex > 0)
            {
                var bonusPart = expr.Substring(signIndex + 1);
                if (int.TryParse(bonusPart, out var parsedBonus))
                    flatBonus = sign * parsedBonus;
                expr = expr.Substring(0, signIndex);
            }

            int dIndex = expr.IndexOf('d');
            if (dIndex <= 0)
                throw new ArgumentException($"Invalid dice expression: {expression}", nameof(expression));

            var countPart = expr.Substring(0, dIndex);
            var sizePart = expr.Substring(dIndex + 1);

            if (!int.TryParse(countPart, out var count))
                throw new ArgumentException($"Invalid dice count: {expression}", nameof(expression));
            if (!int.TryParse(sizePart, out var size))
                throw new ArgumentException($"Invalid dice size: {expression}", nameof(expression));

            int total = 0;
            for (int i = 0; i < count; i++)
            {
                total += _rng.Next(1, size + 1);
            }

            return total + flatBonus;
        }
    }

    /// <summary>
    /// A simple turn-based combat engine that iterates through combatants in initiative order.
    /// This is intentionally minimal and meant as a starting point.
    /// </summary>
    public sealed class BasicCombatEngine : ICombatEngine
    {
        private readonly RulesContext _context;
        private readonly List<ICombatant> _initiativeOrder = new();
        private int _currentIndex = -1;
        private bool _started;

        public Encounter Encounter { get; }

        public BasicCombatEngine(Encounter encounter, IRollService rollService)
        {
            Encounter = encounter;
            _context = new RulesContext(encounter, rollService);
        }

        public void StartEncounter()
        {
            if (_started) return;
            _started = true;
            Encounter.Round = 1;
            BuildInitiativeOrder();
        }

        private void BuildInitiativeOrder()
        {
            _initiativeOrder.Clear();
            _initiativeOrder.AddRange(Encounter.Combatants);

            // Very simple initiative: roll d20 + Dex mod. Replace with full rules later.
            var rng = new Random();
            _initiativeOrder.Sort((a, b) =>
            {
                int aDex = a.AbilityScores[AbilityScoreType.Dexterity].Modifier;
                int bDex = b.AbilityScores[AbilityScoreType.Dexterity].Modifier;
                int aRoll = rng.Next(1, 21) + aDex;
                int bRoll = rng.Next(1, 21) + bDex;
                return bRoll.CompareTo(aRoll);
            });

            _currentIndex = 0;
        }

        public void EndEncounter()
        {
            _started = false;
            _initiativeOrder.Clear();
        }

        public void ProcessTurn(ICombatant activeCombatant)
        {
            if (!_started) return;
            if (!_initiativeOrder.Contains(activeCombatant)) return;

            // TODO: integrate with an AI / player control layer.
            // For now, this is a hook to call into from outside (e.g., Unity, a controller, or tests).

            // After the active combatant finishes their actions, advance to next.
            NextTurn();
        }

        private void NextTurn()
        {
            if (_initiativeOrder.Count == 0) return;

            _currentIndex++;
            if (_currentIndex >= _initiativeOrder.Count)
            {
                _currentIndex = 0;
                Encounter.Round++;
                ApplyOngoingEffects();
            }

            // You would typically raise an event or callback here to drive the UI/clients.
            var next = _initiativeOrder[_currentIndex];
            // e.g., OnTurnStarted?.Invoke(next, _context);
        }

        public void ApplyOngoingEffects()
        {
            // Iterate over combatants and conditions/effects and call OnRoundStart/OnRoundEnd as needed.
            foreach (var combatant in Encounter.Combatants.ToList())
            {
                foreach (var condition in combatant.Conditions.ToList())
                {
                    if (condition.RemainingRounds.HasValue)
                    {
                        condition.RemainingRounds -= 1;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Default implementation of IRulesEngine that wires together a roll service and combat engine.
    /// </summary>
    public sealed class DefaultRulesEngine : IRulesEngine
    {
        public IRollService RollService { get; }

        public DefaultRulesEngine(IRollService? rollService = null)
        {
            RollService = rollService ?? new RandomRollService();
        }

        public ICombatEngine CreateCombatEngine(Encounter encounter)
        {
            return new BasicCombatEngine(encounter, RollService);
        }
    }
}
