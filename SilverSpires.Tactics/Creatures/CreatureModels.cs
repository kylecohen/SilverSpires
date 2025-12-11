using SilverSpires.Tactics.Maps;
using SilverSpires.Tactics.Srd.Monsters;
using SilverSpires.Tactics.Srd.Rules;

namespace SilverSpires.Tactics.Creatures
{
    public sealed class CreatureStats
    {
        public string TemplateId { get; }
        public string Name { get; }

        public SizeCategory Size { get; }
        public CreatureType CreatureType { get; }

        public int ArmorClass { get; }
        public int MaxHitPoints { get; }

        public int Strength { get; }
        public int Dexterity { get; }
        public int Constitution { get; }
        public int Intelligence { get; }
        public int Wisdom { get; }
        public int Charisma { get; }

        public int SpeedFeet { get; }
        public ChallengeRating ChallengeRating { get; }

        public CreatureStats(
            string templateId,
            string name,
            SizeCategory size,
            CreatureType creatureType,
            int armorClass,
            int maxHitPoints,
            int strength,
            int dexterity,
            int constitution,
            int intelligence,
            int wisdom,
            int charisma,
            int speedFeet,
            ChallengeRating challengeRating)
        {
            TemplateId = templateId;
            Name = name;
            Size = size;
            CreatureType = creatureType;
            ArmorClass = armorClass;
            MaxHitPoints = maxHitPoints;
            Strength = strength;
            Dexterity = dexterity;
            Constitution = constitution;
            Intelligence = intelligence;
            Wisdom = wisdom;
            Charisma = charisma;
            SpeedFeet = speedFeet;
            ChallengeRating = challengeRating;
        }

        public CreatureStats(SrdMonster monster)
            : this(
                monster.Id,
                monster.Name,
                monster.Size,
                monster.Type,
                monster.ArmorClass,
                monster.HitPointsAverage,
                monster.Strength,
                monster.Dexterity,
                monster.Constitution,
                monster.Intelligence,
                monster.Wisdom,
                monster.Charisma,
                monster.SpeedWalk,
                monster.ChallengeRating)
        {
        }

        public static int AbilityMod(int score) => (score - 10) / 2;

        public int SpeedTiles => SpeedFeet / 5;
    }

    public sealed class CreatureInstance
    {
        public Guid Id { get; } = Guid.NewGuid();
        public CreatureStats Stats { get; }

        public GridPosition Position { get; private set; }
        public int CurrentHitPoints { get; private set; }

        public bool IsAlive => CurrentHitPoints > 0;

        public CreatureInstance(CreatureStats stats, GridPosition startPosition)
        {
            Stats = stats;
            Position = startPosition;
            CurrentHitPoints = stats.MaxHitPoints;
        }

        public void MoveTo(GridPosition newPosition) => Position = newPosition;

        public void ApplyDamage(int amount)
        {
            if (amount <= 0) return;
            CurrentHitPoints = Math.Max(0, CurrentHitPoints - amount);
        }

        public void Heal(int amount)
        {
            if (amount <= 0) return;
            CurrentHitPoints = Math.Min(Stats.MaxHitPoints, CurrentHitPoints + amount);
        }
    }
}
