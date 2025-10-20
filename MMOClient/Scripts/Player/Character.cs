using System;
using System.Collections.Generic;

// ==================== CHARACTER DATA ====================

[Serializable]
public class CharacterData
{
    public int id;
    public int accountId;
    public string nome;
    public string raca;
    public string classe;
    
    public int level;
    public int experience;
    public int statusPoints;
    
    public int health;
    public int maxHealth;
    public int mana;
    public int maxMana;
    
    public int strength;
    public int intelligence;
    public int dexterity;
    public int vitality;
    
    public int attackPower;
    public int magicPower;
    public int defense;
    public float attackSpeed;
    
    public PositionData position;
    public bool isDead;
    
    public int GetRequiredExp()
    {
        return 100 * level * level;
    }
}


[Serializable]
public class PlayerAttackData
{
    public string playerId;        // ID único do jogador
    public string characterName;   // Nome do personagem que atacou
    public int monsterId;          // ID do monstro alvo
    public string monsterName;     // Nome do monstro alvo
}
[Serializable]
public class PositionData
{
    public float x;
    public float y;
    public float z;
}

// ==================== LOGIN/REGISTER ====================

[Serializable]
public class LoginResponseData
{
    public bool success;
    public string message;
    public int accountId;
    public CharacterData[] characters;
}

[Serializable]
public class RegisterResponseData
{
    public bool success;
    public string message;
}

[Serializable]
public class CreateCharacterResponseData
{
    public bool success;
    public string message;
    public CharacterData character;
}

[Serializable]
public class SelectCharacterResponseData
{
    public bool success;
    public string message;
    public CharacterData character;
    public string playerId;
    public PlayerStateData[] allPlayers;
    public MonsterStateData[] allMonsters;
    public InventoryData inventory; // 🆕 Inventário incluído
}

// ==================== PLAYER STATE ====================

[Serializable]
public class PlayerStateData
{
    public string playerId;
    public string characterName;
    public PositionData position;
    public string raca;
    public string classe;
    public int level;
    public int health;
    public int maxHealth;
    public int mana;
    public int maxMana;
    public int experience;
    public int statusPoints;
    public bool isMoving;
    public PositionData targetPosition;
    public bool inCombat;
    public int? targetMonsterId;
    public bool isDead;
}

[Serializable]
public class PlayerJoinedData
{
    public string playerId;
    public string characterName;
    public PositionData position;
    public string raca;
    public string classe;
    public int level;
    public int health;
    public int maxHealth;
}

// ==================== MONSTER STATE ====================

[Serializable]
public class MonsterStateData
{
    public int id;
    public int templateId;
    public string name;
    public int level;
    public int currentHealth;
    public int maxHealth;
    public PositionData position;
    public bool isAlive;
    public bool inCombat;
    public string targetPlayerId;
    public bool isMoving;
	public string prefabPath;
}

// ==================== WORLD STATE ====================

[Serializable]
public class WorldStateData
{
    public long timestamp;
    public PlayerStateData[] players;
    public MonsterStateData[] monsters;
}

// ==================== COMBAT ====================

[Serializable]
public class CombatResultData
{
    public string attackerId;
    public string targetId;
    public string attackerType;
    public string targetType;
    public int damage;
    public bool isCritical;
    public int remainingHealth;
    public bool targetDied;
    public int experienceGained;
    public bool leveledUp;
    public int newLevel;
}

[Serializable]
public class AttackStartedData
{
    public int monsterId;
    public string monsterName;
}

// ==================== LEVEL UP ====================

[Serializable]
public class LevelUpData
{
    public string playerId;
    public string characterName;
    public int newLevel;
    public int statusPoints;
    public int experience;
    public int requiredExp;
    public StatsData newStats;
}

[Serializable]
public class StatsData
{
    public int maxHealth;
    public int maxMana;
    public int attackPower;
    public int magicPower;
    public int defense;
    public float attackSpeed;
    public int strength;
    public int intelligence;
    public int dexterity;
    public int vitality;
}

[Serializable]
public class StatusPointAddedData
{
    public string playerId;
    public string characterName;
    public string stat;
    public int statusPoints;
    public StatsData newStats;
}

// ==================== DEATH/RESPAWN ====================

[Serializable]
public class PlayerDeathData
{
    public string playerId;
    public string characterName;
}

[Serializable]
public class PlayerRespawnData
{
    public string playerId;
    public string characterName;
    public PositionData position;
    public int health;
    public int maxHealth;
}

// ==================== ITEM SYSTEM ====================

[Serializable]
public class ItemTemplateData
{
    public int id;
    public string name;
    public string description;
    public string type; // consumable, equipment, material, currency
    public string subType; // potion, weapon, armor, accessory, drop, rare, gold
    public string slot; // weapon, armor, helmet, boots, gloves, ring, necklace
    public int maxStack;
    public string iconPath;
    public int requiredLevel;
    public string requiredClass;
    
    // Efeitos (consumíveis)
    public string effectType;
    public int effectValue;
    public string effectTarget;
    
    // Bônus (equipamentos)
    public int bonusStrength;
    public int bonusIntelligence;
    public int bonusDexterity;
    public int bonusVitality;
    public int bonusMaxHealth;
    public int bonusMaxMana;
    public int bonusAttackPower;
    public int bonusMagicPower;
    public int bonusDefense;
}

[Serializable]
public class ItemInstanceData
{
    public int instanceId;
    public int templateId;
    public int quantity;
    public int slot;
    public bool isEquipped;
    public ItemTemplateData template;
}

[Serializable]
public class InventoryData
{
    public int characterId;
    public int maxSlots;
    public int gold;
    public List<ItemInstanceData> items = new List<ItemInstanceData>();
    
    // Equipamentos
    public int? weaponId;
    public int? armorId;
    public int? helmetId;
    public int? bootsId;
    public int? glovesId;
    public int? ringId;
    public int? necklaceId;
}

[Serializable]
public class LootReceivedData
{
    public string playerId;
    public string characterName;
    public int gold;
    public List<LootedItemData> items = new List<LootedItemData>();
}

[Serializable]
public class LootedItemData
{
    public int itemId;
    public string itemName;
    public int quantity;
}

[Serializable]
public class ItemUsedData
{
    public string playerId;
    public int instanceId;
    public int health;
    public int maxHealth;
    public int mana;
    public int maxMana;
    public int remainingQuantity;
}

[Serializable]
public class ItemEquippedData
{
    public string playerId;
    public int instanceId;
    public StatsData newStats;
    public EquipmentData equipment;
}

[Serializable]
public class ItemDroppedData
{
    public string playerId;
    public int instanceId;
    public int quantity;
}

[Serializable]
public class EquipmentData
{
    public int? weaponId;
    public int? armorId;
    public int? helmetId;
    public int? bootsId;
    public int? glovesId;
    public int? ringId;
    public int? necklaceId;
}