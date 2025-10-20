using UnityEngine;
using System.Collections.Generic;

public class WorldManager : MonoBehaviour
{
    public static WorldManager Instance { get; private set; }

    [Header("Prefabs")]
    public GameObject playerPrefab;
    public GameObject monsterPrefabFallback; // Prefab padrão se não encontrar

    [Header("Camera")]
    public CameraController cameraController;

    [Header("Spawn Settings")]
    public bool useCustomSpawnPositions = true;
    public float characterHeightOffset = 0f;
	
    public Vector3 humanoSpawn = new Vector3(0, 0, 0);
    public Vector3 elfoSpawn = new Vector3(20, 0, 20);
    public Vector3 anaoSpawn = new Vector3(-20, 0, -20);
    public Vector3 orcSpawn = new Vector3(-20, 0, 20);

    private Dictionary<string, GameObject> playerObjects = new Dictionary<string, GameObject>();
    private Dictionary<int, GameObject> monsterObjects = new Dictionary<int, GameObject>();
    
    // 🆕 Cache de prefabs de monstros
    private Dictionary<string, GameObject> monsterPrefabCache = new Dictionary<string, GameObject>();
    
    private GameObject localPlayer;
    private string localPlayerId;
    private CharacterData localCharacterData;
    private bool isInitialized = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (isInitialized) return;

        Debug.Log("🌍 WorldManager: Initializing...");

        if (MessageHandler.Instance != null)
        {
            MessageHandler.Instance.OnSelectCharacterResponse += HandleSelectCharacterResponse;
            MessageHandler.Instance.OnPlayerJoined += HandlePlayerJoined;
            MessageHandler.Instance.OnPlayerDisconnected += HandlePlayerDisconnected;
            MessageHandler.Instance.OnWorldStateUpdate += HandleWorldStateUpdate;
            MessageHandler.Instance.OnCombatResult += HandleCombatResult;
            MessageHandler.Instance.OnLevelUp += HandleLevelUp;
            MessageHandler.Instance.OnPlayerDeath += HandlePlayerDeath;
            MessageHandler.Instance.OnPlayerRespawn += HandlePlayerRespawn;
            MessageHandler.Instance.OnStatusPointAdded += HandleStatusPointAdded;
            
            isInitialized = true;
            Debug.Log("✅ WorldManager: Event handlers registered");
            CheckPendingCharacterData();
        }
        else
        {
            Debug.LogWarning("⚠️ MessageHandler not found, retrying...");
            Invoke("Initialize", 0.5f);
        }
    }

    /// <summary>
    /// 🆕 Carrega prefab de monstro do Resources
    /// </summary>
    private GameObject LoadMonsterPrefab(string prefabPath)
    {
        // Verifica cache primeiro
        if (monsterPrefabCache.TryGetValue(prefabPath, out GameObject cachedPrefab))
        {
            return cachedPrefab;
        }

        // Tenta carregar do Resources
        if (!string.IsNullOrEmpty(prefabPath))
        {
            GameObject loadedPrefab = Resources.Load<GameObject>(prefabPath);
            
            if (loadedPrefab != null)
            {
                monsterPrefabCache[prefabPath] = loadedPrefab;
                Debug.Log($"✅ Loaded monster prefab: {prefabPath}");
                return loadedPrefab;
            }
            else
            {
                Debug.LogWarning($"⚠️ Prefab not found at: Resources/{prefabPath}");
            }
        }

        // Fallback: usa prefab padrão
        if (monsterPrefabFallback != null)
        {
            return monsterPrefabFallback;
        }

        Debug.LogError("❌ No monster prefab available!");
        return null;
    }

    private void CheckPendingCharacterData()
    {
        if (PlayerPrefs.HasKey("PendingCharacterData"))
        {
            Debug.Log("📦 Found pending character data");
            string jsonData = PlayerPrefs.GetString("PendingCharacterData");
            PlayerPrefs.DeleteKey("PendingCharacterData");

            try
            {
                var data = JsonUtility.FromJson<SelectCharacterResponseData>(jsonData);
                HandleSelectCharacterResponse(data);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Failed to parse pending data: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ No pending character data found!");
        }
    }

    private void HandleSelectCharacterResponse(SelectCharacterResponseData data)
    {
        if (!data.success || data.character == null)
        {
            Debug.LogError("❌ Select character failed!");
            return;
        }

        Debug.Log($"🎮 Loading character: {data.character.nome}");

        localPlayerId = data.playerId;
        localCharacterData = data.character;
        
        if (UIManager.Instance != null)
        {
            Debug.Log("📊 Updating UIManager with character data");
            UIManager.Instance.UpdateLocalCharacterData(data.character);
        }
        
        Vector3 spawnPos = useCustomSpawnPositions 
            ? GetSpawnPositionByRace(data.character.raca)
            : new Vector3(data.character.position.x, characterHeightOffset, data.character.position.z);

        if (TerrainHelper.Instance != null)
        {
            spawnPos = TerrainHelper.Instance.ClampToGround(spawnPos, 0f);
        }

        Debug.Log($"📍 Spawning at: {spawnPos}");

        localPlayer = SpawnPlayer(data.playerId, data.character.nome, spawnPos, true, 
                                 data.character.raca, data.character.classe,
                                 data.character.health, data.character.maxHealth,
                                 data.character.level, data.character.isDead);

        if (cameraController != null && localPlayer != null)
        {
            cameraController.SetTarget(localPlayer.transform);
            Debug.Log("📹 Camera target set");
        }

        if (data.allPlayers != null)
        {
            Debug.Log($"👥 Spawning {data.allPlayers.Length} other players");
            foreach (var playerState in data.allPlayers)
            {
                if (playerState.playerId != data.playerId)
                {
                    Vector3 otherPos = new Vector3(playerState.position.x, characterHeightOffset, playerState.position.z);
                    
                    if (TerrainHelper.Instance != null)
                    {
                        otherPos = TerrainHelper.Instance.ClampToGround(otherPos, characterHeightOffset);
                    }
                    
                    SpawnPlayer(playerState.playerId, playerState.characterName, otherPos, 
                               false, playerState.raca, playerState.classe,
                               playerState.health, playerState.maxHealth,
                               playerState.level, playerState.isDead);
                }
            }
        }

        // 🆕 Spawn monstros com prefabs corretos
        if (data.allMonsters != null)
        {
            Debug.Log($"👹 Spawning {data.allMonsters.Length} monsters");
            foreach (var monsterState in data.allMonsters)
            {
                if (monsterState.isAlive)
                {
                    SpawnMonster(monsterState);
                }
            }
        }

        Debug.Log($"✅ World setup complete! Players: {playerObjects.Count}, Monsters: {monsterObjects.Count}");
    }

    private void HandleWorldStateUpdate(WorldStateData data)
    {
        if (data.players != null)
        {
            foreach (var playerState in data.players)
            {
                if (playerObjects.TryGetValue(playerState.playerId, out GameObject playerObj))
                {
                    PlayerController controller = playerObj.GetComponent<PlayerController>();
                    if (controller != null)
                    {
                        Vector3 serverPos = new Vector3(playerState.position.x, playerState.position.y, playerState.position.z);
                        Vector3? targetPos = playerState.targetPosition != null ? 
                            new Vector3(playerState.targetPosition.x, playerState.targetPosition.y, playerState.targetPosition.z) : null;

                        controller.UpdateFromServer(serverPos, targetPos, playerState.isMoving, 
                                                   playerState.health, playerState.maxHealth, 
                                                   playerState.isDead, playerState.inCombat);
                        
                        if (playerState.playerId == localPlayerId && UIManager.Instance != null)
                        {
                            UIManager.Instance.UpdateHealthBar(playerState.health, playerState.maxHealth);
                            UIManager.Instance.UpdateManaBar(playerState.mana, playerState.maxMana);
                            UIManager.Instance.UpdateExpBar(playerState.experience, 100 * playerState.level * playerState.level);
                            
                            if (localCharacterData != null)
                            {
                                localCharacterData.health = playerState.health;
                                localCharacterData.mana = playerState.mana;
                                localCharacterData.experience = playerState.experience;
                                localCharacterData.statusPoints = playerState.statusPoints;
                            }
                        }
                    }
                }
            }
        }

        if (data.monsters != null)
        {
            foreach (var monsterState in data.monsters)
            {
                if (monsterObjects.TryGetValue(monsterState.id, out GameObject monsterObj))
                {
                    MonsterController controller = monsterObj.GetComponent<MonsterController>();
                    if (controller != null)
                    {
                        Vector3 pos = new Vector3(monsterState.position.x, monsterState.position.y, monsterState.position.z);
                        controller.UpdateFromServer(pos, monsterState.currentHealth, monsterState.isAlive, 
                                                   monsterState.isMoving, monsterState.inCombat);
                    }
                }
                else if (monsterState.isAlive)
                {
                    SpawnMonster(monsterState);
                }
            }
        }
    }

    private void HandleCombatResult(CombatResultData data)
    {
        Debug.Log($"⚔️ Combat: {data.attackerType} dealt {data.damage} damage to {data.targetType}");
        
        if (data.attackerType == "player" && data.targetType == "monster")
        {
            if (monsterObjects.TryGetValue(int.Parse(data.targetId), out GameObject monsterObj))
            {
                var monster = monsterObj.GetComponent<MonsterController>();
                monster?.ShowDamage(data.damage, data.isCritical);
            }
        }
        else if (data.attackerType == "monster" && data.targetType == "player")
        {
            if (playerObjects.TryGetValue(data.targetId, out GameObject playerObj))
            {
                var player = playerObj.GetComponent<PlayerController>();
                player?.ShowDamage(data.damage, data.isCritical);
            }
        }
    }

    private void HandleLevelUp(LevelUpData data)
    {
        Debug.Log($"🌟 {data.characterName} leveled up to {data.newLevel}!");
        
        if (playerObjects.TryGetValue(data.playerId, out GameObject playerObj))
        {
            var player = playerObj.GetComponent<PlayerController>();
            if (player != null)
            {
                player.level = data.newLevel;
                player.maxHealth = data.newStats.maxHealth;
                player.currentHealth = data.newStats.maxHealth;
                
                if (player.nameText != null)
                {
                    player.nameText.text = $"{player.characterName}\nLv.{data.newLevel}";
                }
                
                player.UpdateHealthBar();
            }
            
            if (data.playerId == localPlayerId && UIManager.Instance != null && localCharacterData != null)
            {
                localCharacterData.level = data.newLevel;
                localCharacterData.maxHealth = data.newStats.maxHealth;
                localCharacterData.health = data.newStats.maxHealth;
                localCharacterData.maxMana = data.newStats.maxMana;
                localCharacterData.mana = data.newStats.maxMana;
                localCharacterData.attackPower = data.newStats.attackPower;
                localCharacterData.magicPower = data.newStats.magicPower;
                localCharacterData.defense = data.newStats.defense;
                localCharacterData.attackSpeed = data.newStats.attackSpeed;
                localCharacterData.strength = data.newStats.strength;
                localCharacterData.intelligence = data.newStats.intelligence;
                localCharacterData.dexterity = data.newStats.dexterity;
                localCharacterData.vitality = data.newStats.vitality;
                localCharacterData.statusPoints = data.statusPoints;
                localCharacterData.experience = data.experience;
                
                UIManager.Instance.UpdateLocalCharacterData(localCharacterData);
            }
        }
    }

    private void HandleStatusPointAdded(StatusPointAddedData data)
    {
        Debug.Log($"📈 {data.characterName} added point to {data.stat}");
        
        if (data.playerId == localPlayerId && UIManager.Instance != null && localCharacterData != null)
        {
            localCharacterData.statusPoints = data.statusPoints;
            localCharacterData.strength = data.newStats.strength;
            localCharacterData.intelligence = data.newStats.intelligence;
            localCharacterData.dexterity = data.newStats.dexterity;
            localCharacterData.vitality = data.newStats.vitality;
            localCharacterData.maxHealth = data.newStats.maxHealth;
            localCharacterData.maxMana = data.newStats.maxMana;
            localCharacterData.attackPower = data.newStats.attackPower;
            localCharacterData.defense = data.newStats.defense;
            localCharacterData.attackSpeed = data.newStats.attackSpeed;
            
            UIManager.Instance.UpdateLocalCharacterData(localCharacterData);
            UIManager.Instance.AddCombatLog($"<color=lime>✅ {data.stat.ToUpper()} aumentado!</color>");
        }
    }

    private void HandlePlayerDeath(PlayerDeathData data)
    {
        Debug.Log($"💀 {data.characterName} died!");
        
        if (playerObjects.TryGetValue(data.playerId, out GameObject playerObj))
        {
            var player = playerObj.GetComponent<PlayerController>();
            player?.OnDeath();
        }
    }

    private void HandlePlayerRespawn(PlayerRespawnData data)
    {
        Debug.Log($"✨ {data.characterName} respawned!");
        
        if (playerObjects.TryGetValue(data.playerId, out GameObject playerObj))
        {
            Vector3 pos = new Vector3(data.position.x, data.position.y, data.position.z);
            
            if (TerrainHelper.Instance != null)
            {
                pos = TerrainHelper.Instance.ClampToGround(pos, characterHeightOffset);
            }
            
            var player = playerObj.GetComponent<PlayerController>();
            player?.OnRespawn(pos);
            
            if (data.playerId == localPlayerId && UIManager.Instance != null)
            {
                UIManager.Instance.UpdateHealthBar(data.health, data.maxHealth);
                UIManager.Instance.ShowCombatStatus(false);
            }
        }
    }

    private void HandlePlayerJoined(PlayerJoinedData data)
    {
        if (playerObjects.ContainsKey(data.playerId)) return;

        Debug.Log($"👤 Player joined: {data.characterName}");

        Vector3 position = useCustomSpawnPositions 
            ? GetSpawnPositionByRace(data.raca)
            : new Vector3(data.position.x, characterHeightOffset, data.position.z);

        if (TerrainHelper.Instance != null)
        {
            position = TerrainHelper.Instance.ClampToGround(position, characterHeightOffset);
        }

        SpawnPlayer(data.playerId, data.characterName, position, false, data.raca, data.classe,
                   data.health, data.maxHealth, data.level, false);
    }

    private void HandlePlayerDisconnected(string playerId)
    {
        if (playerObjects.TryGetValue(playerId, out GameObject playerObj))
        {
            Debug.Log($"👋 Player disconnected: {playerId}");
            Destroy(playerObj);
            playerObjects.Remove(playerId);
        }
    }

    private GameObject SpawnPlayer(string playerId, string characterName, Vector3 position, bool isLocal, 
                                   string raca, string classe, int health, int maxHealth, int level, bool isDead)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("❌ PlayerPrefab is NULL!");
            return null;
        }

        GameObject playerObj = Instantiate(playerPrefab, position, Quaternion.identity);
        playerObj.name = $"Player_{characterName}_{playerId.Substring(0, 8)}";

        PlayerController controller = playerObj.GetComponent<PlayerController>();
        if (controller == null)
        {
            controller = playerObj.AddComponent<PlayerController>();
        }

        controller.characterHeightOffset = characterHeightOffset;
        controller.Initialize(playerId, characterName, isLocal, health, maxHealth, level);
        
        if (isDead)
        {
            controller.OnDeath();
        }

        playerObjects[playerId] = playerObj;
        Debug.Log($"✅ Spawned player: {characterName} at {position}");
        return playerObj;
    }

    /// <summary>
    /// 🆕 Spawna monstro usando prefab específico
    /// </summary>
    private void SpawnMonster(MonsterStateData data)
    {
        // 🆕 Carrega prefab correto
        GameObject prefab = LoadMonsterPrefab(data.prefabPath);
        
        if (prefab == null)
        {
            Debug.LogError($"❌ No prefab available for monster {data.name}!");
            return;
        }

        Vector3 pos = new Vector3(data.position.x, data.position.y, data.position.z);
        
        if (TerrainHelper.Instance != null)
        {
            pos = TerrainHelper.Instance.ClampToGround(pos, characterHeightOffset);
        }
        
        GameObject monsterObj = Instantiate(prefab, pos, Quaternion.identity);
        monsterObj.name = $"Monster_{data.name}_{data.id}";

        MonsterController controller = monsterObj.GetComponent<MonsterController>();
        if (controller == null)
        {
            controller = monsterObj.AddComponent<MonsterController>();
        }

        controller.characterHeightOffset = characterHeightOffset;
        controller.Initialize(data.id, data.name, data.level, data.currentHealth, data.maxHealth, data.isAlive);
        monsterObjects[data.id] = monsterObj;
        
        Debug.Log($"✅ Spawned {data.name} (ID: {data.id}) using prefab: {data.prefabPath}");
    }

    private Vector3 GetSpawnPositionByRace(string raca)
    {
        Vector3 spawn;
        switch (raca)
        {
            case "Humano": spawn = humanoSpawn; break;
            case "Elfo": spawn = elfoSpawn; break;
            case "Anao": spawn = anaoSpawn; break;
            case "Orc": spawn = orcSpawn; break;
            default: spawn = humanoSpawn; break;
        }
        
        if (TerrainHelper.Instance != null)
        {
            spawn = TerrainHelper.Instance.ClampToGround(spawn, characterHeightOffset);
        }
        else
        {
            spawn.y = characterHeightOffset;
        }
        
        return spawn;
    }

    private void OnDestroy()
    {
        if (MessageHandler.Instance != null)
        {
            MessageHandler.Instance.OnSelectCharacterResponse -= HandleSelectCharacterResponse;
            MessageHandler.Instance.OnPlayerJoined -= HandlePlayerJoined;
            MessageHandler.Instance.OnPlayerDisconnected -= HandlePlayerDisconnected;
            MessageHandler.Instance.OnWorldStateUpdate -= HandleWorldStateUpdate;
            MessageHandler.Instance.OnCombatResult -= HandleCombatResult;
            MessageHandler.Instance.OnLevelUp -= HandleLevelUp;
            MessageHandler.Instance.OnPlayerDeath -= HandlePlayerDeath;
            MessageHandler.Instance.OnPlayerRespawn -= HandlePlayerRespawn;
            MessageHandler.Instance.OnStatusPointAdded -= HandleStatusPointAdded;
        }
    }

    public CharacterData GetLocalCharacterData()
    {
        return localCharacterData;
    }
}