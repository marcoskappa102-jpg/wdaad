namespace MMOServer.Models
{
    public class CombatAction
    {
        public string attackerId { get; set; } = "";
        public string targetId { get; set; } = "";
        public string attackerType { get; set; } = "player"; // "player" ou "monster"
        public string targetType { get; set; } = "monster"; // "player" ou "monster"
        public int damage { get; set; }
        public bool isCritical { get; set; } = false;
        public string damageType { get; set; } = "physical"; // "physical" ou "magical"
    }
    
    public class CombatResult
    {
        public string attackerId { get; set; } = "";
        public string targetId { get; set; } = "";
        public string attackerType { get; set; } = "";
        public string targetType { get; set; } = "";
        public int damage { get; set; }
        public bool isCritical { get; set; }
        public int remainingHealth { get; set; }
        public bool targetDied { get; set; }
        public int experienceGained { get; set; } = 0;
        public bool leveledUp { get; set; } = false;
        public int newLevel { get; set; } = 0;
    }
}