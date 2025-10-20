using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Newtonsoft.Json;

/// <summary>
/// ✅ LoginUI CORRIGIDO - Remove warning CS0414
/// </summary>
public class LoginUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public Button loginButton;
    public Button registerButton;
    public TextMeshProUGUI statusText;

    [Header("Panels")]
    public GameObject loginPanel;
    public GameObject registerPanel;
    public TMP_InputField registerUsernameInput;
    public TMP_InputField registerPasswordInput;
    public TMP_InputField registerConfirmPasswordInput;
    public Button confirmRegisterButton;
    public Button backButton;

    [Header("Connection Status")]
    public GameObject connectionStatusPanel;
    public TextMeshProUGUI connectionStatusText;

    // ✅ REMOVIDO: isConnecting (não estava sendo usado)
    private bool isConnected = false;

    private void Start()
    {
        // Configura botões
        loginButton.onClick.AddListener(OnLoginClick);
        registerButton.onClick.AddListener(ShowRegisterPanel);
        confirmRegisterButton.onClick.AddListener(OnRegisterClick);
        backButton.onClick.AddListener(ShowLoginPanel);

        // Registra eventos de conexão
        if (ClientManager.Instance != null)
        {
            ClientManager.Instance.OnConnected += OnConnectedToServer;
            ClientManager.Instance.OnDisconnected += OnDisconnectedFromServer;
        }

        // Registra eventos de resposta
        if (MessageHandler.Instance != null)
        {
            MessageHandler.Instance.OnLoginResponse += HandleLoginResponse;
            MessageHandler.Instance.OnRegisterResponse += HandleRegisterResponse;
        }

        ShowLoginPanel();
        CheckConnectionStatus();
    }

    private void OnDestroy()
    {
        if (ClientManager.Instance != null)
        {
            ClientManager.Instance.OnConnected -= OnConnectedToServer;
            ClientManager.Instance.OnDisconnected -= OnDisconnectedFromServer;
        }

        if (MessageHandler.Instance != null)
        {
            MessageHandler.Instance.OnLoginResponse -= HandleLoginResponse;
            MessageHandler.Instance.OnRegisterResponse -= HandleRegisterResponse;
        }
    }

    // ===================================
    // CONTROLE DE CONEXÃO
    // ===================================

    private void CheckConnectionStatus()
    {
        if (ClientManager.Instance == null)
        {
            ShowConnectionStatus("❌ ClientManager não encontrado!", Color.red);
            DisableButtons();
            return;
        }

        if (ClientManager.Instance.IsConnected)
        {
            OnConnectedToServer();
        }
        else
        {
            ShowConnectionStatus("🔌 Conectando ao servidor...", Color.yellow);
            DisableButtons();
        }
    }

    private void OnConnectedToServer()
    {
        isConnected = true;
        ShowConnectionStatus("✅ Conectado ao servidor!", Color.green);
        EnableButtons();
        
        Invoke(nameof(HideConnectionStatus), 2f);
    }

    private void OnDisconnectedFromServer()
    {
        isConnected = false;
        ShowConnectionStatus("❌ Desconectado do servidor!", Color.red);
        DisableButtons();
        statusText.text = "Conexão perdida. Tentando reconectar...";
        statusText.color = Color.red;
    }

    private void ShowConnectionStatus(string message, Color color)
    {
        Debug.Log(message);
        
        if (connectionStatusPanel != null)
        {
            connectionStatusPanel.SetActive(true);
        }
        
        if (connectionStatusText != null)
        {
            connectionStatusText.text = message;
            connectionStatusText.color = color;
        }
    }

    private void HideConnectionStatus()
    {
        if (connectionStatusPanel != null)
        {
            connectionStatusPanel.SetActive(false);
        }
    }

    private void DisableButtons()
    {
        if (loginButton != null) loginButton.interactable = false;
        if (registerButton != null) registerButton.interactable = false;
        if (confirmRegisterButton != null) confirmRegisterButton.interactable = false;
    }

    private void EnableButtons()
    {
        if (loginButton != null) loginButton.interactable = true;
        if (registerButton != null) registerButton.interactable = true;
        if (confirmRegisterButton != null) confirmRegisterButton.interactable = true;
    }

    // ===================================
    // NAVEGAÇÃO DE PAINÉIS
    // ===================================

    private void ShowLoginPanel()
    {
        loginPanel.SetActive(true);
        registerPanel.SetActive(false);
        statusText.text = "";
    }

    private void ShowRegisterPanel()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(true);
        statusText.text = "";
    }

    // ===================================
    // LOGIN
    // ===================================

    private void OnLoginClick()
    {
        if (!isConnected)
        {
            statusText.text = "Aguarde a conexão com o servidor...";
            statusText.color = Color.yellow;
            return;
        }

        string username = usernameInput.text.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            statusText.text = "Por favor, preencha todos os campos.";
            statusText.color = Color.red;
            return;
        }

        if (username.Length < 3)
        {
            statusText.text = "Username deve ter pelo menos 3 caracteres.";
            statusText.color = Color.red;
            return;
        }

        if (password.Length < 6)
        {
            statusText.text = "Senha deve ter pelo menos 6 caracteres.";
            statusText.color = Color.red;
            return;
        }

        statusText.text = "Conectando...";
        statusText.color = Color.yellow;

        var message = new
        {
            type = "login",
            username = username,
            password = password
        };

        string json = JsonConvert.SerializeObject(message);
        ClientManager.Instance.SendMessage(json);

        loginButton.interactable = false;
        Invoke(nameof(EnableLoginButton), 3f);
    }

    private void EnableLoginButton()
    {
        if (loginButton != null)
            loginButton.interactable = true;
    }

    // ===================================
    // REGISTRO
    // ===================================

    private void OnRegisterClick()
    {
        if (!isConnected)
        {
            statusText.text = "Aguarde a conexão com o servidor...";
            statusText.color = Color.yellow;
            return;
        }

        string username = registerUsernameInput.text.Trim();
        string password = registerPasswordInput.text;
        string confirmPassword = registerConfirmPasswordInput.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            statusText.text = "Por favor, preencha todos os campos.";
            statusText.color = Color.red;
            return;
        }

        if (username.Length < 3)
        {
            statusText.text = "Username deve ter pelo menos 3 caracteres.";
            statusText.color = Color.red;
            return;
        }

        if (password.Length < 6)
        {
            statusText.text = "Senha deve ter pelo menos 6 caracteres.";
            statusText.color = Color.red;
            return;
        }

        if (password != confirmPassword)
        {
            statusText.text = "As senhas não coincidem.";
            statusText.color = Color.red;
            return;
        }

        statusText.text = "Criando conta...";
        statusText.color = Color.yellow;

        var message = new
        {
            type = "register",
            username = username,
            password = password
        };

        string json = JsonConvert.SerializeObject(message);
        ClientManager.Instance.SendMessage(json);

        confirmRegisterButton.interactable = false;
        Invoke(nameof(EnableRegisterButton), 3f);
    }

    private void EnableRegisterButton()
    {
        if (confirmRegisterButton != null)
            confirmRegisterButton.interactable = true;
    }

    // ===================================
    // RESPOSTAS DO SERVIDOR
    // ===================================

    private void HandleLoginResponse(LoginResponseData data)
    {
        loginButton.interactable = true;

        if (data.success)
        {
            statusText.text = "Login bem-sucedido!";
            statusText.color = Color.green;

            PlayerPrefs.SetInt("AccountId", data.accountId);
            PlayerPrefs.SetString("SavedUsername", usernameInput.text);
            PlayerPrefs.SetString("SavedPassword", passwordInput.text);
            PlayerPrefs.Save();
            
            Debug.Log($"✅ Login: Saved credentials and AccountId: {data.accountId}");

            Invoke(nameof(LoadCharacterSelect), 1f);
        }
        else
        {
            statusText.text = data.message ?? "Erro ao fazer login";
            statusText.color = Color.red;
        }
    }

    private void HandleRegisterResponse(RegisterResponseData data)
    {
        confirmRegisterButton.interactable = true;

        if (data.success)
        {
            statusText.text = "Conta criada! Faça login.";
            statusText.color = Color.green;
            Invoke(nameof(ShowLoginPanel), 2f);
        }
        else
        {
            statusText.text = data.message ?? "Erro ao criar conta";
            statusText.color = Color.red;
        }
    }

    private void LoadCharacterSelect()
    {
        SceneManager.LoadScene("CharacterSelect");
    }
}