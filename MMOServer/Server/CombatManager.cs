using MMOServer.Models;

namespace MMOServer.Server
{
    public class CombatManager
    {
        private static CombatManager? instance;
        public static CombatManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new CombatManager();
                return instance;
            }
        }

        private Random random = new Random();
        
        // ✅ BALANCEAMENTO MELHORADO
        private const float ATTACK_RANGE = 3.5f; // 1 célula de range
        private const float CRITICAL_MULTIPLIER = 1.5f; // 150% de dano (aumentado de 140%)
        
        // ✅ NOVOS: Garantias de dano mínimo
        private const int MIN_DAMAGE_PERCENT = 10; // 10% do ATK nunca é reduzido por DEF
        private const int ABSOLUTE_MIN_DAMAGE = 1; // Dano mínimo absoluto

        // ==========================================
        // PLAYER ATACA MONSTER
        // ==========================================
        public CombatResult PlayerAttackMonster(Player player, MonsterInstance monster)
        {
            if (player.character.isDead || !monster.isAlive)
            {
                return new CombatResult { damage = 0 };
            }

            // ✅ Verifica range (2D - ignora Y)
            if (!IsInAttackRange(player.position, monster.position, ATTACK_RANGE))
            {
                return new CombatResult { damage = 0 };
            }

            // === CÁLCULO DE HIT (MELHORADO) ===
            // HIT = 175 + DEX + BaseLv
            int playerHit = 175 + player.character.dexterity + player.character.level;
            
            // FLEE do monstro (reduzido para dar mais hit ao player)
            int monsterFlee = 100 + monster.template.level + monster.template.defense;
            
            // Hit Chance = 80% base + (HIT - FLEE) / 10
            float hitChance = 0.80f + ((playerHit - monsterFlee) / 100f);
            hitChance = Math.Clamp(hitChance, 0.30f, 0.95f); // Mínimo 30%, Máximo 95%
            
            double hitRoll = random.NextDouble();
            
            if (hitRoll > hitChance)
            {
                // MISS!
                Console.WriteLine($"❌ {player.character.nome} MISSED {monster.template.name} (roll:{hitRoll:F2} vs chance:{hitChance:F2})");
                
                return new CombatResult
                {
                    attackerId = player.sessionId,
                    targetId = monster.id.ToString(),
                    attackerType = "player",
                    targetType = "monster",
                    damage = 0,
                    isCritical = false,
                    remainingHealth = monster.currentHealth,
                    targetDied = false
                };
            }

            // === CÁLCULO DE CRITICAL (MELHORADO) ===
            // Base: 1% + (DEX * 0.3)%
            float baseCritChance = 0.01f + (player.character.dexterity * 0.003f);
            
            // Bonus por LUK (se tiver) - aqui usamos Level como substituto
            float lukBonus = player.character.level * 0.001f; // +0.1% por level
            
            float critChance = baseCritChance + lukBonus;
            critChance = Math.Clamp(critChance, 0.01f, 0.50f); // 1% a 50%
            
            bool isCritical = random.NextDouble() < critChance;

            // === CÁLCULO DE DANO (MELHORADO) ===
            
            // 1. ATK Base do Player
            int baseATK = player.character.attackPower;
            
            // 2. Variação de ATK (±5%)
            float atkVariance = 0.95f + ((float)random.NextDouble() * 0.10f); // 0.95 a 1.05
            int variedATK = (int)(baseATK * atkVariance);
            
            // 3. Multiplicador de STR
            float strMultiplier = 1.0f + (player.character.strength / 100f);
            int damage = (int)(variedATK * strMultiplier);
            
            // 4. Dano garantido (10% do ATK ignora defesa)
            int guaranteedDamage = (int)(damage * (MIN_DAMAGE_PERCENT / 100f));
            int defensibleDamage = damage - guaranteedDamage;
            
            // 5. Redução de defesa (só aplica na parte defensável)
            int monsterDEF = monster.template.defense;
            float defReduction = 1.0f - (monsterDEF / (float)(monsterDEF + 100));
            defReduction = Math.Max(defReduction, 0.1f); // Mínimo 10% do dano passa
            
            int finalDefensibleDamage = (int)(defensibleDamage * defReduction);
            damage = guaranteedDamage + finalDefensibleDamage;
            
            // 6. Aplicar crítico (multiplica o dano total)
            if (isCritical)
            {
                damage = (int)(damage * CRITICAL_MULTIPLIER);
            }
            
            // 7. Dano mínimo absoluto
            damage = Math.Max(damage, ABSOLUTE_MIN_DAMAGE);

            // === LOG DETALHADO ===
            Console.WriteLine($"⚔️ {player.character.nome} -> {monster.template.name}:");
            Console.WriteLine($"   ATK:{baseATK} STR:{player.character.strength} DEX:{player.character.dexterity}");
            Console.WriteLine($"   Monster DEF:{monsterDEF} HP:{monster.currentHealth}/{monster.template.maxHealth}");
            Console.WriteLine($"   Hit Roll:{hitRoll:F2} vs {hitChance:F2} = HIT!");
            Console.WriteLine($"   Crit Roll:{random.NextDouble():F2} vs {critChance:F2} = {(isCritical ? "CRIT!" : "Normal")}");
            Console.WriteLine($"   Final Damage: {damage} ({guaranteedDamage} guaranteed + {finalDefensibleDamage} after def)");

            // Aplica dano ao monstro
            int actualDamage = monster.TakeDamage(damage);

            // Log de combate no banco
            try
            {
                DatabaseHandler.Instance.LogCombat(player.character.id, monster.id, actualDamage, "physical", isCritical);
            }
            catch { }

            var result = new CombatResult
            {
                attackerId = player.sessionId,
                targetId = monster.id.ToString(),
                attackerType = "player",
                targetType = "monster",
                damage = actualDamage,
                isCritical = isCritical,
                remainingHealth = monster.currentHealth,
                targetDied = !monster.isAlive
            };

            // Se matou o monstro, ganha XP
            if (result.targetDied)
            {
                int expGained = CalculateExperienceReward(player.character.level, monster.template.level, monster.template.experienceReward);
                bool leveledUp = player.character.GainExperience(expGained);

                result.experienceGained = expGained;
                result.leveledUp = leveledUp;
                result.newLevel = player.character.level;

                Console.WriteLine($"💀 {monster.template.name} died! {player.character.nome} gained {expGained} XP");

                // Atualiza no banco
                DatabaseHandler.Instance.UpdateCharacter(player.character);
            }

            return result;
        }

        // ==========================================
        // MONSTER ATACA PLAYER
        // ==========================================
        public CombatResult MonsterAttackPlayer(MonsterInstance monster, Player player)
        {
            if (!monster.isAlive || player.character.isDead)
            {
                return new CombatResult { damage = 0 };
            }

            // Verifica range
            if (!IsInAttackRange(monster.position, player.position, ATTACK_RANGE))
            {
                return new CombatResult { damage = 0 };
            }

            // === CÁLCULO DE HIT ===
            int monsterHit = 175 + monster.template.level + (monster.template.attackPower / 5);
            
            // FLEE do player (melhorado)
            int playerFlee = 100 + player.character.level + player.character.dexterity + (player.character.dexterity / 5);
            
            float hitChance = 0.80f + ((monsterHit - playerFlee) / 100f);
            hitChance = Math.Clamp(hitChance, 0.30f, 0.95f);
            
            double hitRoll = random.NextDouble();
            
            if (hitRoll > hitChance)
            {
                // MISS!
                Console.WriteLine($"❌ {monster.template.name} MISSED {player.character.nome} (roll:{hitRoll:F2} vs chance:{hitChance:F2})");
                
                return new CombatResult
                {
                    attackerId = monster.id.ToString(),
                    targetId = player.sessionId,
                    attackerType = "monster",
                    targetType = "player",
                    damage = 0,
                    isCritical = false,
                    remainingHealth = player.character.health,
                    targetDied = false
                };
            }

            // Monstros têm chance menor de crítico (2% + 0.1% por level)
            float critChance = 0.02f + (monster.template.level * 0.001f);
            critChance = Math.Clamp(critChance, 0.02f, 0.15f); // 2% a 15%
            
            bool isCritical = random.NextDouble() < critChance;

            // === CÁLCULO DE DANO DO MONSTRO ===
            
            // 1. ATK base do monstro
            int baseATK = monster.template.attackPower;
            
            // 2. Variação (±10% para monstros)
            float atkVariance = 0.90f + ((float)random.NextDouble() * 0.20f);
            int damage = (int)(baseATK * atkVariance);
            
            // 3. Dano garantido (10% ignora DEF)
            int guaranteedDamage = (int)(damage * (MIN_DAMAGE_PERCENT / 100f));
            int defensibleDamage = damage - guaranteedDamage;
            
            // 4. Redução de defesa do player
            int playerDEF = player.character.defense;
            float defReduction = 1.0f - (playerDEF / (float)(playerDEF + 100));
            defReduction = Math.Max(defReduction, 0.1f);
            
            int finalDefensibleDamage = (int)(defensibleDamage * defReduction);
            damage = guaranteedDamage + finalDefensibleDamage;
            
            // 5. Crítico
            if (isCritical)
            {
                damage = (int)(damage * CRITICAL_MULTIPLIER);
            }
            
            // 6. Dano mínimo
            damage = Math.Max(damage, ABSOLUTE_MIN_DAMAGE);

            // === LOG ===
            Console.WriteLine($"👹 {monster.template.name} -> {player.character.nome}:");
            Console.WriteLine($"   Monster ATK:{baseATK} Level:{monster.template.level}");
            Console.WriteLine($"   Player DEF:{playerDEF} HP:{player.character.health}/{player.character.maxHealth}");
            Console.WriteLine($"   Hit Roll:{hitRoll:F2} vs {hitChance:F2} = HIT!");
            Console.WriteLine($"   Crit:{(isCritical ? "YES" : "NO")} Final Damage:{damage}");

            // Aplica dano ao player
            int actualDamage = player.character.TakeDamage(damage);

            // Log de combate
            try
            {
                DatabaseHandler.Instance.LogCombat(player.character.id, monster.id, actualDamage, "physical", isCritical);
            }
            catch { }

            var result = new CombatResult
            {
                attackerId = monster.id.ToString(),
                targetId = player.sessionId,
                attackerType = "monster",
                targetType = "player",
                damage = actualDamage,
                isCritical = isCritical,
                remainingHealth = player.character.health,
                targetDied = player.character.isDead
            };

            if (result.targetDied)
            {
                Console.WriteLine($"💀 {player.character.nome} was killed by {monster.template.name}!");
            }

            // Atualiza no banco
            DatabaseHandler.Instance.UpdateCharacter(player.character);

            return result;
        }

        // ==========================================
        // CÁLCULO DE EXPERIÊNCIA (MELHORADO)
        // ==========================================
        private int CalculateExperienceReward(int playerLevel, int monsterLevel, int baseExp)
        {
            int levelDiff = monsterLevel - playerLevel;
            float multiplier = 1.0f;
            
            // Penalidades por monstros muito fracos
            if (levelDiff <= -10)
            {
                multiplier = 0.05f; // 5% apenas
            }
            else if (levelDiff <= -5)
            {
                multiplier = 0.30f; // 30%
            }
            else if (levelDiff <= -3)
            {
                multiplier = 0.60f; // 60%
            }
            else if (levelDiff == -2)
            {
                multiplier = 0.80f; // 80%
            }
            else if (levelDiff == -1)
            {
                multiplier = 0.90f; // 90%
            }
            // Bônus por monstros mais fortes
            else if (levelDiff >= 10)
            {
                multiplier = 2.0f; // +100%
            }
            else if (levelDiff >= 5)
            {
                multiplier = 1.5f; // +50%
            }
            else if (levelDiff >= 3)
            {
                multiplier = 1.25f; // +25%
            }
            else if (levelDiff >= 1)
            {
                multiplier = 1.10f; // +10%
            }
            // Nível igual = 100%
            
            int finalExp = (int)(baseExp * multiplier);
            
            // XP mínimo de 1
            return Math.Max(1, finalExp);
        }

        // ==========================================
        // FUNÇÕES AUXILIARES
        // ==========================================
        
        public bool IsInAttackRange(Position pos1, Position pos2, float range)
        {
            float dx = pos1.x - pos2.x;
            float dz = pos1.z - pos2.z;
            float distance = (float)Math.Sqrt(dx * dx + dz * dz);
            
            return distance <= range;
        }

        public bool IsInAggroRange(Position pos1, Position pos2, float aggroRange)
        {
            float dx = pos1.x - pos2.x;
            float dz = pos1.z - pos2.z;
            float distance = (float)Math.Sqrt(dx * dx + dz * dz);
            
            return distance <= aggroRange;
        }

        public float GetDistance(Position pos1, Position pos2)
        {
            float dx = pos1.x - pos2.x;
            float dz = pos1.z - pos2.z;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }

        public float GetAttackRange()
        {
            return ATTACK_RANGE;
        }

        // ==========================================
        // MÉTODOS DE UTILIDADE PARA BALANCEAMENTO
        // ==========================================

        /// <summary>
        /// Calcula chance de acerto entre atacante e alvo
        /// </summary>
        public float CalculateHitChance(int attackerHit, int defenderFlee)
        {
            float hitChance = 0.80f + ((attackerHit - defenderFlee) / 100f);
            return Math.Clamp(hitChance, 0.30f, 0.95f);
        }

        /// <summary>
        /// Calcula redução de dano por defesa
        /// </summary>
        public float CalculateDefenseReduction(int defense)
        {
            float reduction = 1.0f - (defense / (float)(defense + 100));
            return Math.Max(reduction, 0.1f); // Mínimo 10% passa
        }

        /// <summary>
        /// Obtém estatísticas de combate para debug
        /// </summary>
        public string GetCombatStats(Player player)
        {
            int hit = 175 + player.character.dexterity + player.character.level;
            int flee = 100 + player.character.level + player.character.dexterity + (player.character.dexterity / 5);
            float critChance = 0.01f + (player.character.dexterity * 0.003f) + (player.character.level * 0.001f);
            critChance = Math.Clamp(critChance, 0.01f, 0.50f);
            
            return $"Combat Stats for {player.character.nome}:\n" +
                   $"  ATK: {player.character.attackPower}\n" +
                   $"  DEF: {player.character.defense}\n" +
                   $"  HIT: {hit}\n" +
                   $"  FLEE: {flee}\n" +
                   $"  CRIT: {critChance * 100:F1}%\n" +
                   $"  ASPD: {player.character.attackSpeed:F2}s";
        }

        /// <summary>
        /// Obtém estatísticas de monstro para debug
        /// </summary>
        public string GetMonsterStats(MonsterInstance monster)
        {
            int hit = 175 + monster.template.level + (monster.template.attackPower / 5);
            float critChance = 0.02f + (monster.template.level * 0.001f);
            critChance = Math.Clamp(critChance, 0.02f, 0.15f);
            
            return $"Combat Stats for {monster.template.name}:\n" +
                   $"  ATK: {monster.template.attackPower}\n" +
                   $"  DEF: {monster.template.defense}\n" +
                   $"  HIT: {hit}\n" +
                   $"  CRIT: {critChance * 100:F1}%\n" +
                   $"  ASPD: {monster.template.attackSpeed:F2}s";
        }
    }
}