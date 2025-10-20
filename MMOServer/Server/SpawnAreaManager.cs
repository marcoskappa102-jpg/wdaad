using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using MMOServer.Models;

namespace MMOServer.Server
{
    /// <summary>
    /// Gerenciador de áreas de spawn de monstros
    /// Permite definir zonas onde monstros nascem
    /// </summary>
    public class SpawnAreaManager
    {
        private static SpawnAreaManager? instance;
        public static SpawnAreaManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new SpawnAreaManager();
                return instance;
            }
        }

        private const string CONFIG_FILE = "spawn_areas.json";
        private SpawnAreasConfig config = new SpawnAreasConfig();
        private Random random = new Random();

        public void Initialize()
        {
            Console.WriteLine("📍 SpawnAreaManager: Initializing...");
            LoadConfiguration();
        }

        private void LoadConfiguration()
        {
            string filePath = Path.Combine("Config", CONFIG_FILE);

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"⚠️ {CONFIG_FILE} not found! Creating default...");
                config = CreateDefaultConfig();
                SaveConfiguration();
                return;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                config = JsonConvert.DeserializeObject<SpawnAreasConfig>(json) ?? new SpawnAreasConfig();
                
                Console.WriteLine($"✅ Loaded {config.spawnAreas.Count} spawn areas");
                
                // Log resumo
                int totalSpawns = 0;
                foreach (var area in config.spawnAreas)
                {
                    int areaSpawns = area.spawns.Sum(s => s.count);
                    totalSpawns += areaSpawns;
                    Console.WriteLine($"   [{area.id}] {area.name}: {areaSpawns} monsters ({area.shape})");
                }
                Console.WriteLine($"   Total monsters to spawn: {totalSpawns}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading {CONFIG_FILE}: {ex.Message}");
                config = CreateDefaultConfig();
            }
        }

        private SpawnAreasConfig CreateDefaultConfig()
        {
            return new SpawnAreasConfig
            {
                spawnAreas = new List<SpawnArea>
                {
                    new SpawnArea
                    {
                        id = 1,
                        name = "Planície dos Iniciantes",
                        description = "Área para jogadores iniciantes",
                        shape = "circle",
                        centerX = 10, centerY = 1, centerZ = 10,
                        radius = 25,
                        maxSlope = 35,
                        spawns = new List<SpawnEntry>
                        {
                            new SpawnEntry { monsterId = 1, monsterName = "Lobo Selvagem", count = 3, respawnTime = 30 }
                        }
                    }
                }
            };
        }

        public void SaveConfiguration()
        {
            try
            {
                string filePath = Path.Combine("Config", CONFIG_FILE);
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(filePath, json);
                Console.WriteLine($"💾 Saved spawn areas to {CONFIG_FILE}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error saving {CONFIG_FILE}: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtém todas as áreas de spawn
        /// </summary>
        public List<SpawnArea> GetAllAreas()
        {
            return config.spawnAreas;
        }

        /// <summary>
        /// Obtém uma área específica por ID
        /// </summary>
        public SpawnArea? GetArea(int areaId)
        {
            return config.spawnAreas.FirstOrDefault(a => a.id == areaId);
        }

        /// <summary>
        /// Gera posição aleatória dentro de uma área
        /// Considera formato da área e valida inclinação do terreno
        /// </summary>
        public Position? GetRandomPositionInArea(SpawnArea area, int maxAttempts = 20)
        {
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                Position pos;

                if (area.shape == "circle")
                {
                    pos = GetRandomPositionInCircle(area);
                }
                else if (area.shape == "rectangle")
                {
                    pos = GetRandomPositionInRectangle(area);
                }
                else
                {
                    Console.WriteLine($"⚠️ Unknown shape: {area.shape}");
                    return null;
                }

                // Valida inclinação
                if (TerrainHeightmap.Instance.IsValidSpawnPosition(pos.x, pos.z, area.maxSlope))
                {
                    // Ajusta altura ao terreno
                    pos.y = TerrainHeightmap.Instance.GetHeightAt(pos.x, pos.z) + 1f;
                    return pos;
                }
            }

            // Fallback: usa centro da área
            Console.WriteLine($"⚠️ Could not find valid position in area {area.name}, using center");
            return new Position 
            { 
                x = area.centerX, 
                y = TerrainHeightmap.Instance.GetHeightAt(area.centerX, area.centerZ) + 1f, 
                z = area.centerZ 
            };
        }

        private Position GetRandomPositionInCircle(SpawnArea area)
        {
            // Distribuição uniforme em círculo
            double angle = random.NextDouble() * Math.PI * 2;
            double distance = Math.Sqrt(random.NextDouble()) * area.radius; // Sqrt para distribuição uniforme

            float x = area.centerX + (float)(Math.Cos(angle) * distance);
            float z = area.centerZ + (float)(Math.Sin(angle) * distance);

            return new Position { x = x, y = area.centerY, z = z };
        }

        private Position GetRandomPositionInRectangle(SpawnArea area)
        {
            float halfWidth = area.width / 2f;
            float halfLength = area.length / 2f;

            float x = area.centerX + (float)((random.NextDouble() * 2 - 1) * halfWidth);
            float z = area.centerZ + (float)((random.NextDouble() * 2 - 1) * halfLength);

            return new Position { x = x, y = area.centerY, z = z };
        }

        /// <summary>
        /// Verifica se uma posição está dentro de uma área
        /// </summary>
        public bool IsPositionInArea(Position pos, SpawnArea area)
        {
            if (area.shape == "circle")
            {
                float dx = pos.x - area.centerX;
                float dz = pos.z - area.centerZ;
                float distance = (float)Math.Sqrt(dx * dx + dz * dz);
                return distance <= area.radius;
            }
            else if (area.shape == "rectangle")
            {
                float halfWidth = area.width / 2f;
                float halfLength = area.length / 2f;

                float minX = area.centerX - halfWidth;
                float maxX = area.centerX + halfWidth;
                float minZ = area.centerZ - halfLength;
                float maxZ = area.centerZ + halfLength;

                return pos.x >= minX && pos.x <= maxX && pos.z >= minZ && pos.z <= maxZ;
            }

            return false;
        }

        /// <summary>
        /// Encontra a área mais próxima de uma posição
        /// </summary>
        public SpawnArea? FindNearestArea(Position pos)
        {
            SpawnArea? nearest = null;
            float minDistance = float.MaxValue;

            foreach (var area in config.spawnAreas)
            {
                float dx = pos.x - area.centerX;
                float dz = pos.z - area.centerZ;
                float distance = (float)Math.Sqrt(dx * dx + dz * dz);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = area;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Lista áreas em um raio específico
        /// </summary>
        public List<SpawnArea> GetAreasInRadius(Position center, float radius)
        {
            var result = new List<SpawnArea>();

            foreach (var area in config.spawnAreas)
            {
                float dx = center.x - area.centerX;
                float dz = center.z - area.centerZ;
                float distance = (float)Math.Sqrt(dx * dx + dz * dz);

                if (distance <= radius)
                {
                    result.Add(area);
                }
            }

            return result;
        }

        public void ReloadConfiguration()
        {
            Console.WriteLine("🔄 Reloading spawn area configuration...");
            LoadConfiguration();
        }
    }

    // ==================== CLASSES DE CONFIGURAÇÃO ====================

    [Serializable]
    public class SpawnAreasConfig
    {
        public List<SpawnArea> spawnAreas { get; set; } = new List<SpawnArea>();
    }

    [Serializable]
    public class SpawnArea
    {
        public int id { get; set; }
        public string name { get; set; } = "";
        public string description { get; set; } = "";
        
        // Formato: "circle" ou "rectangle"
        public string shape { get; set; } = "circle";
        
        // Centro da área
        public float centerX { get; set; }
        public float centerY { get; set; }
        public float centerZ { get; set; }
        
        // Para círculos
        public float radius { get; set; }
        
        // Para retângulos
        public float width { get; set; }
        public float length { get; set; }
        
        // Validação de terreno
        public float maxSlope { get; set; } = 45f;
        
        // Monstros que nascem nesta área
        public List<SpawnEntry> spawns { get; set; } = new List<SpawnEntry>();
    }

    [Serializable]
    public class SpawnEntry
    {
        public int monsterId { get; set; }
        public string monsterName { get; set; } = "";
        public int count { get; set; } = 1; // Quantos monstros spawnam
        public int respawnTime { get; set; } = 30; // Tempo de respawn em segundos
    }
}