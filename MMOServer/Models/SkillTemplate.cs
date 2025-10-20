using System;
using System.Collections.Generic;

namespace MMOServer.Models
{
    /// <summary>
    /// Template de skill - Define as propriedades e comportamentos da habilidade
    /// Baseado no sistema Ragnarok Online com melhorias
    /// </summary>
    [Serializable]
    public class SkillTemplate
    {
        public int id { get; set; }
        public string name { get; set; } = "";
        public string description { get; set; } = "";
        public int classRequired { get; set; } // 0 = todas classes, 1 = Guerreiro, 2 = Mago, 3 = Arqueiro, 4 = Clerigo
        public int levelRequired { get; set; }
        public int skillPointsRequired { get; set; }
        
        // ========== MANA E CUSTO ==========
        public int manaCost { get; set; }
        public int healthCost { get; set; } // Alguns skills custam HP
        public float cooldown { get; set; } // Em segundos
        public float castTime { get; set; } // Tempo de cast em segundos
        public int range { get; set; } // Alcance em unidades
        
        // ========== TIPO DE SKILL ==========
        public string skillType { get; set; } = ""; // "attack", "heal", "buff", "debuff", "area"
        public string targetType { get; set; } = ""; // "single", "aoe", "self", "party", "ground"
        
        // ========== DANO E EFEITOS ==========
        public int baseDamage { get; set; }
        public float damageMultiplier { get; set; } // Multiplicador de ATK/INT
        public int minDamage { get; set; }
        public int maxDamage { get; set; }
        public string damageType { get; set; } = "physical"; // "physical", "magical", "pure"
        
        // ========== ÁREA DE EFEITO ==========
        public float aoeRadius { get; set; } // Raio de área (0 = single target)
        public int maxTargets { get; set; } // Máximo de alvo em AOE (-1 = ilimitado)
        
        // ========== BUFFS E EFEITOS ==========
        public string effectType { get; set; } = ""; // "damage", "heal", "buff", "debuff", "cc"
        public int effectValue { get; set; }
        public string effectTarget { get; set; } = ""; // "health", "mana", "str", "def", "dex", "int", "vit"
        public float effectDuration { get; set; } // Duração do buff/debuff em segundos
        
        // ========== CARACTERÍSTICAS ESPECIAIS ==========
        public int criticalChance { get; set; } // Chance de crítico em %
        public int knockback { get; set; } // Força do knockback
        public bool canMiss { get; set; } = true;
        public bool blockable { get; set; } = true;
        public bool reflectable { get; set; } = false;
        
        // ========== REQUERIMENTOS ==========
        public List<int> requiredSkills { get; set; } = new List<int>(); // IDs de skills requeridos
        public int requiredWeaponType { get; set; } = -1; // -1 = qualquer, 0 = sem arma, 1 = espada, 2 = cajado, 3 = arco
        
        // ========== FÓRMULAS E ESCALAS ==========
        public float strScale { get; set; } = 0f; // Como STR influencia o skill
        public float intScale { get; set; } = 0f; // Como INT influencia o skill
        public float dexScale { get; set; } = 0f; // Como DEX influencia o skill
        public float vitScale { get; set; } = 0f; // Como VIT influencia o skill
        public float levelScale { get; set; } = 1.0f; // Como level influencia o skill
        
        // ========== PROJECTIL (para skills de projeção) ==========
        public bool hasProjectile { get; set; } = false;
        public float projectileSpeed { get; set; } = 0f;
        public string projectilePath { get; set; } = "";
        
        // ========== VISUAL E SOM ==========
        public string effectPath { get; set; } = ""; // Caminho do efeito visual
        public string soundPath { get; set; } = ""; // Caminho do som
        public float animationDuration { get; set; } = 0f; // Duração da animação
        
        // ========== IMAGEM ==========
        public string iconPath { get; set; } = "";
    }

    /// <summary>
    /// Instância de skill que um player possui
    /// </summary>
    [Serializable]
    public class SkillInstance
    {
        public int skillId { get; set; }
        public int characterId { get; set; }
        public int level { get; set; } = 1; // Alguns skills aumentam de nível com pontos
        public int exp { get; set; } = 0; // EXP do skill
        
        // Uso
        public DateTime lastCastTime { get; set; } = DateTime.MinValue;
        public bool isLearned { get; set; } = false;
    }

    /// <summary>
    /// Resultado de um skill cast
    /// </summary>
    [Serializable]
    public class SkillCastResult
    {
        public int casterId { get; set; }
        public string casterName { get; set; } = "";
        public string casterType { get; set; } = "player"; // "player" ou "monster"
        
        public int skillId { get; set; }
        public string skillName { get; set; } = "";
        
        public bool success { get; set; }
        public string failReason { get; set; } = ""; // "out_of_range", "no_mana", "on_cooldown", etc.
        
        public List<SkillTargetResult> targetResults { get; set; } = new List<SkillTargetResult>();
        
        public float castTime { get; set; }
        public DateTime castTime_Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Resultado para cada alvo afetado pelo skill
    /// </summary>
    [Serializable]
    public class SkillTargetResult
    {
        public int targetId { get; set; }
        public string targetName { get; set; } = "";
        public string targetType { get; set; } = ""; // "player" ou "monster"
        
        public int damage { get; set; }
        public int heal { get; set; }
        
        public bool isCritical { get; set; }
        public bool isMiss { get; set; }
        public bool isEvaded { get; set; }
        
        public int remainingHealth { get; set; }
        public int remainingMana { get; set; }
        
        public bool died { get; set; }
        
        // Buffs/Debuffs aplicados
        public List<ActiveBuff> appliedBuffs { get; set; } = new List<ActiveBuff>();
    }

    /// <summary>
    /// Buff ou Debuff ativo no personagem
    /// </summary>
    [Serializable]
    public class ActiveBuff
    {
        public int buffId { get; set; }
        public string buffName { get; set; } = "";
        public string skillName { get; set; } = "";
        
        public int skillId { get; set; }
        public int casterId { get; set; }
        
        public string buffType { get; set; } = ""; // "buff" ou "debuff"
        public string effectType { get; set; } = ""; // "damage", "heal", "stat_boost", "stat_reduction", "cc", etc.
        
        // Valores do buff
        public int statBoost { get; set; } // Quanto aumenta/diminui (pode ser STR, INT, etc)
        public string affectedStat { get; set; } = ""; // "str", "int", "dex", "vit", "def", "atk", etc.
        
        public float remainingDuration { get; set; }
        public DateTime applicationTime { get; set; }
        
        public bool isActive { get; set; } = true;
    }

    /// <summary>
    /// Configuração de classe de skill
    /// </summary>
    [Serializable]
    public class SkillConfig
    {
        public List<SkillTemplate> skills { get; set; } = new List<SkillTemplate>();
    }
}