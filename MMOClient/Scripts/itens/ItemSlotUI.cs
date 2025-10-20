using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

public class ItemSlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Elements")]
    public Image iconImage;
    public TextMeshProUGUI quantityText;
    public GameObject highlightBorder;
    
    [Header("Data")]
    public int slotIndex = -1;
    public ItemInstanceData itemData;
    
    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color hoverColor = new Color(1f, 1f, 0.5f);
    public Color selectedColor = Color.yellow;

    public event Action<ItemSlotUI> OnSlotClicked;

    private Image backgroundImage;
    private bool isHovered = false;
    private bool isSelected = false;

    private void Awake()
    {
        backgroundImage = GetComponent<Image>();
        
        if (highlightBorder != null)
            highlightBorder.SetActive(false);
    }

    /// <summary>
    /// Define o item no slot
    /// </summary>
    public void SetItem(ItemInstanceData item)
    {
        itemData = item;

        if (item == null || item.template == null)
        {
            Clear();
            return;
        }

        // Ativa ícone
        if (iconImage != null)
        {
            iconImage.enabled = true;
            iconImage.sprite = LoadIcon(item.template.iconPath);
        }

        // Quantidade (só mostra se > 1)
        if (quantityText != null)
        {
            if (item.quantity > 1)
            {
                quantityText.enabled = true;
                quantityText.text = item.quantity.ToString();
            }
            else
            {
                quantityText.enabled = false;
            }
        }

        // Borda para itens equipados
        if (highlightBorder != null && item.isEquipped)
        {
            highlightBorder.SetActive(true);
        }
    }

    /// <summary>
    /// Limpa o slot
    /// </summary>
    public void Clear()
    {
        itemData = null;

        if (iconImage != null)
            iconImage.enabled = false;

        if (quantityText != null)
            quantityText.enabled = false;

        if (highlightBorder != null)
            highlightBorder.SetActive(false);

        isSelected = false;
        UpdateVisuals();
    }

    /// <summary>
    /// Carrega sprite do ícone (placeholder por enquanto)
    /// </summary>
    private Sprite LoadIcon(string iconPath)
    {
        // TODO: Carregar de Resources ou AssetBundle
        // Por enquanto, usa sprite padrão
        
        if (string.IsNullOrEmpty(iconPath))
            return null;

        // Tenta carregar de Resources
        Sprite sprite = Resources.Load<Sprite>(iconPath);
        
        if (sprite == null)
        {
            // Placeholder baseado no tipo
            sprite = Resources.Load<Sprite>("Icons/default_item");
        }

        return sprite;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            isSelected = !isSelected;
            UpdateVisuals();
            OnSlotClicked?.Invoke(this);
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            // Clique direito - ação rápida
            QuickAction();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        UpdateVisuals();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (backgroundImage == null)
            return;

        if (isSelected)
        {
            backgroundImage.color = selectedColor;
        }
        else if (isHovered)
        {
            backgroundImage.color = hoverColor;
        }
        else
        {
            backgroundImage.color = normalColor;
        }
    }

    /// <summary>
    /// Ação rápida (usar/equipar com clique direito)
    /// </summary>
    private void QuickAction()
    {
        if (itemData == null || itemData.template == null)
            return;

        if (itemData.template.type == "consumable")
        {
            // Usa item consumível
            var message = new
            {
                type = "useItem",
                instanceId = itemData.instanceId
            };

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(message);
            ClientManager.Instance.SendMessage(json);
        }
        else if (itemData.template.type == "equipment")
        {
            if (itemData.isEquipped)
            {
                // Desequipa
                var message = new
                {
                    type = "unequipItem",
                    slot = itemData.template.slot
                };

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(message);
                ClientManager.Instance.SendMessage(json);
            }
            else
            {
                // Equipa
                var message = new
                {
                    type = "equipItem",
                    instanceId = itemData.instanceId
                };

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(message);
                ClientManager.Instance.SendMessage(json);
            }
        }
    }

    /// <summary>
    /// Deseleciona o slot
    /// </summary>
    public void Deselect()
    {
        isSelected = false;
        UpdateVisuals();
    }
}