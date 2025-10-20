using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// Dialog de confirmação reutilizável
/// Coloque em: mmoclient/Scripts/UI/ConfirmDialogUI.cs
/// </summary>
public class ConfirmDialogUI : MonoBehaviour
{
    public static ConfirmDialogUI Instance { get; private set; }

    [Header("UI Elements")]
    public GameObject dialogPanel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI messageText;
    public Button confirmButton;
    public Button cancelButton;
    public TextMeshProUGUI confirmButtonText;
    public TextMeshProUGUI cancelButtonText;

    private Action onConfirmCallback;
    private Action onCancelCallback;

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
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClick);
        
        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelClick);

        Hide();
    }

    /// <summary>
    /// Mostra dialog de confirmação
    /// </summary>
    public void Show(string title, string message, Action onConfirm, Action onCancel = null, string confirmText = "Confirmar", string cancelText = "Cancelar")
    {
        if (dialogPanel == null)
        {
            Debug.LogError("ConfirmDialog: dialogPanel is null!");
            return;
        }

        dialogPanel.SetActive(true);

        if (titleText != null)
            titleText.text = title;

        if (messageText != null)
            messageText.text = message;

        if (confirmButtonText != null)
            confirmButtonText.text = confirmText;

        if (cancelButtonText != null)
            cancelButtonText.text = cancelText;

        onConfirmCallback = onConfirm;
        onCancelCallback = onCancel;
    }

    /// <summary>
    /// Esconde o dialog
    /// </summary>
    public void Hide()
    {
        if (dialogPanel != null)
            dialogPanel.SetActive(false);

        onConfirmCallback = null;
        onCancelCallback = null;
    }

    private void OnConfirmClick()
    {
        onConfirmCallback?.Invoke();
        Hide();
    }

    private void OnCancelClick()
    {
        onCancelCallback?.Invoke();
        Hide();
    }

    /// <summary>
    /// Atalho para confirmar drop de item
    /// </summary>
    public void ShowDropConfirmation(string itemName, int quantity, Action onConfirm)
    {
        string message = quantity > 1 
            ? $"Dropar {quantity}x {itemName}?\n\nEste item será perdido permanentemente!"
            : $"Dropar {itemName}?\n\nEste item será perdido permanentemente!";

        Show("⚠️ Confirmar Drop", message, onConfirm, null, "Dropar", "Cancelar");
    }
}