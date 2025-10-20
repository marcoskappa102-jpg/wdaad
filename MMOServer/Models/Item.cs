namespace MMOServer.Models
{
    /// <summary>
    /// Template de item (dados imutáveis do JSON)
    /// </summary>
    [Serializable]
    public class ItemTemplate
    {
        public int id { get; set; }
        public string name { get; set; } = "";
        public string description { get; set; } = "";
        public string type { get; set; } = ""; // consumable, equipment, material, currency
        public string subType { get; set; } = ""; // potion, weapon, armor, accessory, drop, rare, gold
        public string slot { get; set; } = ""; // weapon, armor, helmet, boots, gloves, ring, necklace
        public int maxStack { get; set; } = 1;
        public int sellPrice { get; set; } = 0;
        public int buyPrice { get; set; } = 0;
        public int level { get; set; } = 1;
        public int requiredLevel { get; set; } = 1;
        public string requiredClass { get; set; } = ""; // "", ou "Guerreiro", "Mago", etc.
        
        // Efeitos (consumíveis)
        public string effectType { get; set; } = ""; // heal, buff, debuff
        public int effectValue { get; set; } = 0;
        public string effectTarget { get; set; } = ""; // health, mana
        public float cooldown { get; set; } = 0f;
        
        // Bônus (equipamentos)
        public int bonusStrength { get; set; } = 0;
        public int bonusIntelligence { get; set; } = 0;
        public int bonusDexterity { get; set; } = 0;
        public int bonusVitality { get; set; } = 0;
        public int bonusMaxHealth { get; set; } = 0;
        public int bonusMaxMana { get; set; } = 0;
        public int bonusAttackPower { get; set; } = 0;
        public int bonusMagicPower { get; set; } = 0;
        public int bonusDefense { get; set; } = 0;
        public float bonusAttackSpeed { get; set; } = 0f;
        
        // Visual
        public string iconPath { get; set; } = "";
    }

    /// <summary>
    /// Instância de item (item no inventário/equipamento)
    /// </summary>
    [Serializable]
    public class ItemInstance
    {
        public int instanceId { get; set; } // ID único da instância
        public int templateId { get; set; } // ID do template
        public int quantity { get; set; } = 1;
        public int slot { get; set; } = -1; // Slot no inventário (-1 = não equipado)
        public bool isEquipped { get; set; } = false;
        
        // Referência ao template (preenchido pelo servidor)
        [NonSerialized]
        public ItemTemplate? template;
    }

    /// <summary>
    /// Inventário do jogador
    /// </summary>
    [Serializable]
    public class Inventory
    {
        public int characterId { get; set; }
        public int maxSlots { get; set; } = 50; // Tamanho máximo do inventário
        public int gold { get; set; } = 0;
        public List<ItemInstance> items { get; set; } = new List<ItemInstance>();
        
        // Equipamentos
        public int? weaponId { get; set; }
        public int? armorId { get; set; }
        public int? helmetId { get; set; }
        public int? bootsId { get; set; }
        public int? glovesId { get; set; }
        public int? ringId { get; set; }
        public int? necklaceId { get; set; }
        
        /// <summary>
        /// Adiciona item ao inventário (empilha se possível)
        /// </summary>
        public bool AddItem(ItemInstance item, ItemTemplate template)
        {
            // Verifica se pode empilhar
            if (template.maxStack > 1)
            {
                var existingItem = items.FirstOrDefault(i => i.templateId == item.templateId && !i.isEquipped);
                
                if (existingItem != null && existingItem.quantity < template.maxStack)
                {
                    int spaceLeft = template.maxStack - existingItem.quantity;
                    int toAdd = Math.Min(spaceLeft, item.quantity);
                    
                    existingItem.quantity += toAdd;
                    item.quantity -= toAdd;
                    
                    if (item.quantity <= 0)
                        return true;
                }
            }
            
            // Verifica espaço
            if (items.Count >= maxSlots)
                return false;
            
            // Adiciona novo item
            item.slot = FindEmptySlot();
            items.Add(item);
            return true;
        }
        
        /// <summary>
        /// Remove item do inventário
        /// </summary>
public bool RemoveItem(int instanceId, int quantity = 1)
{
    var item = items.FirstOrDefault(i => i.instanceId == instanceId);
    
    if (item == null)
    {
        Console.WriteLine($"⚠️ Inventory.RemoveItem: Item {instanceId} not found");
        return false;
    }

    if (item.quantity < quantity)
    {
        Console.WriteLine($"⚠️ Inventory.RemoveItem: Not enough quantity (has {item.quantity}, need {quantity})");
        return false;
    }

    item.quantity -= quantity;
    
    if (item.quantity <= 0)
    {
        items.Remove(item);
        Console.WriteLine($"✅ Inventory.RemoveItem: Removed item {instanceId} completely");
    }
    else
    {
        Console.WriteLine($"✅ Inventory.RemoveItem: Reduced item {instanceId} quantity to {item.quantity}");
    }
    
    return true;
}
        
        /// <summary>
        /// Encontra slot vazio
        /// </summary>
        private int FindEmptySlot()
        {
            for (int i = 0; i < maxSlots; i++)
            {
                if (!items.Any(item => item.slot == i))
                    return i;
            }
            return items.Count;
        }
        
        /// <summary>
        /// Obtém item por ID de instância
        /// </summary>
        public ItemInstance? GetItem(int instanceId)
        {
            return items.FirstOrDefault(i => i.instanceId == instanceId);
        }
        
        /// <summary>
        /// Verifica se tem espaço
        /// </summary>
        public bool HasSpace()
        {
            return items.Count < maxSlots;
        }
    }

    /// <summary>
    /// Tabela de loot
    /// </summary>
    [Serializable]
    public class LootTable
    {
        public int monsterId { get; set; }
        public string monsterName { get; set; } = "";
        public GoldDrop guaranteedGold { get; set; } = new GoldDrop();
        public List<ItemDrop> drops { get; set; } = new List<ItemDrop>();
    }

    [Serializable]
    public class GoldDrop
    {
        public int min { get; set; } = 0;
        public int max { get; set; } = 0;
    }

    [Serializable]
    public class ItemDrop
    {
        public int itemId { get; set; }
        public string itemName { get; set; } = "";
        public float dropChance { get; set; } = 0f; // 0.0 a 1.0
        public int minQuantity { get; set; } = 1;
        public int maxQuantity { get; set; } = 1;
    }

    /// <summary>
    /// Resultado de loot
    /// </summary>
    [Serializable]
    public class LootResult
    {
        public int gold { get; set; } = 0;
        public List<LootedItem> items { get; set; } = new List<LootedItem>();
    }

    [Serializable]
    public class LootedItem
    {
        public int itemId { get; set; }
        public string itemName { get; set; } = "";
        public int quantity { get; set; } = 1;
    }
}