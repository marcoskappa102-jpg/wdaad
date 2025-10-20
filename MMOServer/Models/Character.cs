namespace MMOServer.Models
{
    public class Character
    {
        public int id { get; set; }
        public int accountId { get; set; }
        public string nome { get; set; } = "";
        public string raca { get; set; } = "";
        public string classe { get; set; } = "";
        
        // Atributos de level e experiência
        public int level { get; set; } = 1;
        public int experience { get; set; } = 0;
        public int statusPoints { get; set; } = 0;
        
        // Vida e Mana
        public int health { get; set; } = 100;
        public int maxHealth { get; set; } = 100;
        public int mana { get; set; } = 100;
        public int maxMana { get; set; } = 100;
        
        // Atributos base (igual RO)
        public int strength { get; set; } = 10;      // STR - Força
        public int intelligence { get; set; } = 10;  // INT - Inteligência
        public int dexterity { get; set; } = 10;     // DEX - Destreza
        public int vitality { get; set; } = 10;      // VIT - Vitalidade
        
        // Atributos calculados
        public int attackPower { get; set; } = 10;
        public int magicPower { get; set; } = 10;
        public int defense { get; set; } = 5;
        public float attackSpeed { get; set; } = 1.0f;  // Delay em segundos
        
        // Posição
        public Position position { get; set; } = new Position();
        
        // Estado
        public bool isDead { get; set; } = false;
        
        // Calcula XP necessário para próximo level
        public int GetRequiredExp()
        {
            return 100 * level * level;
        }
        
        // Recalcula todos os atributos baseado nos stats (FÓRMULAS DO RAGNAROK)
        public void RecalculateStats()
        {
            // ========================================
            // FÓRMULAS BASEADAS NO RAGNAROK ONLINE
            // ========================================
            
            // HP = Base + (VIT * 5) + (Level * 10)
            maxHealth = 100 + (vitality * 5) + (level * 10);
            
            // SP/MP = Base + (INT * 3) + (Level * 5)
            maxMana = 50 + (intelligence * 3) + (level * 5);
            
            // ATK = Base + STR + (STR / 10)² + Bonus de Arma
            // Simplificado: ATK = STR + (STR/10)²
            int strBonus = strength + (int)Math.Pow(strength / 10.0, 2);
            attackPower = strBonus + (level * 2); // Bonus por level
            
            // MATK = Base + INT + (INT/5) + (INT/7)²
            int intBonus = intelligence + (intelligence / 5) + (int)Math.Pow(intelligence / 7.0, 2);
            magicPower = intBonus + (level * 2);
            
            // DEF = Base + VIT + (VIT/5) + Bonus de Armadura
            int vitBonus = vitality + (vitality / 5);
            defense = vitBonus + (level / 2); // Cresce com level
            
            // ASPD (Attack Speed) - FÓRMULA SIMPLIFICADA DO RO
            // No RO: ASPD = 200 - Delay
            // Aqui vamos calcular o DELAY em segundos
            // Base Delay = 2.0 segundos (ASPD 100)
            // Cada 10 DEX reduz 0.1s
            // Cada 10 AGI reduz 0.1s (vamos usar DEX como AGI também)
            
            float baseDelay = 2.0f;
            float dexReduction = (dexterity / 10f) * 0.1f;
            float levelReduction = (level / 20f) * 0.1f;
            
            attackSpeed = baseDelay - dexReduction - levelReduction;
            
            // Limita entre 0.5s (muito rápido) e 3.0s (muito lento)
            attackSpeed = Math.Clamp(attackSpeed, 0.5f, 3.0f);
            
            // Debug log
            Console.WriteLine($"[RecalculateStats] {nome}:");
            Console.WriteLine($"  STR:{strength} INT:{intelligence} DEX:{dexterity} VIT:{vitality}");
            Console.WriteLine($"  HP:{maxHealth} SP:{maxMana} ATK:{attackPower} DEF:{defense}");
            Console.WriteLine($"  AttackDelay:{attackSpeed:F2}s (ASPD:{(200 - attackSpeed * 50):F0})");
        }
        
        // Aplica level up
        public void LevelUp()
        {
            level++;
            experience = 0;
            
            // Ganha status points para distribuir (5 pontos por level - padrão RO)
            statusPoints += 5;
            
            // Aumenta atributos base automaticamente (bonus pequeno)
            strength += 1;
            intelligence += 1;
            dexterity += 1;
            vitality += 1;
            
            // Recalcula stats
            RecalculateStats();
            
            // Cura completa ao subir de level
            health = maxHealth;
            mana = maxMana;
            
            Console.WriteLine($"🌟 LEVEL UP! {nome} → Level {level}");
            Console.WriteLine($"  Status Points: {statusPoints}");
        }
        
        // Ganha experiência
        public bool GainExperience(int amount)
        {
            experience += amount;
            
            bool leveledUp = false;
            
            // Pode subir múltiplos levels de uma vez
            while (experience >= GetRequiredExp())
            {
                LevelUp();
                leveledUp = true;
            }
            
            return leveledUp;
        }
        
        // Adiciona ponto em um atributo
        public bool AddStatusPoint(string stat)
        {
            if (statusPoints <= 0)
                return false;
            
            switch (stat.ToLower())
            {
                case "str":
                case "strength":
                    strength++;
                    break;
                case "int":
                case "intelligence":
                    intelligence++;
                    break;
                case "dex":
                case "dexterity":
                    dexterity++;
                    break;
                case "vit":
                case "vitality":
                    vitality++;
                    break;
                default:
                    return false;
            }
            
            statusPoints--;
            
            // Recalcula stats
            RecalculateStats();
            
            // Restaura HP/MP proporcionalmente
            float hpPercent = maxHealth > 0 ? (float)health / maxHealth : 1.0f;
            float mpPercent = maxMana > 0 ? (float)mana / maxMana : 1.0f;
            
            health = Math.Min((int)(maxHealth * hpPercent), maxHealth);
            mana = Math.Min((int)(maxMana * mpPercent), maxMana);
            
            Console.WriteLine($"📈 {nome} added point to {stat.ToUpper()}: {GetStatValue(stat)}");
            
            return true;
        }
        
        private int GetStatValue(string stat)
        {
            return stat.ToLower() switch
            {
                "str" or "strength" => strength,
                "int" or "intelligence" => intelligence,
                "dex" or "dexterity" => dexterity,
                "vit" or "vitality" => vitality,
                _ => 0
            };
        }
        
        // Recebe dano
        public int TakeDamage(int damage)
        {
            int actualDamage = Math.Max(1, damage);
            health -= actualDamage;
            
            if (health <= 0)
            {
                health = 0;
                isDead = true;
            }
            
            return actualDamage;
        }
        
        // Revive
        public void Respawn(Position spawnPosition)
        {
            isDead = false;
            health = maxHealth;
            mana = maxMana;
            position = spawnPosition;
        }
    }
}