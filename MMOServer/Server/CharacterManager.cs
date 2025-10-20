using MMOServer.Models;

namespace MMOServer.Server
{
    /// <summary>
    /// ✅ CORRIGIDO: Agora usa classes.json para criar personagens
    /// </summary>
    public class CharacterManager
    {
        private static CharacterManager? instance;
        public static CharacterManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new CharacterManager();
                return instance;
            }
        }

        // Spawn positions por raça (Y será calculado dinamicamente pelo terreno)
        private Dictionary<string, Position> raceSpawnPoints = new Dictionary<string, Position>
        {
            { "Humano", new Position { x = 0, y = 0, z = 0 } },
            { "Elfo", new Position { x = 50, y = 0, z = 50 } },
            { "Anao", new Position { x = -50, y = 0, z = -50 } },
            { "Orc", new Position { x = -50, y = 0, z = 50 } }
        };

        /// <summary>
        /// ✅ AGORA USA CLASSES.JSON
        /// Cria personagem baseado na configuração de classe
        /// </summary>
        public Character? CreateCharacter(int accountId, string nome, string raca, string classe)
        {
            if (string.IsNullOrEmpty(nome) || string.IsNullOrEmpty(raca) || string.IsNullOrEmpty(classe))
            {
                Console.WriteLine("❌ Invalid character data (empty fields)");
                return null;
            }

            // ✅ BUSCA CONFIGURAÇÃO DA CLASSE
            var classConfig = ConfigManager.Instance.GetClassConfig(classe);
            
            if (classConfig == null)
            {
                Console.WriteLine($"❌ Class '{classe}' not found in classes.json!");
                Console.WriteLine($"   Available classes: {string.Join(", ", GetAvailableClasses())}");
                return null;
            }

            // Obtém spawn position e ajusta ao terreno
            var spawnPos = GetSpawnPosition(raca);

            // ✅ CRIA PERSONAGEM COM STATS DA CLASSE
            var character = new Character
            {
                accountId = accountId,
                nome = nome,
                raca = raca,
                classe = classe,
                position = spawnPos,
                level = 1,
                experience = 0,
                statusPoints = 0,
                
                // ✅ ATRIBUTOS BASE VINDOS DE CLASSES.JSON
                strength = classConfig.baseStrength,
                intelligence = classConfig.baseIntelligence,
                dexterity = classConfig.baseDexterity,
                vitality = classConfig.baseVitality,
                
                isDead = false
            };

            // Calcula stats derivados (HP, MP, ATK, DEF, ASPD)
            character.RecalculateStats();
            
            // Seta HP/MP cheios
            character.health = character.maxHealth;
            character.mana = character.maxMana;

            // Salva no banco de dados
            var characterId = DatabaseHandler.Instance.CreateCharacter(character);
            
            if (characterId > 0)
            {
                character.id = characterId;
                
                Console.WriteLine($"✅ Character created: {nome} (ID: {characterId})");
                Console.WriteLine($"   Race: {raca} | Class: {classe}");
                Console.WriteLine($"   Spawn: ({spawnPos.x:F1}, {spawnPos.y:F1}, {spawnPos.z:F1})");
                Console.WriteLine($"   Base Stats: STR={character.strength} INT={character.intelligence} DEX={character.dexterity} VIT={character.vitality}");
                Console.WriteLine($"   Calculated: HP={character.maxHealth} MP={character.maxMana} ATK={character.attackPower} DEF={character.defense}");
                
                return character;
            }

            Console.WriteLine("❌ Failed to save character to database");
            return null;
        }

        /// <summary>
        /// Obtém personagem do banco de dados
        /// </summary>
        public Character? GetCharacter(int characterId)
        {
            return DatabaseHandler.Instance.GetCharacter(characterId);
        }

        /// <summary>
        /// ✅ Calcula spawn position baseado na raça
        /// Ajusta Y ao terreno automaticamente
        /// </summary>
        public Position GetSpawnPosition(string raca)
        {
            Position basePos;
            
            if (raceSpawnPoints.ContainsKey(raca))
            {
                basePos = raceSpawnPoints[raca];
            }
            else
            {
                Console.WriteLine($"⚠️ Unknown race '{raca}', using default spawn");
                basePos = new Position { x = 0, y = 0, z = 0 };
            }

            // Cria nova Position para não modificar o dicionário
            var spawnPos = new Position 
            { 
                x = basePos.x, 
                y = basePos.y, 
                z = basePos.z 
            };

            // Ajusta Y ao terreno (com offset para player ficar em pé)
            TerrainHeightmap.Instance.ClampToGround(spawnPos, 0f);

            return spawnPos;
        }

        /// <summary>
        /// ✅ NOVO: Retorna lista de classes disponíveis
        /// </summary>
        public List<string> GetAvailableClasses()
        {
            return ConfigManager.Instance.ClassConfig.classes
                .Select(c => c.className)
                .ToList();
        }

        /// <summary>
        /// ✅ NOVO: Retorna informações de uma classe
        /// </summary>
        public CharacterClassConfig? GetClassInfo(string className)
        {
            return ConfigManager.Instance.GetClassConfig(className);
        }

        /// <summary>
        /// ✅ NOVO: Valida se raça existe
        /// </summary>
        public bool IsValidRace(string raca)
        {
            return raceSpawnPoints.ContainsKey(raca);
        }

        /// <summary>
        /// ✅ NOVO: Retorna lista de raças disponíveis
        /// </summary>
        public List<string> GetAvailableRaces()
        {
            return raceSpawnPoints.Keys.ToList();
        }

        /// <summary>
        /// ✅ NOVO: Valida criação de personagem
        /// </summary>
        public (bool valid, string message) ValidateCharacterCreation(
            string nome, 
            string raca, 
            string classe)
        {
            if (string.IsNullOrWhiteSpace(nome))
                return (false, "Nome não pode estar vazio");

            if (nome.Length < 3)
                return (false, "Nome deve ter pelo menos 3 caracteres");

            if (nome.Length > 20)
                return (false, "Nome muito longo (máximo 20 caracteres)");

            if (!IsValidRace(raca))
                return (false, $"Raça '{raca}' inválida. Raças disponíveis: {string.Join(", ", GetAvailableRaces())}");

            var classConfig = GetClassInfo(classe);
            if (classConfig == null)
                return (false, $"Classe '{classe}' inválida. Classes disponíveis: {string.Join(", ", GetAvailableClasses())}");

            return (true, "OK");
        }

        /// <summary>
        /// ✅ NOVO: Mostra informações da classe no console
        /// </summary>
        public void PrintClassInfo(string className)
        {
            var classConfig = GetClassInfo(className);
            
            if (classConfig == null)
            {
                Console.WriteLine($"❌ Class '{className}' not found");
                return;
            }

            Console.WriteLine($"\n📚 Class: {classConfig.className}");
            Console.WriteLine($"   Description: {classConfig.description}");
            Console.WriteLine($"   Base Stats:");
            Console.WriteLine($"      STR: {classConfig.baseStrength}");
            Console.WriteLine($"      INT: {classConfig.baseIntelligence}");
            Console.WriteLine($"      DEX: {classConfig.baseDexterity}");
            Console.WriteLine($"      VIT: {classConfig.baseVitality}");
            Console.WriteLine($"   Growth per Level:");
            Console.WriteLine($"      +{classConfig.bonusHealthPerLevel} HP");
            Console.WriteLine($"      +{classConfig.bonusManaPerLevel} MP");
            Console.WriteLine($"      +{classConfig.bonusAttackPowerPerLevel} ATK");
            Console.WriteLine($"      +{classConfig.bonusDefensePerLevel} DEF");
            Console.WriteLine($"   Recommended Stats: {classConfig.recommendedStats}");
            Console.WriteLine($"   Playstyle: {classConfig.playstyle}\n");
        }
    }
}