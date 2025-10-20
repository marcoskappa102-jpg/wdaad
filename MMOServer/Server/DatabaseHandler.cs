using MySql.Data.MySqlClient;
using MMOServer.Models;
using MMOServer.Configuration;

namespace MMOServer.Server
{
    public class DatabaseHandler
    {
        private static DatabaseHandler? instance;
        public static DatabaseHandler Instance
        {
            get
            {
                if (instance == null)
                    instance = new DatabaseHandler();
                return instance;
            }
        }

        private string connectionString = "";

        public void Initialize()
        {
            // ✅ AGORA USA appsettings.json
            connectionString = ConfigLoader.Instance.Settings.DatabaseSettings.GetConnectionString();
            
            Console.WriteLine("💾 Database Handler initialized");
            Console.WriteLine($"   Connection: {ConfigLoader.Instance.Settings.DatabaseSettings.Server}/{ConfigLoader.Instance.Settings.DatabaseSettings.Database}");
            
            // Testa conexão
            try
            {
                using var conn = GetConnection();
                conn.Open();
                Console.WriteLine("✅ Database connection test successful!");
                conn.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Database connection failed: {ex.Message}");
                Console.WriteLine($"   Check your appsettings.json database configuration!");
            }
        }

        private MySqlConnection GetConnection()
        {
            return new MySqlConnection(connectionString);
        }

        // ==================== ACCOUNTS ====================
        
        public int ValidateLogin(string username, string password)
        {
            using var conn = GetConnection();
            conn.Open();

            var query = "SELECT id FROM accounts WHERE username = @username AND password = @password";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@username", username);
            cmd.Parameters.AddWithValue("@password", password);

            var result = cmd.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        public bool CreateAccount(string username, string password)
        {
            try
            {
                using var conn = GetConnection();
                conn.Open();

                var query = "INSERT INTO accounts (username, password) VALUES (@username, @password)";
                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@username", username);
                cmd.Parameters.AddWithValue("@password", password);

                cmd.ExecuteNonQuery();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ==================== CHARACTERS ====================
        
        public int CreateCharacter(Character character)
        {
            using var conn = GetConnection();
            conn.Open();

            var query = @"INSERT INTO characters (
                account_id, nome, raca, classe, level, experience, status_points,
                health, max_health, mana, max_mana,
                strength, intelligence, dexterity, vitality,
                attack_power, magic_power, defense, attack_speed,
                pos_x, pos_y, pos_z, is_dead
            ) VALUES (
                @accountId, @nome, @raca, @classe, @level, @experience, @statusPoints,
                @health, @maxHealth, @mana, @maxMana,
                @strength, @intelligence, @dexterity, @vitality,
                @attackPower, @magicPower, @defense, @attackSpeed,
                @posX, @posY, @posZ, @isDead
            )";
            
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@accountId", character.accountId);
            cmd.Parameters.AddWithValue("@nome", character.nome);
            cmd.Parameters.AddWithValue("@raca", character.raca);
            cmd.Parameters.AddWithValue("@classe", character.classe);
            cmd.Parameters.AddWithValue("@level", character.level);
            cmd.Parameters.AddWithValue("@experience", character.experience);
            cmd.Parameters.AddWithValue("@statusPoints", character.statusPoints);
            cmd.Parameters.AddWithValue("@health", character.health);
            cmd.Parameters.AddWithValue("@maxHealth", character.maxHealth);
            cmd.Parameters.AddWithValue("@mana", character.mana);
            cmd.Parameters.AddWithValue("@maxMana", character.maxMana);
            cmd.Parameters.AddWithValue("@strength", character.strength);
            cmd.Parameters.AddWithValue("@intelligence", character.intelligence);
            cmd.Parameters.AddWithValue("@dexterity", character.dexterity);
            cmd.Parameters.AddWithValue("@vitality", character.vitality);
            cmd.Parameters.AddWithValue("@attackPower", character.attackPower);
            cmd.Parameters.AddWithValue("@magicPower", character.magicPower);
            cmd.Parameters.AddWithValue("@defense", character.defense);
            cmd.Parameters.AddWithValue("@attackSpeed", character.attackSpeed);
            cmd.Parameters.AddWithValue("@posX", character.position.x);
            cmd.Parameters.AddWithValue("@posY", character.position.y);
            cmd.Parameters.AddWithValue("@posZ", character.position.z);
            cmd.Parameters.AddWithValue("@isDead", character.isDead);

            cmd.ExecuteNonQuery();
            int characterId = (int)cmd.LastInsertedId;
            
            // Cria inventário inicial
            CreateDefaultInventory(characterId);
            
            return characterId;
        }

        public List<Character> GetCharacters(int accountId)
        {
            var characters = new List<Character>();

            using var conn = GetConnection();
            conn.Open();

            var query = "SELECT * FROM characters WHERE account_id = @accountId";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@accountId", accountId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                characters.Add(ReadCharacterFromReader(reader));
            }

            return characters;
        }

        public Character? GetCharacter(int characterId)
        {
            using var conn = GetConnection();
            conn.Open();

            var query = "SELECT * FROM characters WHERE id = @id";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", characterId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return ReadCharacterFromReader(reader);
            }

            return null;
        }

        private Character ReadCharacterFromReader(MySqlDataReader reader)
        {
            return new Character
            {
                id = reader.GetInt32("id"),
                accountId = reader.GetInt32("account_id"),
                nome = reader.GetString("nome"),
                raca = reader.GetString("raca"),
                classe = reader.GetString("classe"),
                level = reader.GetInt32("level"),
                experience = reader.GetInt32("experience"),
                statusPoints = reader.GetInt32("status_points"),
                health = reader.GetInt32("health"),
                maxHealth = reader.GetInt32("max_health"),
                mana = reader.GetInt32("mana"),
                maxMana = reader.GetInt32("max_mana"),
                strength = reader.GetInt32("strength"),
                intelligence = reader.GetInt32("intelligence"),
                dexterity = reader.GetInt32("dexterity"),
                vitality = reader.GetInt32("vitality"),
                attackPower = reader.GetInt32("attack_power"),
                magicPower = reader.GetInt32("magic_power"),
                defense = reader.GetInt32("defense"),
                attackSpeed = reader.GetFloat("attack_speed"),
                position = new Position
                {
                    x = reader.GetFloat("pos_x"),
                    y = reader.GetFloat("pos_y"),
                    z = reader.GetFloat("pos_z")
                },
                isDead = reader.GetBoolean("is_dead")
            };
        }

        public void UpdateCharacter(Character character)
        {
            using var conn = GetConnection();
            conn.Open();

            var query = @"UPDATE characters SET 
                level = @level, experience = @experience, status_points = @statusPoints,
                health = @health, max_health = @maxHealth,
                mana = @mana, max_mana = @maxMana,
                strength = @strength, intelligence = @intelligence,
                dexterity = @dexterity, vitality = @vitality,
                attack_power = @attackPower, magic_power = @magicPower,
                defense = @defense, attack_speed = @attackSpeed,
                pos_x = @posX, pos_y = @posY, pos_z = @posZ,
                is_dead = @isDead
                WHERE id = @id";
            
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", character.id);
            cmd.Parameters.AddWithValue("@level", character.level);
            cmd.Parameters.AddWithValue("@experience", character.experience);
            cmd.Parameters.AddWithValue("@statusPoints", character.statusPoints);
            cmd.Parameters.AddWithValue("@health", character.health);
            cmd.Parameters.AddWithValue("@maxHealth", character.maxHealth);
            cmd.Parameters.AddWithValue("@mana", character.mana);
            cmd.Parameters.AddWithValue("@maxMana", character.maxMana);
            cmd.Parameters.AddWithValue("@strength", character.strength);
            cmd.Parameters.AddWithValue("@intelligence", character.intelligence);
            cmd.Parameters.AddWithValue("@dexterity", character.dexterity);
            cmd.Parameters.AddWithValue("@vitality", character.vitality);
            cmd.Parameters.AddWithValue("@attackPower", character.attackPower);
            cmd.Parameters.AddWithValue("@magicPower", character.magicPower);
            cmd.Parameters.AddWithValue("@defense", character.defense);
            cmd.Parameters.AddWithValue("@attackSpeed", character.attackSpeed);
            cmd.Parameters.AddWithValue("@posX", character.position.x);
            cmd.Parameters.AddWithValue("@posY", character.position.y);
            cmd.Parameters.AddWithValue("@posZ", character.position.z);
            cmd.Parameters.AddWithValue("@isDead", character.isDead);

            cmd.ExecuteNonQuery();
        }

        public void UpdateCharacterPosition(int characterId, Position position)
        {
            using var conn = GetConnection();
            conn.Open();

            var query = "UPDATE characters SET pos_x = @posX, pos_y = @posY, pos_z = @posZ WHERE id = @id";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@posX", position.x);
            cmd.Parameters.AddWithValue("@posY", position.y);
            cmd.Parameters.AddWithValue("@posZ", position.z);
            cmd.Parameters.AddWithValue("@id", characterId);

            cmd.ExecuteNonQuery();
        }

        // ==================== MONSTERS ====================
        
        public List<MonsterTemplate> GetAllMonsterTemplates()
        {
            var templates = new List<MonsterTemplate>();

            using var conn = GetConnection();
            conn.Open();

            var query = "SELECT * FROM monster_templates";
            using var cmd = new MySqlCommand(query, conn);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                templates.Add(new MonsterTemplate
                {
                    id = reader.GetInt32("id"),
                    name = reader.GetString("name"),
                    level = reader.GetInt32("level"),
                    maxHealth = reader.GetInt32("max_health"),
                    attackPower = reader.GetInt32("attack_power"),
                    defense = reader.GetInt32("defense"),
                    experienceReward = reader.GetInt32("experience_reward"),
                    attackSpeed = reader.GetFloat("attack_speed"),
                    movementSpeed = reader.GetFloat("movement_speed"),
                    aggroRange = reader.GetFloat("aggro_range"),
                    spawnX = reader.GetFloat("spawn_x"),
                    spawnY = reader.GetFloat("spawn_y"),
                    spawnZ = reader.GetFloat("spawn_z"),
                    spawnRadius = reader.GetFloat("spawn_radius"),
                    respawnTime = reader.GetInt32("respawn_time")
                });
            }

            return templates;
        }

        public MonsterTemplate? GetMonsterTemplate(int templateId)
        {
            using var conn = GetConnection();
            conn.Open();

            var query = "SELECT * FROM monster_templates WHERE id = @id";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", templateId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new MonsterTemplate
                {
                    id = reader.GetInt32("id"),
                    name = reader.GetString("name"),
                    level = reader.GetInt32("level"),
                    maxHealth = reader.GetInt32("max_health"),
                    attackPower = reader.GetInt32("attack_power"),
                    defense = reader.GetInt32("defense"),
                    experienceReward = reader.GetInt32("experience_reward"),
                    attackSpeed = reader.GetFloat("attack_speed"),
                    movementSpeed = reader.GetFloat("movement_speed"),
                    aggroRange = reader.GetFloat("aggro_range"),
                    spawnX = reader.GetFloat("spawn_x"),
                    spawnY = reader.GetFloat("spawn_y"),
                    spawnZ = reader.GetFloat("spawn_z"),
                    spawnRadius = reader.GetFloat("spawn_radius"),
                    respawnTime = reader.GetInt32("respawn_time")
                };
            }

            return null;
        }

        public List<MonsterInstance> LoadMonsterInstances()
        {
            var instances = new List<MonsterInstance>();

            using var conn = GetConnection();
            conn.Open();

            var query = @"SELECT mi.*, mt.* 
                         FROM monster_instances mi
                         JOIN monster_templates mt ON mi.template_id = mt.id";
            
            using var cmd = new MySqlCommand(query, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var template = new MonsterTemplate
                {
                    id = reader.GetInt32("template_id"),
                    name = reader.GetString("name"),
                    level = reader.GetInt32("level"),
                    maxHealth = reader.GetInt32("max_health"),
                    attackPower = reader.GetInt32("attack_power"),
                    defense = reader.GetInt32("defense"),
                    experienceReward = reader.GetInt32("experience_reward"),
                    attackSpeed = reader.GetFloat("attack_speed"),
                    movementSpeed = reader.GetFloat("movement_speed"),
                    aggroRange = reader.GetFloat("aggro_range"),
                    spawnX = reader.GetFloat("spawn_x"),
                    spawnY = reader.GetFloat("spawn_y"),
                    spawnZ = reader.GetFloat("spawn_z"),
                    spawnRadius = reader.GetFloat("spawn_radius"),
                    respawnTime = reader.GetInt32("respawn_time")
                };

                instances.Add(new MonsterInstance
                {
                    id = reader.GetInt32("id"),
                    templateId = reader.GetInt32("template_id"),
                    template = template,
                    currentHealth = reader.GetInt32("current_health"),
                    position = new Position
                    {
                        x = reader.GetFloat("pos_x"),
                        y = reader.GetFloat("pos_y"),
                        z = reader.GetFloat("pos_z")
                    },
                    isAlive = reader.GetBoolean("is_alive"),
                    lastRespawn = reader.GetDateTime("last_respawn")
                });
            }

            return instances;
        }

        public void UpdateMonsterInstance(MonsterInstance monster)
        {
            using var conn = GetConnection();
            conn.Open();

            var query = @"UPDATE monster_instances SET 
                current_health = @health,
                pos_x = @posX, pos_y = @posY, pos_z = @posZ,
                is_alive = @isAlive,
                last_respawn = @lastRespawn
                WHERE id = @id";
            
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", monster.id);
            cmd.Parameters.AddWithValue("@health", monster.currentHealth);
            cmd.Parameters.AddWithValue("@posX", monster.position.x);
            cmd.Parameters.AddWithValue("@posY", monster.position.y);
            cmd.Parameters.AddWithValue("@posZ", monster.position.z);
            cmd.Parameters.AddWithValue("@isAlive", monster.isAlive);
            cmd.Parameters.AddWithValue("@lastRespawn", monster.lastRespawn);

            cmd.ExecuteNonQuery();
        }


// ==================== SKILLS ====================

public List<SkillInstance> LoadCharacterSkills(int characterId)
{
    var skills = new List<SkillInstance>();

    try
    {
        using var conn = GetConnection();
        conn.Open();

        var query = @"SELECT skill_id, skill_level, skill_exp, last_cast 
                     FROM character_skills 
                     WHERE character_id = @characterId";
        
        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@characterId", characterId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            skills.Add(new SkillInstance
            {
                skillId = reader.GetInt32("skill_id"),
                characterId = characterId,
                level = reader.GetInt32("skill_level"),
                exp = reader.GetInt32("skill_exp"),
                isLearned = true,
                lastCastTime = reader.IsDBNull(reader.GetOrdinal("last_cast")) 
                    ? DateTime.MinValue 
                    : reader.GetDateTime("last_cast")
            });
        }

        Console.WriteLine($"✅ Loaded {skills.Count} skills for character {characterId}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error loading character skills: {ex.Message}");
    }

    return skills;
}

public void SaveCharacterSkill(SkillInstance skill)
{
    try
    {
        using var conn = GetConnection();
        conn.Open();

        var query = @"INSERT INTO character_skills 
                     (character_id, skill_id, skill_level, skill_exp, last_cast) 
                     VALUES (@characterId, @skillId, @skillLevel, @skillExp, @lastCast)
                     ON DUPLICATE KEY UPDATE 
                     skill_level = @skillLevel, 
                     skill_exp = @skillExp, 
                     last_cast = @lastCast";
        
        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@characterId", skill.characterId);
        cmd.Parameters.AddWithValue("@skillId", skill.skillId);
        cmd.Parameters.AddWithValue("@skillLevel", skill.level);
        cmd.Parameters.AddWithValue("@skillExp", skill.exp);
        cmd.Parameters.AddWithValue("@lastCast", skill.lastCastTime == DateTime.MinValue 
            ? DBNull.Value 
            : (object)skill.lastCastTime);

        cmd.ExecuteNonQuery();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error saving character skill: {ex.Message}");
    }
}

public void LogSkillCast(int characterId, int skillId, int? targetId, string targetType, 
    bool success, int damage, int heal, bool wasCritical, bool wasMiss)
{
    try
    {
        using var conn = GetConnection();
        conn.Open();

        var query = @"INSERT INTO skill_log 
                     (character_id, skill_id, target_id, target_type, success, 
                      damage_dealt, heal_done, was_critical, was_miss) 
                     VALUES (@charId, @skillId, @targetId, @targetType, @success, 
                             @damage, @heal, @critical, @miss)";
        
        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@charId", characterId);
        cmd.Parameters.AddWithValue("@skillId", skillId);
        cmd.Parameters.AddWithValue("@targetId", targetId.HasValue ? targetId.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@targetType", targetType);
        cmd.Parameters.AddWithValue("@success", success);
        cmd.Parameters.AddWithValue("@damage", damage);
        cmd.Parameters.AddWithValue("@heal", heal);
        cmd.Parameters.AddWithValue("@critical", wasCritical);
        cmd.Parameters.AddWithValue("@miss", wasMiss);

        cmd.ExecuteNonQuery();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error logging skill cast: {ex.Message}");
    }
}

// ==================== BUFFS ====================

public void SaveActiveBuff(int characterId, ActiveBuff buff)
{
    try
    {
        using var conn = GetConnection();
        conn.Open();

        var query = @"INSERT INTO active_buffs 
                     (buff_id, character_id, skill_id, caster_id, buff_type, 
                      effect_type, affected_stat, stat_boost, duration_remaining, is_active) 
                     VALUES (@buffId, @characterId, @skillId, @casterId, @buffType, 
                             @effectType, @affectedStat, @statBoost, @durationRemaining, @isActive)";
        
        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@buffId", buff.buffId);
        cmd.Parameters.AddWithValue("@characterId", characterId);
        cmd.Parameters.AddWithValue("@skillId", buff.skillId);
        cmd.Parameters.AddWithValue("@casterId", buff.casterId);
        cmd.Parameters.AddWithValue("@buffType", buff.buffType);
        cmd.Parameters.AddWithValue("@effectType", buff.effectType);
        cmd.Parameters.AddWithValue("@affectedStat", buff.affectedStat);
        cmd.Parameters.AddWithValue("@statBoost", buff.statBoost);
        cmd.Parameters.AddWithValue("@durationRemaining", buff.remainingDuration);
        cmd.Parameters.AddWithValue("@isActive", buff.isActive);

        cmd.ExecuteNonQuery();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error saving active buff: {ex.Message}");
    }
}

public List<ActiveBuff> LoadActiveBuffs(int characterId)
{
    var buffs = new List<ActiveBuff>();

    try
    {
        using var conn = GetConnection();
        conn.Open();

        var query = @"SELECT buff_id, skill_id, caster_id, buff_type, effect_type, 
                            affected_stat, stat_boost, duration_remaining 
                     FROM active_buffs 
                     WHERE character_id = @characterId AND is_active = TRUE";
        
        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@characterId", characterId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            buffs.Add(new ActiveBuff
            {
                buffId = reader.GetInt32("buff_id"),
                skillId = reader.GetInt32("skill_id"),
                casterId = reader.GetInt32("caster_id"),
                buffType = reader.GetString("buff_type"),
                effectType = reader.GetString("effect_type"),
                affectedStat = reader.GetString("affected_stat"),
                statBoost = reader.GetInt32("stat_boost"),
                remainingDuration = reader.GetFloat("duration_remaining"),
                isActive = true
            });
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error loading active buffs: {ex.Message}");
    }

    return buffs;
}

public void UpdateBuffExpiration(int buffId, float remainingDuration)
{
    try
    {
        using var conn = GetConnection();
        conn.Open();

        if (remainingDuration <= 0)
        {
            var query = "UPDATE active_buffs SET is_active = FALSE WHERE buff_id = @buffId";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@buffId", buffId);
            cmd.ExecuteNonQuery();
        }
        else
        {
            var query = "UPDATE active_buffs SET duration_remaining = @duration WHERE buff_id = @buffId";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@buffId", buffId);
            cmd.Parameters.AddWithValue("@duration", remainingDuration);
            cmd.ExecuteNonQuery();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error updating buff expiration: {ex.Message}");
    }
}

// ==================== ESTATÍSTICAS ====================

public Dictionary<string, object> GetSkillStats(int characterId)
{
    var stats = new Dictionary<string, object>();

    try
    {
        using var conn = GetConnection();
        conn.Open();

        // Total de skills aprendidos
        var skillCountQuery = "SELECT COUNT(*) FROM character_skills WHERE character_id = @characterId";
        using var cmd = new MySqlCommand(skillCountQuery, conn);
        cmd.Parameters.AddWithValue("@characterId", characterId);
        var skillCount = (int)cmd.ExecuteScalar();
        stats["skillsLearned"] = skillCount;

        // Total de uses
        var useCountQuery = "SELECT COUNT(*) FROM skill_log WHERE character_id = @characterId";
        using var cmd2 = new MySqlCommand(useCountQuery, conn);
        cmd2.Parameters.AddWithValue("@characterId", characterId);
        var useCount = (int)cmd2.ExecuteScalar();
        stats["totalUses"] = useCount;

        // Total de dano causado
        var damageQuery = "SELECT COALESCE(SUM(damage_dealt), 0) FROM skill_log WHERE character_id = @characterId";
        using var cmd3 = new MySqlCommand(damageQuery, conn);
        cmd3.Parameters.AddWithValue("@characterId", characterId);
        var totalDamage = (long)cmd3.ExecuteScalar();
        stats["totalDamage"] = totalDamage;

        // Total de cura feita
        var healQuery = "SELECT COALESCE(SUM(heal_done), 0) FROM skill_log WHERE character_id = @characterId";
        using var cmd4 = new MySqlCommand(healQuery, conn);
        cmd4.Parameters.AddWithValue("@characterId", characterId);
        var totalHeal = (long)cmd4.ExecuteScalar();
        stats["totalHealing"] = totalHeal;

        // Taxa de crítico
        var critQuery = @"SELECT COALESCE(COUNT(*) * 100.0 / NULLIF((SELECT COUNT(*) FROM skill_log 
                         WHERE character_id = @characterId), 0), 0) 
                         FROM skill_log WHERE character_id = @characterId AND was_critical = TRUE";
        using var cmd5 = new MySqlCommand(critQuery, conn);
        cmd5.Parameters.AddWithValue("@characterId", characterId);
        var critRate = (double?)cmd5.ExecuteScalar() ?? 0;
        stats["criticalRate"] = Math.Round(critRate, 2);

        // Buffs ativos
        var buffQuery = "SELECT COUNT(*) FROM active_buffs WHERE character_id = @characterId AND is_active = TRUE";
        using var cmd6 = new MySqlCommand(buffQuery, conn);
        cmd6.Parameters.AddWithValue("@characterId", characterId);
        var activeBuffs = (int)cmd6.ExecuteScalar();
        stats["activeBuffs"] = activeBuffs;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error getting skill stats: {ex.Message}");
    }

    return stats;
}

        // ==================== COMBAT LOG ====================
        
        public void LogCombat(int? characterId, int? monsterId, int damage, string damageType, bool isCritical)
        {
            try
            {
                using var conn = GetConnection();
                conn.Open();

                var query = @"INSERT INTO combat_log 
                    (character_id, monster_id, damage_dealt, damage_type, is_critical) 
                    VALUES (@charId, @monsterId, @damage, @damageType, @isCritical)";
                
                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@charId", characterId.HasValue ? characterId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@monsterId", monsterId.HasValue ? monsterId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@damage", damage);
                cmd.Parameters.AddWithValue("@damageType", damageType);
                cmd.Parameters.AddWithValue("@isCritical", isCritical);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error logging combat: {ex.Message}");
            }
        }

        // ==================== INVENTÁRIO ====================

        private void CreateDefaultInventory(int characterId)
        {
            using var conn = GetConnection();
            conn.Open();

            var query = @"INSERT INTO inventories (character_id, max_slots, gold) 
                         VALUES (@characterId, 50, 100)";
            
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@characterId", characterId);
            cmd.ExecuteNonQuery();

            // Adiciona 5 poções de vida pequena iniciais
            var nextId = GetNextItemInstanceId();
            var itemQuery = @"INSERT INTO item_instances 
                (instance_id, character_id, template_id, quantity, slot, is_equipped) 
                VALUES (@instanceId, @characterId, 1, 5, 0, FALSE)";
            
            using var itemCmd = new MySqlCommand(itemQuery, conn);
            itemCmd.Parameters.AddWithValue("@instanceId", nextId);
            itemCmd.Parameters.AddWithValue("@characterId", characterId);
            itemCmd.ExecuteNonQuery();

            SaveNextItemInstanceId(nextId + 1);
        }

        public Inventory LoadInventory(int characterId)
        {
            var inventory = new Inventory { characterId = characterId };

            using var conn = GetConnection();
            conn.Open();

            // Carrega dados do inventário
            var query = @"SELECT * FROM inventories WHERE character_id = @characterId";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@characterId", characterId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                inventory.maxSlots = reader.GetInt32("max_slots");
                inventory.gold = reader.GetInt32("gold");
                inventory.weaponId = reader.IsDBNull(reader.GetOrdinal("weapon_id")) ? null : reader.GetInt32("weapon_id");
                inventory.armorId = reader.IsDBNull(reader.GetOrdinal("armor_id")) ? null : reader.GetInt32("armor_id");
                inventory.helmetId = reader.IsDBNull(reader.GetOrdinal("helmet_id")) ? null : reader.GetInt32("helmet_id");
                inventory.bootsId = reader.IsDBNull(reader.GetOrdinal("boots_id")) ? null : reader.GetInt32("boots_id");
                inventory.glovesId = reader.IsDBNull(reader.GetOrdinal("gloves_id")) ? null : reader.GetInt32("gloves_id");
                inventory.ringId = reader.IsDBNull(reader.GetOrdinal("ring_id")) ? null : reader.GetInt32("ring_id");
                inventory.necklaceId = reader.IsDBNull(reader.GetOrdinal("necklace_id")) ? null : reader.GetInt32("necklace_id");
            }
            reader.Close();

            // Carrega itens
            var itemQuery = @"SELECT * FROM item_instances WHERE character_id = @characterId";
            using var itemCmd = new MySqlCommand(itemQuery, conn);
            itemCmd.Parameters.AddWithValue("@characterId", characterId);

            using var itemReader = itemCmd.ExecuteReader();
            while (itemReader.Read())
            {
                inventory.items.Add(new ItemInstance
                {
                    instanceId = itemReader.GetInt32("instance_id"),
                    templateId = itemReader.GetInt32("template_id"),
                    quantity = itemReader.GetInt32("quantity"),
                    slot = itemReader.GetInt32("slot"),
                    isEquipped = itemReader.GetBoolean("is_equipped")
                });
            }

            return inventory;
        }

        public void SaveInventory(Inventory inventory)
        {
            using var conn = GetConnection();
            conn.Open();

            // Atualiza inventário
            var query = @"UPDATE inventories SET 
                max_slots = @maxSlots, 
                gold = @gold,
                weapon_id = @weaponId,
                armor_id = @armorId,
                helmet_id = @helmetId,
                boots_id = @bootsId,
                gloves_id = @glovesId,
                ring_id = @ringId,
                necklace_id = @necklaceId
                WHERE character_id = @characterId";
            
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@characterId", inventory.characterId);
            cmd.Parameters.AddWithValue("@maxSlots", inventory.maxSlots);
            cmd.Parameters.AddWithValue("@gold", inventory.gold);
            cmd.Parameters.AddWithValue("@weaponId", inventory.weaponId.HasValue ? inventory.weaponId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@armorId", inventory.armorId.HasValue ? inventory.armorId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@helmetId", inventory.helmetId.HasValue ? inventory.helmetId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@bootsId", inventory.bootsId.HasValue ? inventory.bootsId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@glovesId", inventory.glovesId.HasValue ? inventory.glovesId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@ringId", inventory.ringId.HasValue ? inventory.ringId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@necklaceId", inventory.necklaceId.HasValue ? inventory.necklaceId.Value : DBNull.Value);
            cmd.ExecuteNonQuery();

            // Remove itens antigos
            var deleteQuery = @"DELETE FROM item_instances WHERE character_id = @characterId";
            using var deleteCmd = new MySqlCommand(deleteQuery, conn);
            deleteCmd.Parameters.AddWithValue("@characterId", inventory.characterId);
            deleteCmd.ExecuteNonQuery();

            // Insere itens atualizados
            foreach (var item in inventory.items)
            {
                var itemQuery = @"INSERT INTO item_instances 
                    (instance_id, character_id, template_id, quantity, slot, is_equipped) 
                    VALUES (@instanceId, @characterId, @templateId, @quantity, @slot, @isEquipped)";
                
                using var itemCmd = new MySqlCommand(itemQuery, conn);
                itemCmd.Parameters.AddWithValue("@instanceId", item.instanceId);
                itemCmd.Parameters.AddWithValue("@characterId", inventory.characterId);
                itemCmd.Parameters.AddWithValue("@templateId", item.templateId);
                itemCmd.Parameters.AddWithValue("@quantity", item.quantity);
                itemCmd.Parameters.AddWithValue("@slot", item.slot);
                itemCmd.Parameters.AddWithValue("@isEquipped", item.isEquipped);
                itemCmd.ExecuteNonQuery();
            }
        }

        public int GetNextItemInstanceId()
        {
            using var conn = GetConnection();
            conn.Open();

            var query = "SELECT next_instance_id FROM item_id_counter WHERE id = 1";
            using var cmd = new MySqlCommand(query, conn);
            
            var result = cmd.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : 1;
        }

        public void SaveNextItemInstanceId(int nextId)
        {
            using var conn = GetConnection();
            conn.Open();

            var query = "UPDATE item_id_counter SET next_instance_id = @nextId WHERE id = 1";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@nextId", nextId);
            cmd.ExecuteNonQuery();
        }
    }
}