using UnityEngine;

/// <summary>
/// Helper para consultar altura do terreno no cliente
/// Sincronizado com o servidor via heightmap
/// Coloque em: mmoclient/Scripts/Utils/TerrainHelper.cs
/// </summary>
public class TerrainHelper : MonoBehaviour
{
    public static TerrainHelper Instance { get; private set; }

    [Header("Settings")]
    public float characterHeightOffset = 1f; // Altura do personagem acima do chão
    public bool debugMode = false;

    private Terrain terrain;
    private TerrainData terrainData;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        FindTerrain();
    }

    private void FindTerrain()
    {
        terrain = FindObjectOfType<Terrain>();
        
        if (terrain != null)
        {
            terrainData = terrain.terrainData;
            Debug.Log($"✅ TerrainHelper: Found terrain");
            Debug.Log($"   Size: {terrainData.size.x}x{terrainData.size.z}");
            Debug.Log($"   Height: {terrainData.size.y}");
            Debug.Log($"   Position: {terrain.transform.position}");
        }
        else
        {
            Debug.LogWarning("⚠️ TerrainHelper: No terrain found! Using flat ground (Y=0)");
        }
    }

    /// <summary>
    /// Obtém a altura EXATA do terreno na posição (worldX, worldZ)
    /// Usa interpolação bilinear para suavidade
    /// </summary>
    public float GetHeightAt(float worldX, float worldZ)
    {
        if (terrain == null || terrainData == null)
        {
            if (debugMode)
                Debug.Log($"[TerrainHelper] No terrain, returning Y=0 for ({worldX}, {worldZ})");
            return 0f;
        }

        Vector3 terrainPos = terrain.transform.position;
        Vector3 terrainSize = terrainData.size;

        // Coordenadas locais (relativas ao terreno)
        float localX = worldX - terrainPos.x;
        float localZ = worldZ - terrainPos.z;

        // Normaliza (0 a 1)
        float normX = localX / terrainSize.x;
        float normZ = localZ / terrainSize.z;

        // Fora dos limites? Retorna altura do terreno base
        if (normX < 0 || normX > 1 || normZ < 0 || normZ > 1)
        {
            if (debugMode)
                Debug.LogWarning($"[TerrainHelper] Out of bounds: ({worldX}, {worldZ})");
            return terrainPos.y;
        }

        // Obtém altura interpolada do terreno
        float height = terrainData.GetInterpolatedHeight(normX, normZ);
        float finalHeight = terrainPos.y + height;

        if (debugMode)
            Debug.Log($"[TerrainHelper] Height at ({worldX}, {worldZ}) = {finalHeight}");

        return finalHeight;
    }

    /// <summary>
    /// Ajusta uma posição para ficar no chão do terreno
    /// </summary>
    public Vector3 ClampToGround(Vector3 position, float offset = -1f)
    {
        if (offset < 0)
            offset = characterHeightOffset;

        position.y = GetHeightAt(position.x, position.z) + offset;
        return position;
    }

    /// <summary>
    /// Ajusta apenas o Y de uma posição existente
    /// </summary>
    public void AdjustHeight(ref Vector3 position, float offset = -1f)
    {
        if (offset < 0)
            offset = characterHeightOffset;

        position.y = GetHeightAt(position.x, position.z) + offset;
    }

    /// <summary>
    /// Verifica se uma posição está dentro dos limites do terreno
    /// </summary>
    public bool IsInBounds(Vector3 position)
    {
        if (terrain == null)
            return true;

        Vector3 terrainPos = terrain.transform.position;
        Vector3 terrainSize = terrainData.size;

        return position.x >= terrainPos.x && position.x <= terrainPos.x + terrainSize.x &&
               position.z >= terrainPos.z && position.z <= terrainPos.z + terrainSize.z;
    }

    /// <summary>
    /// Raycast específico para terreno (usado em cliques de mouse)
    /// </summary>
    public bool RaycastTerrain(Ray ray, out Vector3 hitPoint)
    {
        hitPoint = Vector3.zero;

        if (terrain == null)
        {
            // Fallback: plano horizontal Y=0
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out float distance))
            {
                hitPoint = ray.GetPoint(distance);
                if (debugMode)
                    Debug.Log($"[TerrainHelper] Flat terrain hit: {hitPoint}");
                return true;
            }
            return false;
        }

        // Raycast no terreno
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 2000f))
        {
            // Verifica se acertou o terreno especificamente
            Terrain hitTerrain = hit.collider.GetComponent<Terrain>();
            
            if (hitTerrain != null)
            {
                hitPoint = hit.point;
                if (debugMode)
                    Debug.Log($"[TerrainHelper] Terrain hit: {hitPoint}");
                return true;
            }
        }

        if (debugMode)
            Debug.LogWarning("[TerrainHelper] No terrain hit");
        
        return false;
    }

    /// <summary>
    /// Obtém a normal (direção "para cima") do terreno em uma posição
    /// Útil para alinhar objetos com o terreno
    /// </summary>
    public Vector3 GetNormalAt(float worldX, float worldZ)
    {
        if (terrain == null || terrainData == null)
            return Vector3.up;

        Vector3 terrainPos = terrain.transform.position;
        Vector3 terrainSize = terrainData.size;

        float localX = worldX - terrainPos.x;
        float localZ = worldZ - terrainPos.z;

        float normX = localX / terrainSize.x;
        float normZ = localZ / terrainSize.z;

        if (normX < 0 || normX > 1 || normZ < 0 || normZ > 1)
            return Vector3.up;

        return terrainData.GetInterpolatedNormal(normX, normZ);
    }

    /// <summary>
    /// Força recarregar o terreno (útil após troca de cena)
    /// </summary>
    public void ReloadTerrain()
    {
        FindTerrain();
    }

    private void OnDrawGizmos()
    {
        if (!debugMode || terrain == null)
            return;

        // Desenha os limites do terreno
        Vector3 terrainPos = terrain.transform.position;
        Vector3 terrainSize = terrainData.size;

        Gizmos.color = Color.yellow;
        
        Vector3 corner1 = terrainPos;
        Vector3 corner2 = terrainPos + new Vector3(terrainSize.x, 0, 0);
        Vector3 corner3 = terrainPos + new Vector3(terrainSize.x, 0, terrainSize.z);
        Vector3 corner4 = terrainPos + new Vector3(0, 0, terrainSize.z);

        Gizmos.DrawLine(corner1, corner2);
        Gizmos.DrawLine(corner2, corner3);
        Gizmos.DrawLine(corner3, corner4);
        Gizmos.DrawLine(corner4, corner1);
    }
}