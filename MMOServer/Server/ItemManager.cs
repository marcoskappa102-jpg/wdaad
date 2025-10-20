using MMOServer.Models;
using Newtonsoft.Json;

namespace MMOServer.Server
{
    /// <summary>
    /// Gerenciador de itens e loot - VERSÃO CORRIGIDA
    /// </summary>
    public class ItemManager
    {
        private static ItemManager? instance;
        public static ItemManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new ItemManager();
                return instance;
            }
        }

        private Dictionary<int, ItemTemplate> itemTemplates = new Dictionary<int, ItemTemplate>();
        private Dictionary<int, LootTable> lootTables = new Dictionary<int, LootTable>();
        private int nextInstanceId = 1;
        private Random random = new Random();
        
        // ✅ NOVO: Cooldown de poções por jogador
        private Dictionary<string, DateTime> playerPotionCooldowns = new Dictionary<string, DateTime>();
        private const double POTION_COOLDOWN_SECONDS = 1.0; // 1 segundo entre poções

        public void Initialize()
        {
            Console.WriteLine("📦 ItemManager: Initializing...");
            
            LoadItemTemplates();
            LoadLootTables();
            LoadInstanceIdCounter();
            
            Console.WriteLine($"✅ ItemManager: Loaded {itemTemplates.Count} items and {lootTables.Count} loot tables");
        }

        // ==================== ITEM TEMPLATES ====================

        private void LoadItemTemplates()
        {
            string filePath = Path.Combine("Config", "items.json");

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"⚠️ {filePath} not found! Creating default...");
                CreateDefaultItemConfig();
                return;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                var config = JsonConvert.DeserializeObject<ItemConfig>(json);

                if (config?.items != null)
                {
                    foreach (var item in config.items)
                    {
                        itemTemplates[item.id] = item;
                    }
                    Console.WriteLine($"✅ Loaded {itemTemplates.Count} item templates");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading items: {ex.Message}");
            }
        }

        private void CreateDefaultItemConfig()
        {
            Console.WriteLine("Create Config/items.json manually with item definitions");
        }

        public ItemTemplate? GetItemTemplate(int itemId)
        {
            itemTemplates.TryGetValue(itemId, out var template);
            return template;
        }

        // ==================== LOOT TABLES ====================

        private void LoadLootTables()
        {
            string filePath = Path.Combine("Config", "loot_tables.json");

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"⚠️ {filePath} not found!");
                return;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                
                // ✅ CORREÇÃO: Validação antes de deserializar
                if (string.IsNullOrWhiteSpace(json))
                {
                    Console.WriteLine($"⚠️ {filePath} is empty!");
                    return;
                }

                var config = JsonConvert.DeserializeObject<LootConfig>(json);

                if (config?.lootTables != null)
                {
                    foreach (var table in config.lootTables)
                    {
                        // ✅ CORREÇÃO: Valida se table não é null
                        if (table == null)
                        {
                            Console.WriteLine($"⚠️ Skipping null loot table entry");
                            continue;
                        }

                        // ✅ CORREÇÃO: Inicializa listas se forem null
                        if (table.drops == null)
                        {
                            table.drops = new List<ItemDrop>();
                            Console.WriteLine($"⚠️ Loot table for monster {table.monsterId} has no drops");
                        }

                        if (table.guaranteedGold == null)
                        {
                            table.guaranteedGold = new GoldDrop { min = 0, max = 0 };
                        }

                        lootTables[table.monsterId] = table;
                    }
                    Console.WriteLine($"✅ Loaded {lootTables.Count} loot tables");
                }
                else
                {
                    Console.WriteLine($"⚠️ No loot tables found in {filePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading loot tables: {ex.Message}");
                Console.WriteLine($"   Stack: {ex.StackTrace}");
            }
        }

        public LootResult GenerateLoot(int monsterId)
        {
            var result = new LootResult();

            if (!lootTables.TryGetValue(monsterId, out var table))
            {
                Console.WriteLine($"⚠️ No loot table found for monster ID {monsterId}");
                result.gold = random.Next(5, 15);
                Console.WriteLine($"  Using default gold: {result.gold}");
                return result;
            }

            Console.WriteLine($"  Found loot table for {table.monsterName}");

            result.gold = random.Next(table.guaranteedGold.min, table.guaranteedGold.max + 1);
            Console.WriteLine($"  Gold: {result.gold} (range: {table.guaranteedGold.min}-{table.guaranteedGold.max})");

            Console.WriteLine($"  Rolling {table.drops.Count} possible drops:");
            foreach (var drop in table.drops)
            {
                double roll = random.NextDouble();
                Console.WriteLine($"    - {drop.itemName}: rolled {roll:F2} vs {drop.dropChance:F2} = {(roll <= drop.dropChance ? "DROP!" : "miss")}");
                
                if (roll <= drop.dropChance)
                {
                    int quantity = random.Next(drop.minQuantity, drop.maxQuantity + 1);
                    
                    result.items.Add(new LootedItem
                    {
                        itemId = drop.itemId,
                        itemName = drop.itemName,
                        quantity = quantity
                    });
                    
                    Console.WriteLine($"      → Dropped {quantity}x {drop.itemName}");
                }
            }

            return result;
        }

        // ==================== ITEM INSTANCES ====================

public ItemInstance CreateItemInstance(int templateId, int quantity = 1)
{
    var template = GetItemTemplate(templateId);
    
    if (template == null)
    {
        Console.WriteLine($"⚠️ Item template {templateId} not found!");
        return null!;
    }

    var instance = new ItemInstance
    {
        instanceId = nextInstanceId++,
        templateId = templateId,
        quantity = Math.Min(quantity, template.maxStack),
        template = template // ✅ ADICIONE ISTO
    };

    SaveInstanceIdCounter();
    return instance;
}

        // ==================== INVENTÁRIO ====================

        public Inventory LoadInventory(int characterId)
        {
            var inventory = DatabaseHandler.Instance.LoadInventory(characterId);
            
            foreach (var item in inventory.items)
            {
                if (item.template == null)
                {
                    item.template = GetItemTemplate(item.templateId);
                    
                    if (item.template == null)
                    {
                        Console.WriteLine($"⚠️ LoadInventory: Template {item.templateId} not found!");
                    }
                }
            }
            
            Console.WriteLine($"📦 Loaded inventory for character {characterId}: {inventory.items.Count} items, {inventory.gold} gold");
            return inventory;
        }

        public void SaveInventory(Inventory inventory)
        {
            DatabaseHandler.Instance.SaveInventory(inventory);
        }

        public bool AddItemToPlayer(string sessionId, int itemId, int quantity = 1)
        {
            var player = PlayerManager.Instance.GetPlayer(sessionId);
            
            if (player == null)
                return false;

            var template = GetItemTemplate(itemId);
            
            if (template == null)
                return false;

            var itemInstance = CreateItemInstance(itemId, quantity);
            
            if (itemInstance == null)
                return false;

            var inventory = LoadInventory(player.character.id);
            bool success = inventory.AddItem(itemInstance, template);
            
            if (success)
            {
                SaveInventory(inventory);
                Console.WriteLine($"📦 {player.character.nome} received {quantity}x {template.name}");
            }
            
            return success;
        }

public bool RemoveItemFromPlayer(string sessionId, int instanceId, int quantity = 1)
{
    try
    {
        var player = PlayerManager.Instance.GetPlayer(sessionId);
        
        if (player == null)
        {
            Console.WriteLine($"❌ RemoveItem: Player not found (sessionId: {sessionId})");
            return false;
        }

        Console.WriteLine($"📤 RemoveItem: Loading inventory for character {player.character.id}");
        
        var inventory = LoadInventory(player.character.id);
        
        if (inventory == null)
        {
            Console.WriteLine($"❌ RemoveItem: Inventory not found for character {player.character.id}");
            return false;
        }
        
        var item = inventory.GetItem(instanceId);
        
        if (item == null)
        {
            Console.WriteLine($"❌ RemoveItem: Item {instanceId} not found in inventory");
            Console.WriteLine($"   Available items: {string.Join(", ", inventory.items.Select(i => i.instanceId))}");
            return false;
        }

        if (item.isEquipped)
        {
            Console.WriteLine($"❌ RemoveItem: Cannot drop equipped item {instanceId}");
            return false;
        }

        if (item.quantity < quantity)
        {
            Console.WriteLine($"❌ RemoveItem: Not enough quantity (has {item.quantity}, requested {quantity})");
            return false;
        }

        bool success = inventory.RemoveItem(instanceId, quantity);
        
        if (success)
        {
            SaveInventory(inventory);
            
            string itemName = item.template?.name ?? "Unknown Item";
            Console.WriteLine($"📤 {player.character.nome} dropped {quantity}x {itemName} (ID: {instanceId})");
            return true;
        }
        else
        {
            Console.WriteLine($"❌ RemoveItem: Failed to remove item from inventory");
            return false;
        }
    }
    catch (System.Exception ex)
    {
        Console.WriteLine($"❌ Exception in RemoveItemFromPlayer: {ex.Message}");
        Console.WriteLine($"   StackTrace: {ex.StackTrace}");
        return false;
    }
}
        // ✅ CORREÇÃO PRINCIPAL: UseItem com validações corretas
        public string UseItem(string sessionId, int instanceId)
        {
            var player = PlayerManager.Instance.GetPlayer(sessionId);
            
            if (player == null)
            {
                Console.WriteLine($"❌ UseItem: Player not found");
                return "PLAYER_NOT_FOUND";
            }
            
            if (player.character.isDead)
            {
                Console.WriteLine($"❌ UseItem: Player is dead");
                return "PLAYER_DEAD";
            }

            // ✅ Verifica cooldown de poção
            if (playerPotionCooldowns.ContainsKey(sessionId))
            {
                var timeSinceLastUse = (DateTime.UtcNow - playerPotionCooldowns[sessionId]).TotalSeconds;
                
                if (timeSinceLastUse < POTION_COOLDOWN_SECONDS)
                {
                    double remaining = POTION_COOLDOWN_SECONDS - timeSinceLastUse;
                    Console.WriteLine($"⏳ {player.character.nome} potion on cooldown ({remaining:F1}s remaining)");
                    return "ON_COOLDOWN";
                }
            }

            var inventory = LoadInventory(player.character.id);
            var item = inventory.GetItem(instanceId);
            
            if (item == null)
            {
                Console.WriteLine($"❌ UseItem: Item {instanceId} not found in inventory");
                return "ITEM_NOT_FOUND";
            }

            if (item.template == null)
            {
                item.template = GetItemTemplate(item.templateId);
            }

            var template = item.template;
            
            if (template == null)
            {
                Console.WriteLine($"❌ Template not found for item {item.templateId}");
                return "TEMPLATE_NOT_FOUND";
            }

            if (template.type != "consumable")
            {
                Console.WriteLine($"❌ UseItem: Item {template.name} is not consumable (type: {template.type})");
                return "NOT_CONSUMABLE";
            }

            // ✅ NOVA VALIDAÇÃO: Verifica se pode usar ANTES de consumir
            if (template.effectType == "heal")
            {
                if (template.effectTarget == "health")
                {
                    if (player.character.health >= player.character.maxHealth)
                    {
                        Console.WriteLine($"⚠️ {player.character.nome} tried to use {template.name} but HP is already full ({player.character.health}/{player.character.maxHealth})");
                        return "HP_FULL";
                    }
                }
                else if (template.effectTarget == "mana")
                {
                    if (player.character.mana >= player.character.maxMana)
                    {
                        Console.WriteLine($"⚠️ {player.character.nome} tried to use {template.name} but MP is already full ({player.character.mana}/{player.character.maxMana})");
                        return "MP_FULL";
                    }
                }
            }

            // Aplica efeito
            bool effectApplied = false;
            int oldValue = 0;
            int newValue = 0;
            
            if (template.effectType == "heal")
            {
                if (template.effectTarget == "health")
                {
                    oldValue = player.character.health;
                    player.character.health = Math.Min(player.character.health + template.effectValue, player.character.maxHealth);
                    newValue = player.character.health;
                    int healed = newValue - oldValue;
                    
                    Console.WriteLine($"💊 {player.character.nome} healed {healed} HP with {template.name} ({oldValue} -> {newValue})");
                    effectApplied = true;
                }
                else if (template.effectTarget == "mana")
                {
                    oldValue = player.character.mana;
                    player.character.mana = Math.Min(player.character.mana + template.effectValue, player.character.maxMana);
                    newValue = player.character.mana;
                    int restored = newValue - oldValue;
                    
                    Console.WriteLine($"💊 {player.character.nome} restored {restored} MP with {template.name} ({oldValue} -> {newValue})");
                    effectApplied = true;
                }
            }

            if (effectApplied)
            {
                // ✅ Atualiza cooldown
                playerPotionCooldowns[sessionId] = DateTime.UtcNow;
                
                // Remove item
                inventory.RemoveItem(instanceId, 1);
                SaveInventory(inventory);
                DatabaseHandler.Instance.UpdateCharacter(player.character);
                
				WorldManager.Instance.BroadcastPlayerStatsUpdate(player);
                return "SUCCESS";
            }

            return "NO_EFFECT";
        }

        public bool EquipItem(string sessionId, int instanceId)
        {
            var player = PlayerManager.Instance.GetPlayer(sessionId);
            
            if (player == null)
                return false;

            var inventory = LoadInventory(player.character.id);
            var item = inventory.GetItem(instanceId);
            
            if (item == null)
                return false;

            if (item.template == null)
            {
                item.template = GetItemTemplate(item.templateId);
            }

            var template = item.template;
            
            if (template == null)
            {
                Console.WriteLine($"❌ Template not found for item {item.templateId}");
                return false;
            }

            if (template.type != "equipment")
                return false;

            if (player.character.level < template.requiredLevel)
                return false;

            if (!string.IsNullOrEmpty(template.requiredClass) && template.requiredClass != player.character.classe)
                return false;

            string slot = template.slot;
            int? oldItemId = slot switch
            {
                "weapon" => inventory.weaponId,
                "armor" => inventory.armorId,
                "helmet" => inventory.helmetId,
                "boots" => inventory.bootsId,
                "gloves" => inventory.glovesId,
                "ring" => inventory.ringId,
                "necklace" => inventory.necklaceId,
                _ => null
            };

            if (oldItemId.HasValue)
            {
                var oldItem = inventory.GetItem(oldItemId.Value);
                if (oldItem != null)
                {            
					if (!inventory.HasSpace())
					{
						Console.WriteLine($"❌ No space to unequip old item");
						return false;
					}
                    oldItem.isEquipped = false;
                }
            }

            item.isEquipped = true;
            
            switch (slot)
            {
                case "weapon": inventory.weaponId = instanceId; break;
                case "armor": inventory.armorId = instanceId; break;
                case "helmet": inventory.helmetId = instanceId; break;
                case "boots": inventory.bootsId = instanceId; break;
                case "gloves": inventory.glovesId = instanceId; break;
                case "ring": inventory.ringId = instanceId; break;
                case "necklace": inventory.necklaceId = instanceId; break;
            }

            RecalculatePlayerStats(player, inventory);
            
            SaveInventory(inventory);
            DatabaseHandler.Instance.UpdateCharacter(player.character);
            
            Console.WriteLine($"⚔️ {player.character.nome} equipped {template.name}");
            return true;
        }

// Substituir o método UnequipItem no ItemManager.cs

public bool UnequipItem(string sessionId, string slot)
{
    try
    {
        var player = PlayerManager.Instance.GetPlayer(sessionId);
        
        if (player == null)
        {
            Console.WriteLine($"❌ UnequipItem: Player not found: {sessionId}");
            return false;
        }

        var inventory = LoadInventory(player.character.id);
        
        // Determina qual item está equipado no slot
        int? itemId = slot switch
        {
            "weapon" => inventory.weaponId,
            "armor" => inventory.armorId,
            "helmet" => inventory.helmetId,
            "boots" => inventory.bootsId,
            "gloves" => inventory.glovesId,
            "ring" => inventory.ringId,
            "necklace" => inventory.necklaceId,
            _ => null
        };

        if (!itemId.HasValue)
        {
            Console.WriteLine($"⚠️ UnequipItem: No item equipped in slot '{slot}' for {player.character.nome}");
            return false;
        }

        var item = inventory.GetItem(itemId.Value);
        
        if (item == null)
        {
            Console.WriteLine($"❌ UnequipItem: Item {itemId.Value} not found in inventory");
            
            // Limpa o slot corrompido
            switch (slot)
            {
                case "weapon": inventory.weaponId = null; break;
                case "armor": inventory.armorId = null; break;
                case "helmet": inventory.helmetId = null; break;
                case "boots": inventory.bootsId = null; break;
                case "gloves": inventory.glovesId = null; break;
                case "ring": inventory.ringId = null; break;
                case "necklace": inventory.necklaceId = null; break;
            }
            
            SaveInventory(inventory);
            return false;
        }

        // Desequipa o item
        item.isEquipped = false;
        
        // Remove do slot de equipamento
        switch (slot)
        {
            case "weapon": inventory.weaponId = null; break;
            case "armor": inventory.armorId = null; break;
            case "helmet": inventory.helmetId = null; break;
            case "boots": inventory.bootsId = null; break;
            case "gloves": inventory.glovesId = null; break;
            case "ring": inventory.ringId = null; break;
            case "necklace": inventory.necklaceId = null; break;
            default:
                Console.WriteLine($"❌ UnequipItem: Invalid slot '{slot}'");
                return false;
        }

        // Recalcula stats do player
        RecalculatePlayerStats(player, inventory);
        
        // Salva alterações
        SaveInventory(inventory);
        DatabaseHandler.Instance.UpdateCharacter(player.character);
        
        Console.WriteLine($"⚔️ {player.character.nome} unequipped {item.template?.name ?? "item"} from {slot}");
        return true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Exception in UnequipItem: {ex.Message}");
        Console.WriteLine($"   StackTrace: {ex.StackTrace}");
        return false;
    }
}

        private void RecalculatePlayerStats(Player player, Inventory inventory)
        {
            player.character.RecalculateStats();

            var equippedIds = new[] 
            { 
                inventory.weaponId, 
                inventory.armorId, 
                inventory.helmetId, 
                inventory.bootsId, 
                inventory.glovesId, 
                inventory.ringId, 
                inventory.necklaceId 
            };

            foreach (var itemId in equippedIds)
            {
                if (!itemId.HasValue)
                    continue;

                var item = inventory.GetItem(itemId.Value);
                
                if (item?.template == null)
                    continue;

                var t = item.template;

                player.character.strength += t.bonusStrength;
                player.character.intelligence += t.bonusIntelligence;
                player.character.dexterity += t.bonusDexterity;
                player.character.vitality += t.bonusVitality;

                player.character.maxHealth += t.bonusMaxHealth;
                player.character.maxMana += t.bonusMaxMana;
                player.character.attackPower += t.bonusAttackPower;
                player.character.magicPower += t.bonusMagicPower;
                player.character.defense += t.bonusDefense;
                player.character.attackSpeed += t.bonusAttackSpeed;
            }

            player.character.RecalculateStats();

            Console.WriteLine($"📊 {player.character.nome} stats recalculated: ATK={player.character.attackPower} DEF={player.character.defense} HP={player.character.maxHealth}");
        }

        // ==================== PERSISTÊNCIA ====================

        private void LoadInstanceIdCounter()
        {
            nextInstanceId = DatabaseHandler.Instance.GetNextItemInstanceId();
        }

        private void SaveInstanceIdCounter()
        {
            DatabaseHandler.Instance.SaveNextItemInstanceId(nextInstanceId);
        }

        public void ReloadConfigs()
        {
            Console.WriteLine("🔄 Reloading item configurations...");
            itemTemplates.Clear();
            lootTables.Clear();
            LoadItemTemplates();
            LoadLootTables();
            Console.WriteLine("✅ Item configurations reloaded!");
        }
    }

    [Serializable]
    public class ItemConfig
    {
        public List<ItemTemplate> items { get; set; } = new List<ItemTemplate>();
    }

    [Serializable]
    public class LootConfig
    {
        public List<LootTable> lootTables { get; set; } = new List<LootTable>();
    }
}