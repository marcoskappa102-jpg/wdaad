using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    [Header("Panels")]
    public GameObject inventoryPanel;
    public GameObject equipmentPanel;
    
    [Header("Inventory Grid")]
    public Transform inventoryGrid;
    public GameObject itemSlotPrefab;
    
    [Header("Equipment Slots")]
    public ItemSlotUI weaponSlot;
    public ItemSlotUI armorSlot;
    public ItemSlotUI helmetSlot;
    public ItemSlotUI bootsSlot;
    public ItemSlotUI glovesSlot;
    public ItemSlotUI ringSlot;
    public ItemSlotUI necklaceSlot;
    
    [Header("Info")]
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI weightText;
    public GameObject itemInfoPanel;
    public TextMeshProUGUI itemInfoName;
    public TextMeshProUGUI itemInfoDescription;
    public TextMeshProUGUI itemInfoStats;
    
    [Header("Buttons")]
    public Button useButton;
    public Button equipButton;
    public Button unequipButton;
    public Button dropButton;
    public Button closeButton;

    private InventoryData currentInventory;
    private List<ItemSlotUI> inventorySlots = new List<ItemSlotUI>();
    private ItemInstanceData selectedItem;
    private bool isVisible = false;

    private float lastPotionUseTime = -999f;
    private const float POTION_COOLDOWN = 1.0f;

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
        lastPotionUseTime = -999f;

        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
        
        if (useButton != null)
            useButton.onClick.AddListener(OnUseButtonClick);
        
        if (equipButton != null)
            equipButton.onClick.AddListener(OnEquipButtonClick);
        
        if (unequipButton != null)
            unequipButton.onClick.AddListener(OnUnequipButtonClick);
        
        if (dropButton != null)
            dropButton.onClick.AddListener(OnDropButtonClick);

        if (MessageHandler.Instance != null)
        {
            MessageHandler.Instance.OnInventoryReceived += HandleInventoryReceived;
            MessageHandler.Instance.OnLootReceived += HandleLootReceived;
            MessageHandler.Instance.OnItemUsed += HandleItemUsed;
            MessageHandler.Instance.OnItemEquipped += HandleItemEquipped;
            MessageHandler.Instance.OnItemUnequipped += HandleItemUnequipped;
            MessageHandler.Instance.OnItemDropped += HandleItemDropped;
        }

        CreateInventorySlots();
        Hide();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            Toggle();
        }
    }

    private void CreateInventorySlots()
    {
        if (itemSlotPrefab == null || inventoryGrid == null)
        {
            Debug.LogError("InventoryUI: Missing prefab or grid!");
            return;
        }

        for (int i = 0; i < 50; i++)
        {
            GameObject slotObj = Instantiate(itemSlotPrefab, inventoryGrid);
            ItemSlotUI slot = slotObj.GetComponent<ItemSlotUI>();
            
            if (slot != null)
            {
                slot.slotIndex = i;
                slot.OnSlotClicked += OnSlotClicked;
                inventorySlots.Add(slot);
            }
        }
    }

    public void Toggle()
    {
        if (isVisible)
            Hide();
        else
            Show();
    }

    public void Show()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(true);
        
        if (equipmentPanel != null)
            equipmentPanel.SetActive(true);
        
        isVisible = true;
        RequestInventoryUpdate();
    }

    public void Hide()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
        
        if (equipmentPanel != null)
            equipmentPanel.SetActive(false);
        
        if (itemInfoPanel != null)
            itemInfoPanel.SetActive(false);
        
        isVisible = false;
        selectedItem = null;
    }

    private void RequestInventoryUpdate()
    {
        var message = new
        {
            type = "getInventory"
        };

        string json = JsonConvert.SerializeObject(message);
        ClientManager.Instance.SendMessage(json);
    }

    private void HandleInventoryReceived(InventoryData inventory)
    {
        currentInventory = inventory;
        RefreshInventory();
    }

    private void RefreshInventory()
    {
        if (currentInventory == null)
            return;

        if (goldText != null)
            goldText.text = $"Gold: {currentInventory.gold}";

        if (weightText != null)
            weightText.text = $"Slots: {currentInventory.items.Count}/{currentInventory.maxSlots}";

        foreach (var slot in inventorySlots)
        {
            slot.Clear();
        }

        foreach (var item in currentInventory.items)
        {
            if (item.slot >= 0 && item.slot < inventorySlots.Count)
            {
                inventorySlots[item.slot].SetItem(item);
            }
        }

        UpdateEquipmentSlots();
    }

    private void UpdateEquipmentSlots()
    {
        if (currentInventory == null)
            return;

        weaponSlot?.Clear();
        armorSlot?.Clear();
        helmetSlot?.Clear();
        bootsSlot?.Clear();
        glovesSlot?.Clear();
        ringSlot?.Clear();
        necklaceSlot?.Clear();

        foreach (var item in currentInventory.items)
        {
            if (!item.isEquipped)
                continue;

            if (item.template == null)
                continue;

            switch (item.template.slot)
            {
                case "weapon": weaponSlot?.SetItem(item); break;
                case "armor": armorSlot?.SetItem(item); break;
                case "helmet": helmetSlot?.SetItem(item); break;
                case "boots": bootsSlot?.SetItem(item); break;
                case "gloves": glovesSlot?.SetItem(item); break;
                case "ring": ringSlot?.SetItem(item); break;
                case "necklace": necklaceSlot?.SetItem(item); break;
            }
        }
    }

    private void OnSlotClicked(ItemSlotUI slot)
    {
        if (slot.itemData == null)
        {
            selectedItem = null;
            HideItemInfo();
            return;
        }

        selectedItem = slot.itemData;
        ShowItemInfo(selectedItem);
    }

    private void ShowItemInfo(ItemInstanceData item)
    {
        if (itemInfoPanel == null || item.template == null)
            return;

        itemInfoPanel.SetActive(true);

        if (itemInfoName != null)
        {
            string color = GetRarityColor(item.template);
            itemInfoName.text = $"<color={color}>{item.template.name}</color>";
            
            if (item.quantity > 1)
                itemInfoName.text += $" x{item.quantity}";
        }

        if (itemInfoDescription != null)
            itemInfoDescription.text = item.template.description;

        if (itemInfoStats != null)
        {
            string stats = "";

            if (item.template.type == "equipment")
            {
                stats += $"<color=yellow>Equipamento</color>\n";
                stats += $"Slot: {TranslateSlot(item.template.slot)}\n";
                
                if (item.template.requiredLevel > 1)
                    stats += $"Nível: {item.template.requiredLevel}\n";
                
                if (!string.IsNullOrEmpty(item.template.requiredClass))
                    stats += $"Classe: {item.template.requiredClass}\n";

                stats += "\n<color=lime>Bônus:</color>\n";
                
                if (item.template.bonusStrength > 0)
                    stats += $"+{item.template.bonusStrength} STR\n";
                if (item.template.bonusIntelligence > 0)
                    stats += $"+{item.template.bonusIntelligence} INT\n";
                if (item.template.bonusDexterity > 0)
                    stats += $"+{item.template.bonusDexterity} DEX\n";
                if (item.template.bonusVitality > 0)
                    stats += $"+{item.template.bonusVitality} VIT\n";
                if (item.template.bonusMaxHealth > 0)
                    stats += $"+{item.template.bonusMaxHealth} HP\n";
                if (item.template.bonusMaxMana > 0)
                    stats += $"+{item.template.bonusMaxMana} MP\n";
                if (item.template.bonusAttackPower > 0)
                    stats += $"+{item.template.bonusAttackPower} ATK\n";
                if (item.template.bonusMagicPower > 0)
                    stats += $"+{item.template.bonusMagicPower} MATK\n";
                if (item.template.bonusDefense > 0)
                    stats += $"+{item.template.bonusDefense} DEF\n";
            }
            else if (item.template.type == "consumable")
            {
                stats += $"<color=cyan>Consumível</color>\n";
                stats += $"Efeito: {TranslateEffect(item.template.effectType)}\n";
                stats += $"Valor: {item.template.effectValue}\n";
                
                if (item.template.effectTarget == "health")
                    stats += $"Restaura: HP\n";
                else if (item.template.effectTarget == "mana")
                    stats += $"Restaura: MP\n";
            }

            itemInfoStats.text = stats;
        }

        UpdateButtons(item);
    }

    private void UpdateButtons(ItemInstanceData item)
    {
        if (item.template == null)
            return;

        bool isConsumable = item.template.type == "consumable";
        bool isEquipment = item.template.type == "equipment";
        bool isEquipped = item.isEquipped;

        if (useButton != null)
            useButton.gameObject.SetActive(isConsumable);

        if (equipButton != null)
            equipButton.gameObject.SetActive(isEquipment && !isEquipped);

        if (unequipButton != null)
            unequipButton.gameObject.SetActive(isEquipment && isEquipped);

        if (dropButton != null)
            dropButton.gameObject.SetActive(!isEquipped);
    }

    private void HideItemInfo()
    {
        if (itemInfoPanel != null)
            itemInfoPanel.SetActive(false);
    }

    private void OnUseButtonClick()
    {
        if (selectedItem == null || selectedItem.template == null)
            return;

        if (Time.time - lastPotionUseTime < POTION_COOLDOWN)
        {
            float remaining = POTION_COOLDOWN - (Time.time - lastPotionUseTime);
            if (UIManager.Instance != null)
            {
                UIManager.Instance.AddCombatLog($"<color=orange>⏳ Aguarde {remaining:F1}s antes de usar outra poção!</color>");
            }
            return;
        }

        if (selectedItem.template.type == "consumable" && WorldManager.Instance != null)
        {
            var localCharData = WorldManager.Instance.GetLocalCharacterData();
            
            if (localCharData != null)
            {
                if (selectedItem.template.effectTarget == "health" && 
                    localCharData.health >= localCharData.maxHealth)
                {
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.AddCombatLog("<color=yellow>💊 HP já está cheio!</color>");
                    }
                    return;
                }
                
                if (selectedItem.template.effectTarget == "mana" && 
                    localCharData.mana >= localCharData.maxMana)
                {
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.AddCombatLog("<color=cyan>💊 MP já está cheio!</color>");
                    }
                    return;
                }
            }
        }

        lastPotionUseTime = Time.time;

        var message = new
        {
            type = "useItem",
            instanceId = selectedItem.instanceId
        };

        string json = JsonConvert.SerializeObject(message);
        ClientManager.Instance.SendMessage(json);
    }

    private void OnEquipButtonClick()
    {
        if (selectedItem == null)
            return;

        var message = new
        {
            type = "equipItem",
            instanceId = selectedItem.instanceId
        };

        string json = JsonConvert.SerializeObject(message);
        ClientManager.Instance.SendMessage(json);
    }

    private void OnUnequipButtonClick()
    {
        if (selectedItem == null || selectedItem.template == null)
            return;

        var message = new
        {
            type = "unequipItem",
            slot = selectedItem.template.slot
        };

        string json = JsonConvert.SerializeObject(message);
        ClientManager.Instance.SendMessage(json);
    }

    private void OnDropButtonClick()
    {
        if (selectedItem == null)
        {
            Debug.LogWarning("⚠️ No item selected to drop");
            return;
        }

        if (selectedItem.isEquipped)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.AddCombatLog("<color=red>❌ Não pode dropar item equipado!</color>");
            }
            Debug.LogWarning($"⚠️ Cannot drop equipped item: {selectedItem.template?.name}");
            return;
        }

        // Mostra confirmação para itens valiosos (equipamentos)
        if (selectedItem.template != null && selectedItem.template.type == "equipment")
        {
            if (ConfirmDialogUI.Instance != null)
            {
                ConfirmDialogUI.Instance.ShowDropConfirmation(
                    selectedItem.template.name, 
                    selectedItem.quantity,
                    () => ExecuteItemDrop(selectedItem.instanceId)
                );
                return;
            }
        }

        // Dropa sem confirmação (consumíveis, materiais)
        ExecuteItemDrop(selectedItem.instanceId);
    }

    private void ExecuteItemDrop(int instanceId)
    {
        Debug.Log($"📤 Requesting drop for item {instanceId}");

        var message = new
        {
            type = "dropItem",
            instanceId = instanceId,
            quantity = 1
        };

        string json = JsonConvert.SerializeObject(message);
        ClientManager.Instance.SendMessage(json);

        selectedItem = null;
        HideItemInfo();
    }

    private void HandleLootReceived(LootReceivedData data)
    {
        string localPlayerId = ClientManager.Instance.PlayerId;
        
        if (data.playerId == localPlayerId)
        {
            string lootMsg = "";
            
            if (data.gold > 0)
                lootMsg += $"<color=yellow>+{data.gold} Gold</color>\n";
            
            foreach (var item in data.items)
            {
                lootMsg += $"<color=lime>+{item.quantity}x {item.itemName}</color>\n";
            }

            if (UIManager.Instance != null && !string.IsNullOrEmpty(lootMsg))
            {
                UIManager.Instance.AddCombatLog($"💰 Loot:\n{lootMsg}");
            }

            if (isVisible)
            {
                RequestInventoryUpdate();
            }
        }
    }

    private void HandleItemUsed(ItemUsedData data)
    {
        string localPlayerId = ClientManager.Instance.PlayerId;
        
        if (data.playerId == localPlayerId)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateHealthBar(data.health, data.maxHealth);
                UIManager.Instance.UpdateManaBar(data.mana, data.maxMana);
            }

            RequestInventoryUpdate();
        }
    }

    private void HandleItemEquipped(ItemEquippedData data)
    {
        string localPlayerId = ClientManager.Instance.PlayerId;
        
        if (data.playerId == localPlayerId)
        {
            RequestInventoryUpdate();
        }
    }

    private void HandleItemUnequipped(ItemEquippedData data)
    {
        HandleItemEquipped(data);
    }

    private void HandleItemDropped(ItemDroppedData data)
    {
        string localPlayerId = ClientManager.Instance.PlayerId;
        
        if (data.playerId == localPlayerId)
        {
            Debug.Log($"📤 Item {data.instanceId} dropped successfully (qty: {data.quantity})");
            
            if (UIManager.Instance != null)
            {
                UIManager.Instance.AddCombatLog($"<color=yellow>📤 Item dropado</color>");
            }

            if (isVisible)
            {
                RequestInventoryUpdate();
            }
        }
    }

    private string GetRarityColor(ItemTemplateData template)
    {
        if (template.type == "equipment")
        {
            if (template.requiredLevel >= 10)
                return "#FF00FF";
            else if (template.requiredLevel >= 5)
                return "#0088FF";
            else
                return "#00FF00";
        }
        
        return "#FFFFFF";
    }

    private string TranslateSlot(string slot)
    {
        return slot switch
        {
            "weapon" => "Arma",
            "armor" => "Armadura",
            "helmet" => "Elmo",
            "boots" => "Botas",
            "gloves" => "Luvas",
            "ring" => "Anel",
            "necklace" => "Colar",
            _ => slot
        };
    }

    private string TranslateEffect(string effect)
    {
        return effect switch
        {
            "heal" => "Cura",
            "buff" => "Buff",
            "debuff" => "Debuff",
            _ => effect
        };
    }

    private void OnDestroy()
    {
        if (MessageHandler.Instance != null)
        {
            MessageHandler.Instance.OnInventoryReceived -= HandleInventoryReceived;
            MessageHandler.Instance.OnLootReceived -= HandleLootReceived;
            MessageHandler.Instance.OnItemUsed -= HandleItemUsed;
            MessageHandler.Instance.OnItemEquipped -= HandleItemEquipped;
            MessageHandler.Instance.OnItemUnequipped -= HandleItemUnequipped;
            MessageHandler.Instance.OnItemDropped -= HandleItemDropped;
        }
    }
}