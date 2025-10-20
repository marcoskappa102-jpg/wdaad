using UnityEngine;
using System;
using System.Linq;
using Newtonsoft.Json.Linq;

/// <summary>
/// ✅ MessageHandler COMPLETO - Trata TODOS os tipos de mensagem do servidor
/// </summary>
public class MessageHandler : MonoBehaviour
{
    public static MessageHandler Instance { get; private set; }

    // Eventos para cada tipo de mensagem
    public event Action<LoginResponseData> OnLoginResponse;
    public event Action<RegisterResponseData> OnRegisterResponse;
    public event Action<CreateCharacterResponseData> OnCreateCharacterResponse;
    public event Action<SelectCharacterResponseData> OnSelectCharacterResponse;
    public event Action<PlayerJoinedData> OnPlayerJoined;
    public event Action<string> OnPlayerDisconnected;
    public event Action<WorldStateData> OnWorldStateUpdate;
    
    public event Action<CombatResultData> OnCombatResult;
    public event Action<LevelUpData> OnLevelUp;
    public event Action<PlayerDeathData> OnPlayerDeath;
    public event Action<PlayerRespawnData> OnPlayerRespawn;
    public event Action<AttackStartedData> OnAttackStarted;
    public event Action<PlayerAttackData> OnPlayerAttack;
    public event Action<StatusPointAddedData> OnStatusPointAdded;
    
    public event Action<InventoryData> OnInventoryReceived;
    public event Action<LootReceivedData> OnLootReceived;
    public event Action<ItemUsedData> OnItemUsed;
    public event Action<ItemEquippedData> OnItemEquipped;
    public event Action<ItemEquippedData> OnItemUnequipped;
    public event Action<ItemDroppedData> OnItemDropped;

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
        if (ClientManager.Instance != null)
        {
            ClientManager.Instance.OnMessageReceived += HandleMessage;
        }
    }

    private void HandleMessage(string message)
    {
        try
        {
            var json = JObject.Parse(message);
            var type = json["type"]?.ToString();

            // 🔍 LOG para debug - mostra o tipo de mensagem recebida
            Debug.Log($"📨 Message type: '{type}'");

            switch (type)
            {
				case "pong":
					// Silencioso - ping/pong funcionando
					break;
                // ==================== LOGIN/ACCOUNT ====================
                case "loginResponse":
                    HandleLoginResponse(json);
                    break;

                case "registerResponse":
                    HandleRegisterResponse(json);
                    break;

                // ==================== CHARACTER ====================
                case "createCharacterResponse":
                    HandleCreateCharacterResponse(json);
                    break;

                case "selectCharacterResponse":
                    HandleSelectCharacterResponse(json);
                    break;

                // ==================== WORLD/PLAYERS ====================
                case "playerJoined":
                    HandlePlayerJoined(json);
                    break;

                case "playerDisconnected":
                    HandlePlayerDisconnected(json);
                    break;

                case "worldState":
                    HandleWorldState(json);
                    break;

                // ==================== MOVEMENT ====================
                case "moveAccepted":
                    Debug.Log("✅ Move accepted");
                    break;

                // ==================== COMBAT ====================
                case "combatResult":
                    HandleCombatResult(json);
                    break;

                case "attackStarted":
                    HandleAttackStarted(json);
                    break;
                
                case "playerAttack":
                    HandlePlayerAttack(json);
                    break;

                // ==================== LEVEL/STATS ====================
                case "levelUp":
                    HandleLevelUp(json);
                    break;
                
                case "statusPointAdded":
                    HandleStatusPointAdded(json);
                    break;

                // ✅ CRÍTICO: Atualização de stats (HP/MP após usar poção)
                case "playerStatsUpdate":
                    HandlePlayerStatsUpdate(json);
                    break;

                // ==================== DEATH/RESPAWN ====================
                case "playerDeath":
                    HandlePlayerDeath(json);
                    break;

                case "playerRespawn":
                    HandlePlayerRespawn(json);
                    break;

                case "respawnResponse":
                    Debug.Log("✅ Respawn response received");
                    break;

                // ==================== INVENTORY/ITEMS ====================
                case "inventoryResponse":
                    HandleInventoryResponse(json);
                    break;

                case "lootReceived":
                    HandleLootReceived(json);
                    break;

                case "itemUsed":
                    HandleItemUsed(json);
                    break;

                case "itemUseFailed":
                    HandleItemUseFailed(json);
                    break;

                case "itemEquipped":
                    HandleItemEquipped(json);
                    break;

                case "itemUnequipped":
                    HandleItemUnequipped(json);
                    break;

                case "itemDropped":
                    HandleItemDropped(json);
                    break;

                // ==================== ERRORS ====================
                case "error":
                    string errorMsg = json["message"]?.ToString() ?? "Unknown error";
                    Debug.LogError($"❌ Server error: {errorMsg}");
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.AddCombatLog($"<color=red>❌ Erro: {errorMsg}</color>");
                    }
                    break;

                // ==================== UNKNOWN ====================
                default:
                    Debug.LogWarning($"⚠️ Unknown message type: '{type}'");
                    Debug.LogWarning($"   Full message: {message.Substring(0, Math.Min(200, message.Length))}...");
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Error parsing message: {ex.Message}");
            Debug.LogError($"   Message preview: {message.Substring(0, Math.Min(100, message.Length))}...");
        }
    }

    // ==================== HANDLERS ====================

    private void HandlePlayerStatsUpdate(JObject json)
    {
        try
        {
            string playerId = json["playerId"]?.ToString();
            int health = json["health"]?.ToObject<int>() ?? 0;
            int maxHealth = json["maxHealth"]?.ToObject<int>() ?? 0;
            int mana = json["mana"]?.ToObject<int>() ?? 0;
            int maxMana = json["maxMana"]?.ToObject<int>() ?? 0;

            Debug.Log($"📊 Stats update: HP={health}/{maxHealth}, MP={mana}/{maxMana}");

            // Atualiza UI se for o player local
            if (playerId == ClientManager.Instance.PlayerId && UIManager.Instance != null)
            {
                UIManager.Instance.UpdateHealthBar(health, maxHealth);
                UIManager.Instance.UpdateManaBar(mana, maxMana);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Error in HandlePlayerStatsUpdate: {ex.Message}");
        }
    }

    private void HandleWorldState(JObject json)
    {
        var data = new WorldStateData
        {
            timestamp = json["timestamp"]?.ToObject<long>() ?? 0,
            players = json["players"]?.ToObject<PlayerStateData[]>(),
            monsters = json["monsters"]?.ToObject<MonsterStateData[]>()
        };
        
        OnWorldStateUpdate?.Invoke(data);
    }

    private void HandleCombatResult(JObject json)
    {
        var data = json["data"]?.ToObject<CombatResultData>();
        if (data != null)
        {
            OnCombatResult?.Invoke(data);
        }
    }

    private void HandleLevelUp(JObject json)
    {
        var data = new LevelUpData
        {
            playerId = json["playerId"]?.ToString(),
            characterName = json["characterName"]?.ToString(),
            newLevel = json["newLevel"]?.ToObject<int>() ?? 1,
            statusPoints = json["statusPoints"]?.ToObject<int>() ?? 0,
            experience = json["experience"]?.ToObject<int>() ?? 0,
            requiredExp = json["requiredExp"]?.ToObject<int>() ?? 100,
            newStats = json["newStats"]?.ToObject<StatsData>()
        };
        
        OnLevelUp?.Invoke(data);
    }

    private void HandlePlayerDeath(JObject json)
    {
        var data = json.ToObject<PlayerDeathData>();
        OnPlayerDeath?.Invoke(data);
    }

    private void HandlePlayerRespawn(JObject json)
    {
        var data = json.ToObject<PlayerRespawnData>();
        OnPlayerRespawn?.Invoke(data);
    }

    private void HandleAttackStarted(JObject json)
    {
        var data = new AttackStartedData
        {
            monsterId = json["monsterId"]?.ToObject<int>() ?? 0,
            monsterName = json["monsterName"]?.ToString()
        };
        OnAttackStarted?.Invoke(data);
    }

    private void HandlePlayerAttack(JObject json)
    {
        var data = new PlayerAttackData
        {
            playerId = json["playerId"]?.ToString(),
            characterName = json["characterName"]?.ToString(),
            monsterId = json["monsterId"]?.ToObject<int>() ?? 0,
            monsterName = json["monsterName"]?.ToString()
        };
        
        OnPlayerAttack?.Invoke(data);
    }

    private void HandleStatusPointAdded(JObject json)
    {
        var data = new StatusPointAddedData
        {
            playerId = json["playerId"]?.ToString(),
            characterName = json["characterName"]?.ToString(),
            stat = json["stat"]?.ToString(),
            statusPoints = json["statusPoints"]?.ToObject<int>() ?? 0,
            newStats = json["newStats"]?.ToObject<StatsData>()
        };
        
        Debug.Log($"✅ Status point added: {data.stat} - Remaining: {data.statusPoints}");
        OnStatusPointAdded?.Invoke(data);
    }

    // ==================== INVENTORY ====================

    private void HandleInventoryResponse(JObject json)
    {
        bool success = json["success"]?.ToObject<bool>() ?? false;
        
        if (!success)
        {
            Debug.LogError("❌ Failed to receive inventory");
            return;
        }

        var inventoryJson = json["inventory"];
        var data = inventoryJson?.ToObject<InventoryData>();
        
        if (data != null)
        {
            Debug.Log($"📦 Inventory received: {data.items.Count} items, {data.gold} gold");
            OnInventoryReceived?.Invoke(data);
        }
    }

    private void HandleLootReceived(JObject json)
    {
        var data = new LootReceivedData
        {
            playerId = json["playerId"]?.ToString(),
            characterName = json["characterName"]?.ToString(),
            gold = json["gold"]?.ToObject<int>() ?? 0,
            items = json["items"]?.ToObject<System.Collections.Generic.List<LootedItemData>>() 
                    ?? new System.Collections.Generic.List<LootedItemData>()
        };

        Debug.Log($"💰 Loot: {data.gold} gold, {data.items.Count} items");
        OnLootReceived?.Invoke(data);
    }

    private void HandleItemUsed(JObject json)
    {
        var data = new ItemUsedData
        {
            playerId = json["playerId"]?.ToString(),
            instanceId = json["instanceId"]?.ToObject<int>() ?? 0,
            health = json["health"]?.ToObject<int>() ?? 0,
            maxHealth = json["maxHealth"]?.ToObject<int>() ?? 0,
            mana = json["mana"]?.ToObject<int>() ?? 0,
            maxMana = json["maxMana"]?.ToObject<int>() ?? 0,
            remainingQuantity = json["remainingQuantity"]?.ToObject<int>() ?? 0
        };

        Debug.Log($"💊 Item used: {data.instanceId}");
        OnItemUsed?.Invoke(data);
    }

    private void HandleItemUseFailed(JObject json)
    {
        string reason = json["reason"]?.ToString() ?? "";
        string message = json["message"]?.ToString() ?? "Não foi possível usar o item";

        Debug.LogWarning($"⚠️ Item use failed: {reason}");

        if (UIManager.Instance != null)
        {
            string coloredMessage = reason switch
            {
                "HP_FULL" => "<color=yellow>💊 HP já está cheio!</color>",
                "MP_FULL" => "<color=cyan>💊 MP já está cheio!</color>",
                "ON_COOLDOWN" => "<color=orange>⏳ Aguarde antes de usar outra poção!</color>",
                _ => $"<color=yellow>⚠️ {message}</color>"
            };

            UIManager.Instance.AddCombatLog(coloredMessage);
        }
    }

    private void HandleItemEquipped(JObject json)
    {
        var data = new ItemEquippedData
        {
            playerId = json["playerId"]?.ToString(),
            instanceId = json["instanceId"]?.ToObject<int>() ?? 0,
            newStats = json["newStats"]?.ToObject<StatsData>(),
            equipment = json["equipment"]?.ToObject<EquipmentData>()
        };

        Debug.Log($"⚔️ Item equipped: {data.instanceId}");
        OnItemEquipped?.Invoke(data);
    }

    private void HandleItemUnequipped(JObject json)
    {
        var data = new ItemEquippedData
        {
            playerId = json["playerId"]?.ToString(),
            instanceId = 0,
            newStats = json["newStats"]?.ToObject<StatsData>(),
            equipment = json["equipment"]?.ToObject<EquipmentData>()
        };

        Debug.Log($"⚔️ Item unequipped: {json["slot"]}");
        OnItemUnequipped?.Invoke(data);
    }

    private void HandleItemDropped(JObject json)
    {
        var data = new ItemDroppedData
        {
            playerId = json["playerId"]?.ToString(),
            instanceId = json["instanceId"]?.ToObject<int>() ?? 0,
            quantity = json["quantity"]?.ToObject<int>() ?? 1
        };

        Debug.Log($"📤 Item dropped: {data.instanceId}");
        OnItemDropped?.Invoke(data);
    }

    // ==================== LOGIN/CHARACTER ====================

    private void HandleLoginResponse(JObject json)
    {
        var data = json["data"]?.ToObject<LoginResponseData>();
        OnLoginResponse?.Invoke(data);
    }

    private void HandleRegisterResponse(JObject json)
    {
        var data = new RegisterResponseData
        {
            success = json["success"]?.ToObject<bool>() ?? false,
            message = json["message"]?.ToString()
        };
        OnRegisterResponse?.Invoke(data);
    }

    private void HandleCreateCharacterResponse(JObject json)
    {
        var data = new CreateCharacterResponseData
        {
            success = json["success"]?.ToObject<bool>() ?? false,
            message = json["message"]?.ToString(),
            character = json["character"]?.ToObject<CharacterData>()
        };
        OnCreateCharacterResponse?.Invoke(data);
    }

    private void HandleSelectCharacterResponse(JObject json)
    {
        var data = new SelectCharacterResponseData
        {
            success = json["success"]?.ToObject<bool>() ?? false,
            message = json["message"]?.ToString(),
            character = json["character"]?.ToObject<CharacterData>(),
            playerId = json["playerId"]?.ToString(),
            allPlayers = json["allPlayers"]?.ToObject<PlayerStateData[]>(),
            allMonsters = json["allMonsters"]?.ToObject<MonsterStateData[]>(),
            inventory = json["inventory"]?.ToObject<InventoryData>()
        };

        if (data.success && !string.IsNullOrEmpty(data.playerId))
        {
            ClientManager.Instance.SetPlayerId(data.playerId);
        }

        OnSelectCharacterResponse?.Invoke(data);
    }

    private void HandlePlayerJoined(JObject json)
    {
        var playerData = json["player"];
        var data = new PlayerJoinedData
        {
            playerId = playerData["playerId"]?.ToString(),
            characterName = playerData["characterName"]?.ToString(),
            position = playerData["position"]?.ToObject<PositionData>(),
            raca = playerData["raca"]?.ToString(),
            classe = playerData["classe"]?.ToString(),
            level = playerData["level"]?.ToObject<int>() ?? 1,
            health = playerData["health"]?.ToObject<int>() ?? 100,
            maxHealth = playerData["maxHealth"]?.ToObject<int>() ?? 100
        };
        OnPlayerJoined?.Invoke(data);
    }

    private void HandlePlayerDisconnected(JObject json)
    {
        var playerId = json["playerId"]?.ToString();
        OnPlayerDisconnected?.Invoke(playerId);
    }

    private void OnDestroy()
    {
        if (ClientManager.Instance != null)
        {
            ClientManager.Instance.OnMessageReceived -= HandleMessage;
        }
    }
}