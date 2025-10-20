using System;
using System.IO;
using Newtonsoft.Json;

namespace MMOServer.Server
{
    /// <summary>
    /// Sistema de Heightmap para terreno 3D no servidor
    /// Permite consultar altura do terreno em qualquer posição (X, Z)
    /// Coloque em: MMOServer/Server/TerrainHeightmap.cs
    /// </summary>
    public class TerrainHeightmap
    {
        private static TerrainHeightmap? instance;
        public static TerrainHeightmap Instance
        {
            get
            {
                if (instance == null)
                    instance = new TerrainHeightmap();
                return instance;
            }
        }

        private HeightmapData? data;
        private float[,]? heightGrid;
        private bool isLoaded = false;

        public void Initialize()
        {
            string filePath = Path.Combine("Config", "terrain_heightmap.json");

            if (!File.Exists(filePath))
            {
                Console.WriteLine("⚠️ Heightmap not found. Using flat terrain (Y=0)");
                Console.WriteLine($"   Expected: {Path.GetFullPath(filePath)}");
                Console.WriteLine("   Export heightmap from Unity: MMO > Export Terrain Heightmap");
                isLoaded = false;
                return;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                data = JsonConvert.DeserializeObject<HeightmapData>(json);

                if (data == null || data.heights == null)
                {
                    Console.WriteLine("❌ Invalid heightmap data!");
                    isLoaded = false;
                    return;
                }

                // Converte array 1D para 2D para acesso mais rápido
                heightGrid = new float[data.resolution, data.resolution];
                for (int y = 0; y < data.resolution; y++)
                {
                    for (int x = 0; x < data.resolution; x++)
                    {
                        heightGrid[y, x] = data.heights[y * data.resolution + x];
                    }
                }

                isLoaded = true;
                Console.WriteLine($"✅ Heightmap loaded successfully!");
                Console.WriteLine($"   Resolution: {data.resolution}x{data.resolution}");
                Console.WriteLine($"   Terrain Size: {data.terrainWidth}x{data.terrainLength}");
                Console.WriteLine($"   Height Range: 0 to {data.terrainHeight}");
                Console.WriteLine($"   Position: ({data.terrainPosX}, {data.terrainPosY}, {data.terrainPosZ})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading heightmap: {ex.Message}");
                isLoaded = false;
            }
        }

        /// <summary>
        /// Obtém a altura do terreno na posição (worldX, worldZ)
        /// Usa interpolação bilinear para suavidade
        /// </summary>
        public float GetHeightAt(float worldX, float worldZ)
        {
            if (!isLoaded || data == null || heightGrid == null)
                return 0f; // Terreno plano

            // Converte coordenadas mundo para coordenadas locais do terreno
            float localX = worldX - data.terrainPosX;
            float localZ = worldZ - data.terrainPosZ;

            // Normaliza (0 a 1)
            float normX = localX / data.terrainWidth;
            float normZ = localZ / data.terrainLength;

            // Fora dos limites? Retorna altura base do terreno
            if (normX < 0 || normX > 1 || normZ < 0 || normZ > 1)
                return data.terrainPosY;

            // Converte para índices do grid
            float gridX = normX * (data.resolution - 1);
            float gridZ = normZ * (data.resolution - 1);

            // Índices inteiros (canto inferior esquerdo do quad)
            int x0 = (int)Math.Floor(gridX);
            int z0 = (int)Math.Floor(gridZ);
            int x1 = Math.Min(x0 + 1, data.resolution - 1);
            int z1 = Math.Min(z0 + 1, data.resolution - 1);

            // Fatores de interpolação (0 a 1)
            float fx = gridX - x0;
            float fz = gridZ - z0;

            // Interpolação bilinear entre os 4 pontos
            float h00 = heightGrid[z0, x0]; // Inferior esquerdo
            float h10 = heightGrid[z0, x1]; // Inferior direito
            float h01 = heightGrid[z1, x0]; // Superior esquerdo
            float h11 = heightGrid[z1, x1]; // Superior direito

            // Interpola em X
            float h0 = Lerp(h00, h10, fx);
            float h1 = Lerp(h01, h11, fx);

            // Interpola em Z
            float height = Lerp(h0, h1, fz);

            // Converte altura normalizada (0-1) para altura real em metros
            return data.terrainPosY + (height * data.terrainHeight);
        }

        /// <summary>
        /// Ajusta uma posição para ficar no chão com offset
        /// Modifica os valores por referência
        /// </summary>
        public void ClampToGround(ref float x, ref float y, ref float z, float offset = 1f)
        {
            y = GetHeightAt(x, z) + offset;
        }

        /// <summary>
        /// Ajusta uma Position para ficar no chão
        /// </summary>
        public void ClampToGround(MMOServer.Models.Position pos, float offset = 1f)
        {
            pos.y = GetHeightAt(pos.x, pos.z) + offset;
        }

        /// <summary>
        /// Verifica se uma posição está dentro dos limites do terreno
        /// </summary>
        public bool IsInBounds(float worldX, float worldZ)
        {
            if (!isLoaded || data == null)
                return true;

            float localX = worldX - data.terrainPosX;
            float localZ = worldZ - data.terrainPosZ;

            return localX >= 0 && localX <= data.terrainWidth &&
                   localZ >= 0 && localZ <= data.terrainLength;
        }

        /// <summary>
        /// Obtém a inclinação do terreno em uma posição (em graus)
        /// Útil para validar se monstros/players podem ficar em um local
        /// </summary>
        public float GetSlopeAt(float worldX, float worldZ)
        {
            if (!isLoaded)
                return 0f;

            // Amostra alturas ao redor (1 metro de distância)
            float h = GetHeightAt(worldX, worldZ);
            float hL = GetHeightAt(worldX - 1, worldZ);
            float hR = GetHeightAt(worldX + 1, worldZ);
            float hD = GetHeightAt(worldX, worldZ - 1);
            float hU = GetHeightAt(worldX, worldZ + 1);

            // Calcula gradiente
            float dx = (hR - hL) / 2f;
            float dz = (hU - hD) / 2f;

            // Converte para ângulo
            float slope = (float)Math.Sqrt(dx * dx + dz * dz);
            return (float)(Math.Atan(slope) * 180.0 / Math.PI);
        }

        /// <summary>
        /// Obtém a normal (direção "para cima") do terreno em uma posição
        /// Útil para alinhar objetos com o terreno
        /// </summary>
        public void GetNormalAt(float worldX, float worldZ, out float nx, out float ny, out float nz)
        {
            if (!isLoaded)
            {
                nx = 0; ny = 1; nz = 0; // Plano horizontal
                return;
            }

            // Amostra alturas ao redor
            float h = GetHeightAt(worldX, worldZ);
            float hL = GetHeightAt(worldX - 1, worldZ);
            float hR = GetHeightAt(worldX + 1, worldZ);
            float hD = GetHeightAt(worldX, worldZ - 1);
            float hU = GetHeightAt(worldX, worldZ + 1);

            // Calcula vetores tangentes
            float dx = hR - hL; // Gradiente em X
            float dz = hU - hD; // Gradiente em Z

            // Normal = cross product dos tangentes, normalizado
            float length = (float)Math.Sqrt(dx * dx + 4 + dz * dz);
            nx = -dx / length;
            ny = 2f / length;
            nz = -dz / length;
        }

        /// <summary>
        /// Valida se uma posição é segura para spawn (não muito inclinada)
        /// </summary>
        public bool IsValidSpawnPosition(float worldX, float worldZ, float maxSlope = 45f)
        {
            if (!IsInBounds(worldX, worldZ))
                return false;

            float slope = GetSlopeAt(worldX, worldZ);
            return slope <= maxSlope;
        }

        private float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        public bool IsLoaded => isLoaded;

        /// <summary>
        /// Obtém informações do terreno para debug
        /// </summary>
        public string GetTerrainInfo()
        {
            if (!isLoaded || data == null)
                return "Terrain: Not loaded (flat ground)";

            return $"Terrain Info:\n" +
                   $"  Resolution: {data.resolution}x{data.resolution}\n" +
                   $"  Size: {data.terrainWidth}x{data.terrainLength}\n" +
                   $"  Height: 0 to {data.terrainHeight}\n" +
                   $"  Position: ({data.terrainPosX}, {data.terrainPosY}, {data.terrainPosZ})";
        }

        [Serializable]
        public class HeightmapData
        {
            public int resolution { get; set; }
            public float terrainWidth { get; set; }
            public float terrainLength { get; set; }
            public float terrainHeight { get; set; }
            public float terrainPosX { get; set; }
            public float terrainPosY { get; set; }
            public float terrainPosZ { get; set; }
            public float[]? heights { get; set; }
        }
    }
}