using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using MMOServer.Models;

namespace MMOServer.Server
{
    public class ConfigManager
    {
        private static ConfigManager? instance;
        public static ConfigManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new ConfigManager();
                return instance;
            }
        }

        private const string CONFIG_FOLDER = "Config";
        private const string MONSTERS_FILE = "monsters.json";
        private const string CLASSES_FILE = "classes.json";

        public MonsterConfig MonsterConfig { get; private set; } = new MonsterConfig();
        public ClassConfig ClassConfig { get; private set; } = new ClassConfig();

        public void Initialize()
        {
            Console.WriteLine("📋 ConfigManager: Initializing...");

            if (!Directory.Exists(CONFIG_FOLDER))
            {
                Directory.CreateDirectory(CONFIG_FOLDER);
                Console.WriteLine($"📁 Created config folder: {CONFIG_FOLDER}/");
            }

            LoadOrCreateMonsterConfig();
            LoadOrCreateClassConfig();
            
            // 🆕 Inicializa gerenciador de áreas de spawn
            SpawnAreaManager.Instance.Initialize();

            Console.WriteLine("✅ ConfigManager: Initialized successfully!");
        }

        // ==================== MONSTERS ====================

        private void LoadOrCreateMonsterConfig()
        {
            string filePath = Path.Combine(CONFIG_FOLDER, MONSTERS_FILE);

            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    MonsterConfig = JsonConvert.DeserializeObject<MonsterConfig>(json) ?? new MonsterConfig();
                    Console.WriteLine($"✅ Loaded {MonsterConfig.monsters.Count} monster templates from {MONSTERS_FILE}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error loading {MONSTERS_FILE}: {ex.Message}");
                    MonsterConfig = CreateDefaultMonsterConfig();
                    SaveMonsterConfig();
                }
            }
            else
            {
                Console.WriteLine($"📝 Creating default {MONSTERS_FILE}...");
                MonsterConfig = CreateDefaultMonsterConfig();
                SaveMonsterConfig();
            }
        }

        private MonsterConfig CreateDefaultMonsterConfig()
        {
            return new MonsterConfig
            {
                monsters = new List<MonsterTemplateConfig>
                {
                    new MonsterTemplateConfig
                    {
                        id = 1,
                        name = "Lobo Selvagem",
                        level = 1,
                        maxHealth = 50,
                        attackPower = 8,
                        defense = 2,
                        experienceReward = 15,
                        attackSpeed = 1.5f,
                        movementSpeed = 4.0f,
                        aggroRange = 8.0f,
                        description = "Lobo comum encontrado nas planícies"
                    },
                    new MonsterTemplateConfig
                    {
                        id = 2,
                        name = "Goblin Explorador",
                        level = 2,
                        maxHealth = 80,
                        attackPower = 12,
                        defense = 3,
                        experienceReward = 25,
                        attackSpeed = 1.8f,
                        movementSpeed = 3.5f,
                        aggroRange = 10.0f,
                        description = "Goblin fraco que vaga pelas florestas"
                    }
                }
            };
        }

        public void SaveMonsterConfig()
        {
            try
            {
                string filePath = Path.Combine(CONFIG_FOLDER, MONSTERS_FILE);
                string json = JsonConvert.SerializeObject(MonsterConfig, Formatting.Indented);
                File.WriteAllText(filePath, json);
                Console.WriteLine($"💾 Saved monster config to {MONSTERS_FILE}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error saving {MONSTERS_FILE}: {ex.Message}");
            }
        }

        // ==================== CLASSES ====================

        private void LoadOrCreateClassConfig()
        {
            string filePath = Path.Combine(CONFIG_FOLDER, CLASSES_FILE);

            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    ClassConfig = JsonConvert.DeserializeObject<ClassConfig>(json) ?? new ClassConfig();
                    Console.WriteLine($"✅ Loaded {ClassConfig.classes.Count} class configurations from {CLASSES_FILE}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error loading {CLASSES_FILE}: {ex.Message}");
                    ClassConfig = CreateDefaultClassConfig();
                    SaveClassConfig();
                }
            }
            else
            {
                Console.WriteLine($"📝 Creating default {CLASSES_FILE}...");
                ClassConfig = CreateDefaultClassConfig();
                SaveClassConfig();
            }
        }

        private ClassConfig CreateDefaultClassConfig()
        {
            return new ClassConfig
            {
                classes = new List<CharacterClassConfig>
                {
                    new CharacterClassConfig
                    {
                        className = "Guerreiro",
                        description = "Forte combatente corpo-a-corpo com alta vitalidade",
                        baseStrength = 20,
                        baseIntelligence = 5,
                        baseDexterity = 12,
                        baseVitality = 18,
                        bonusHealthPerLevel = 15,
                        bonusManaPerLevel = 3,
                        bonusAttackPowerPerLevel = 3,
                        bonusDefensePerLevel = 2,
                        recommendedStats = "STR > VIT > DEX",
                        playstyle = "Tank/DPS melee com alto HP e dano físico"
                    },
                    new CharacterClassConfig
                    {
                        className = "Mago",
                        description = "Mestre das artes arcanas com devastador poder mágico",
                        baseStrength = 5,
                        baseIntelligence = 25,
                        baseDexterity = 15,
                        baseVitality = 10,
                        bonusHealthPerLevel = 8,
                        bonusManaPerLevel = 10,
                        bonusAttackPowerPerLevel = 1,
                        bonusDefensePerLevel = 1,
                        recommendedStats = "INT > DEX > VIT",
                        playstyle = "DPS ranged com alto dano mágico mas baixo HP"
                    },
                    new CharacterClassConfig
                    {
                        className = "Arqueiro",
                        description = "Atirador preciso com alta velocidade de ataque",
                        baseStrength = 12,
                        baseIntelligence = 5,
                        baseDexterity = 25,
                        baseVitality = 13,
                        bonusHealthPerLevel = 10,
                        bonusManaPerLevel = 5,
                        bonusAttackPowerPerLevel = 2,
                        bonusDefensePerLevel = 1,
                        recommendedStats = "DEX > STR > VIT",
                        playstyle = "DPS ranged com alta velocidade e crítico"
                    },
                    new CharacterClassConfig
                    {
                        className = "Clerigo",
                        description = "Sacerdote divino com poderes de cura e suporte",
                        baseStrength = 8,
                        baseIntelligence = 20,
                        baseDexterity = 10,
                        baseVitality = 17,
                        bonusHealthPerLevel = 12,
                        bonusManaPerLevel = 8,
                        bonusAttackPowerPerLevel = 1,
                        bonusDefensePerLevel = 2,
                        recommendedStats = "INT > VIT > DEX",
                        playstyle = "Suporte/Healer com boa sobrevivência"
                    }
                }
            };
        }

        public void SaveClassConfig()
        {
            try
            {
                string filePath = Path.Combine(CONFIG_FOLDER, CLASSES_FILE);
                string json = JsonConvert.SerializeObject(ClassConfig, Formatting.Indented);
                File.WriteAllText(filePath, json);
                Console.WriteLine($"💾 Saved class config to {CLASSES_FILE}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error saving {CLASSES_FILE}: {ex.Message}");
            }
        }

        // ==================== HELPER METHODS ====================

        public MonsterTemplateConfig? GetMonsterTemplate(int id)
        {
            return MonsterConfig.monsters.Find(m => m.id == id);
        }

        public CharacterClassConfig? GetClassConfig(string className)
        {
            return ClassConfig.classes.Find(c => c.className == className);
        }

        public void ReloadConfigs()
        {
            Console.WriteLine("🔄 Reloading configurations...");
            LoadOrCreateMonsterConfig();
            LoadOrCreateClassConfig();
            SpawnAreaManager.Instance.ReloadConfiguration(); // 🆕
            Console.WriteLine("✅ Configurations reloaded!");
        }
    }

    // ==================== CONFIG CLASSES ====================

    [Serializable]
    public class MonsterConfig
    {
        public List<MonsterTemplateConfig> monsters { get; set; } = new List<MonsterTemplateConfig>();
    }

    [Serializable]
    public class MonsterTemplateConfig
    {
        public int id { get; set; }
        public string name { get; set; } = "";
        public int level { get; set; }
        public int maxHealth { get; set; }
        public int attackPower { get; set; }
        public int defense { get; set; }
        public int experienceReward { get; set; }
        public float attackSpeed { get; set; }
        public float movementSpeed { get; set; }
        public float aggroRange { get; set; }
        public string description { get; set; } = "";
        
        // 🆕 Sistema de Patrulha e Prefabs
        public string prefabPath { get; set; } = "";
        public string patrolBehavior { get; set; } = "wander";
        public float patrolRadius { get; set; } = 10.0f;
        public float patrolInterval { get; set; } = 5.0f;
        public float idleTime { get; set; } = 3.0f;
    }

    [Serializable]
    public class ClassConfig
    {
        public List<CharacterClassConfig> classes { get; set; } = new List<CharacterClassConfig>();
    }

    [Serializable]
    public class CharacterClassConfig
    {
        public string className { get; set; } = "";
        public string description { get; set; } = "";
        
        public int baseStrength { get; set; }
        public int baseIntelligence { get; set; }
        public int baseDexterity { get; set; }
        public int baseVitality { get; set; }
        
        public int bonusHealthPerLevel { get; set; }
        public int bonusManaPerLevel { get; set; }
        public int bonusAttackPowerPerLevel { get; set; }
        public int bonusDefensePerLevel { get; set; }
        
        public string recommendedStats { get; set; } = "";
        public string playstyle { get; set; } = "";
    }
}