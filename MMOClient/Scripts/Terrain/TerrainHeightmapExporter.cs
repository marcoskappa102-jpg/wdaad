using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Exporta o heightmap do terreno Unity para o servidor
/// Coloque em: mmoclient/Assets/Editor/TerrainHeightmapExporter.cs
/// 
/// USO:
/// 1. Menu Unity: MMO > Export Terrain Heightmap
/// 2. Arquivo será salvo em MMOServer/Config/terrain_heightmap.json
/// 3. Servidor carrega automaticamente ao iniciar
/// </summary>
public class TerrainHeightmapExporter : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("MMO/Export Terrain Heightmap")]
    public static void ExportHeightmap()
    {
        Terrain terrain = FindObjectOfType<Terrain>();
        
        if (terrain == null)
        {
            EditorUtility.DisplayDialog("Error", "No Terrain found in scene!", "OK");
            Debug.LogError("❌ No Terrain found in scene!");
            return;
        }

        TerrainData terrainData = terrain.terrainData;
        
        // Configurações do heightmap
        // Quanto maior a resolução, mais preciso, mas arquivo maior
        int targetResolution = 256; // 256x256 é um bom balanço
        
        Debug.Log($"📊 Exporting heightmap...");
        Debug.Log($"   Original resolution: {terrainData.heightmapResolution}x{terrainData.heightmapResolution}");
        Debug.Log($"   Target resolution: {targetResolution}x{targetResolution}");

        // Obtém heights do terreno (0 a 1)
        float[,] heights = terrainData.GetHeights(0, 0, 
            terrainData.heightmapResolution, 
            terrainData.heightmapResolution);

        // Reduz resolução para otimizar
        float[,] reducedHeights = ReduceResolution(heights, targetResolution);

        // Informações do terreno
        Vector3 terrainSize = terrainData.size;
        Vector3 terrainPos = terrain.transform.position;

        // Cria estrutura de dados
        var heightmapData = new HeightmapData
        {
            resolution = targetResolution,
            terrainWidth = terrainSize.x,
            terrainLength = terrainSize.z,
            terrainHeight = terrainSize.y,
            terrainPosX = terrainPos.x,
            terrainPosY = terrainPos.y,
            terrainPosZ = terrainPos.z,
            heights = Flatten2DArray(reducedHeights)
        };

        // Serializa para JSON
        string json = JsonUtility.ToJson(heightmapData, true);
        
        // Salva em dois lugares:
        // 1. Assets/Resources (backup)
        // 2. MMOServer/Config (onde o servidor lê)
        
        string resourcesDir = Path.Combine(Application.dataPath, "Resources");
        if (!Directory.Exists(resourcesDir))
            Directory.CreateDirectory(resourcesDir);
        
        string unityPath = Path.Combine(resourcesDir, "terrain_heightmap.json");
        
        // Tenta encontrar pasta do servidor
        string serverPath = FindServerConfigPath();

        // Salva no Unity
        File.WriteAllText(unityPath, json);
        Debug.Log($"✅ Saved to Unity: {unityPath}");
        
        // Salva no servidor (se encontrar)
        if (!string.IsNullOrEmpty(serverPath))
        {
            try
            {
                string serverDir = Path.GetDirectoryName(serverPath);
                if (!Directory.Exists(serverDir))
                    Directory.CreateDirectory(serverDir);
                
                File.WriteAllText(serverPath, json);
                Debug.Log($"✅ Saved to Server: {serverPath}");
                
                EditorUtility.DisplayDialog("Success!", 
                    $"Heightmap exported successfully!\n\n" +
                    $"Resolution: {targetResolution}x{targetResolution}\n" +
                    $"Terrain Size: {terrainSize.x}x{terrainSize.z}\n" +
                    $"Height Range: 0 to {terrainSize.y}\n\n" +
                    $"Files saved:\n" +
                    $"• {unityPath}\n" +
                    $"• {serverPath}", 
                    "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"⚠️ Could not copy to server: {e.Message}");
                EditorUtility.DisplayDialog("Partial Success", 
                    $"Heightmap saved to Unity but could not copy to server.\n\n" +
                    $"Unity: {unityPath}\n\n" +
                    $"Manually copy to: MMOServer/Config/terrain_heightmap.json", 
                    "OK");
            }
        }
        else
        {
            EditorUtility.DisplayDialog("Partial Success", 
                $"Heightmap saved to Unity but server folder not found.\n\n" +
                $"Saved to: {unityPath}\n\n" +
                $"Manually copy to: MMOServer/Config/terrain_heightmap.json", 
                "OK");
        }

        AssetDatabase.Refresh();
        
        Debug.Log($"📈 Export complete!");
        Debug.Log($"   Resolution: {targetResolution}x{targetResolution}");
        Debug.Log($"   Terrain Size: {terrainSize.x} x {terrainSize.z}");
        Debug.Log($"   Max Height: {terrainSize.y}");
        Debug.Log($"   File Size: {json.Length / 1024f:F2} KB");
    }

    private static string FindServerConfigPath()
    {
        // Tenta encontrar a pasta do servidor
        string projectRoot = Directory.GetParent(Application.dataPath).Parent.FullName;
        
        // Possíveis caminhos
        string[] possiblePaths = new string[]
        {
            Path.Combine(projectRoot, "MMOServer", "Config", "terrain_heightmap.json"),
            Path.Combine(projectRoot, "Server", "Config", "terrain_heightmap.json"),
            Path.Combine(Application.dataPath, "..", "..", "MMOServer", "Config", "terrain_heightmap.json")
        };

        foreach (var path in possiblePaths)
        {
            string dir = Path.GetDirectoryName(path);
            if (Directory.Exists(dir))
            {
                return path;
            }
        }

        return "";
    }

    private static float[,] ReduceResolution(float[,] original, int targetResolution)
    {
        int originalRes = original.GetLength(0);
        
        if (originalRes <= targetResolution)
        {
            Debug.Log($"   No reduction needed (original is {originalRes}x{originalRes})");
            return original;
        }

        Debug.Log($"   Reducing from {originalRes}x{originalRes} to {targetResolution}x{targetResolution}...");

        float[,] reduced = new float[targetResolution, targetResolution];
        float scale = (float)originalRes / targetResolution;

        for (int y = 0; y < targetResolution; y++)
        {
            for (int x = 0; x < targetResolution; x++)
            {
                int origX = Mathf.FloorToInt(x * scale);
                int origY = Mathf.FloorToInt(y * scale);
                
                // Garante que não ultrapassa limites
                origX = Mathf.Min(origX, originalRes - 1);
                origY = Mathf.Min(origY, originalRes - 1);
                
                reduced[y, x] = original[origY, origX];
            }
        }

        return reduced;
    }

    private static float[] Flatten2DArray(float[,] array)
    {
        int width = array.GetLength(0);
        int height = array.GetLength(1);
        float[] flat = new float[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                flat[y * width + x] = array[y, x];
            }
        }

        return flat;
    }

    [System.Serializable]
    public class HeightmapData
    {
        public int resolution;
        public float terrainWidth;
        public float terrainLength;
        public float terrainHeight;
        public float terrainPosX;
        public float terrainPosY;
        public float terrainPosZ;
        public float[] heights;
    }

    // Menu adicional: Validar heightmap existente
    [MenuItem("MMO/Validate Heightmap")]
    public static void ValidateHeightmap()
    {
        string path = Path.Combine(Application.dataPath, "Resources", "terrain_heightmap.json");
        
        if (!File.Exists(path))
        {
            EditorUtility.DisplayDialog("Not Found", 
                "No heightmap found!\n\nExport it first using: MMO > Export Terrain Heightmap", 
                "OK");
            return;
        }

        string json = File.ReadAllText(path);
        var data = JsonUtility.FromJson<HeightmapData>(json);

        string info = $"Heightmap Information:\n\n" +
                     $"Resolution: {data.resolution}x{data.resolution}\n" +
                     $"Terrain Size: {data.terrainWidth}x{data.terrainLength}\n" +
                     $"Height Range: {data.terrainHeight}\n" +
                     $"Position: ({data.terrainPosX}, {data.terrainPosY}, {data.terrainPosZ})\n" +
                     $"Data Points: {data.heights.Length}\n" +
                     $"File Size: {json.Length / 1024f:F2} KB";

        EditorUtility.DisplayDialog("Heightmap Valid", info, "OK");
        Debug.Log($"✅ Heightmap validated:\n{info}");
    }
#endif
}