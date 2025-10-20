using UnityEngine;
using NativeWebSocket;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;


/// <summary>
/// ✅ ClientManager MELHORADO
/// - Reconexão automática
/// - Configuration via ScriptableObject
/// - Health checks
/// - Melhor tratamento de erros
/// </summary>
public class ClientManager : MonoBehaviour
{
    public static ClientManager Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private NetworkConfig networkConfig;
    
    private WebSocket websocket;
    public bool IsConnected => websocket != null && websocket.State == WebSocketState.Open;
    
    public string PlayerId { get; private set; }
    public event Action<string> OnMessageReceived;
    public event Action OnConnected;
    public event Action OnDisconnected;

    // ===================================
    // RECONEXÃO AUTOMÁTICA
    // ===================================
    
    private bool isQuitting = false;
    private bool shouldReconnect = true;
    private int reconnectAttempts = 0;
    private const int MAX_RECONNECT_ATTEMPTS = 5;
    private const float RECONNECT_DELAY = 3f;
    
    // Health check
    private float lastPingTime = 0f;
    private const float PING_INTERVAL = 30f;
    
    // Message queue para envios durante desconexão
    private readonly System.Collections.Generic.Queue<string> messageQueue = new();
    private const int MAX_QUEUE_SIZE = 100;

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
        // Carrega configuração
        if (networkConfig == null)
        {
            Debug.LogError("❌ NetworkConfig not assigned! Create one: Assets > Create > Network Config");
            return;
        }

        // Conecta automaticamente se configurado
        if (networkConfig.autoConnect)
        {
            Connect();
        }
    }

    // ===================================
    // CONEXÃO COM RETRY
    // ===================================

    public async void Connect(string customUrl = null)
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            Debug.LogWarning("⚠️ Already connected!");
            return;
        }

        string url = customUrl ?? networkConfig.GetServerUrl();
        
        try
        {
            Debug.Log($"🔌 Connecting to {url}...");
            
            websocket = new WebSocket(url);

            websocket.OnOpen += () =>
            {
                Debug.Log("✅ Connected to server!");
                reconnectAttempts = 0;
                lastPingTime = Time.time;
                OnConnected?.Invoke();
                
                // Envia mensagens enfileiradas
                ProcessMessageQueue();
            };

            websocket.OnError += (e) =>
            {
                Debug.LogError($"❌ WebSocket Error: {e}");
            };

            websocket.OnClose += (e) =>
            {
                Debug.Log($"🔌 Connection closed: {e}");
                OnDisconnected?.Invoke();
                
                // Reconecta automaticamente se não foi logout intencional
                if (shouldReconnect && !isQuitting)
                {
                    TryReconnect();
                }
            };

            websocket.OnMessage += (bytes) =>
            {
                var message = System.Text.Encoding.UTF8.GetString(bytes);
                
                // Log apenas primeiros caracteres para não poluir
                int previewLength = Math.Min(100, message.Length);
                Debug.Log($"📨 Received: {message.Substring(0, previewLength)}{(message.Length > 100 ? "..." : "")}");
                
                OnMessageReceived?.Invoke(message);
            };

            await websocket.Connect();
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Connection failed: {e.Message}");
            
            if (shouldReconnect && !isQuitting)
            {
                TryReconnect();
            }
        }
    }

    // ===================================
    // RECONEXÃO AUTOMÁTICA
    // ===================================

    private async void TryReconnect()
    {
        if (reconnectAttempts >= MAX_RECONNECT_ATTEMPTS)
        {
            Debug.LogError($"❌ Max reconnection attempts ({MAX_RECONNECT_ATTEMPTS}) reached. Giving up.");
            ShowConnectionError("Não foi possível conectar ao servidor. Verifique sua conexão.");
            return;
        }

        reconnectAttempts++;
        Debug.Log($"🔄 Reconnection attempt {reconnectAttempts}/{MAX_RECONNECT_ATTEMPTS} in {RECONNECT_DELAY}s...");
        
        await Task.Delay((int)(RECONNECT_DELAY * 1000));
        
        if (!isQuitting)
        {
            Connect();
        }
    }

    // ===================================
    // ENVIO DE MENSAGENS COM QUEUE
    // ===================================

    /// <summary>
    /// Envia mensagem ao servidor
    /// Usa 'new' para esconder método herdado do MonoBehaviour
    /// </summary>
// Adicione este método no ClientManager.cs
// Substitua o método SendMessage existente por este:

/// <summary>
/// Envia mensagem ao servidor COM LOG para debug
/// </summary>
public new void SendMessage(string message)
{
    // 🔍 DEBUG: Mostra o que está sendo enviado
    try
    {
        var json = Newtonsoft.Json.Linq.JObject.Parse(message);
        var type = json["type"]?.ToString() ?? "NO_TYPE";
        Debug.Log($"📤 SENDING: type='{type}'");
        Debug.Log($"   Full message: {message.Substring(0, Math.Min(200, message.Length))}...");
    }
    catch
    {
        Debug.LogWarning($"📤 SENDING (invalid JSON): {message.Substring(0, Math.Min(100, message.Length))}");
    }

    if (websocket == null)
    {
        Debug.LogWarning("⚠️ WebSocket is null!");
        EnqueueMessage(message);
        return;
    }

    if (isQuitting)
    {
        Debug.LogWarning("⚠️ Application is quitting, message not sent");
        return;
    }

    if (websocket.State != WebSocketState.Open)
    {
        Debug.LogWarning($"⚠️ WebSocket not open (State: {websocket.State}), queueing message");
        EnqueueMessage(message);
        return;
    }

    try
    {
        _ = websocket.SendText(message);
    }
    catch (Exception e)
    {
        Debug.LogError($"❌ Error sending message: {e.Message}");
        EnqueueMessage(message);
    }
}

    private void EnqueueMessage(string message)
    {
        if (messageQueue.Count >= MAX_QUEUE_SIZE)
        {
            Debug.LogWarning($"⚠️ Message queue full ({MAX_QUEUE_SIZE}), dropping oldest message");
            messageQueue.Dequeue();
        }
        
        messageQueue.Enqueue(message);
        Debug.Log($"📦 Message queued ({messageQueue.Count} in queue)");
    }

    private void ProcessMessageQueue()
    {
        if (messageQueue.Count == 0)
            return;

        Debug.Log($"📤 Processing {messageQueue.Count} queued messages...");
        
        while (messageQueue.Count > 0)
        {
            string message = messageQueue.Dequeue();
            SendMessage(message);
        }
    }

    // ===================================
    // HEALTH CHECK / PING
    // ===================================

    private void Update()
    {
        #if !UNITY_WEBGL || UNITY_EDITOR
        if (websocket != null && !isQuitting)
        {
            try
            {
                websocket.DispatchMessageQueue();
            }
            catch (Exception e)
            {
                if (!isQuitting)
                {
                    Debug.LogError($"❌ Error dispatching messages: {e.Message}");
                }
            }
        }
        #endif

        // Health check periódico
        if (IsConnected && Time.time - lastPingTime >= PING_INTERVAL)
        {
            SendPing();
            lastPingTime = Time.time;
        }
    }

    private void SendPing()
    {
        var pingMessage = new
        {
            type = "ping",
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        SendMessage(Newtonsoft.Json.JsonConvert.SerializeObject(pingMessage));
    }

    // ===================================
    // DESCONEXÃO LIMPA
    // ===================================

    public void Disconnect()
    {
        shouldReconnect = false;
        CloseConnection();
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
        shouldReconnect = false;
        CloseConnection();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            CloseConnection();
        }
    }

    private void CloseConnection()
    {
        if (websocket == null)
            return;

        try
        {
            if (websocket.State == WebSocketState.Open || websocket.State == WebSocketState.Connecting)
            {
                Debug.Log("🔌 Closing WebSocket connection...");
                
                Task.Run(async () =>
                {
                    try
                    {
                        await websocket.Close();
                        Debug.Log("✅ WebSocket closed successfully");
                    }
                    catch (Exception e)
                    {
                        Debug.Log($"⚠️ Error closing WebSocket (ignored): {e.Message}");
                    }
                }).Wait(1000);
            }
        }
        catch (Exception e)
        {
            Debug.Log($"⚠️ Exception during close (ignored): {e.Message}");
        }
        finally
        {
            websocket = null;
        }
    }

    // ===================================
    // HELPERS
    // ===================================

    public void SetPlayerId(string id)
    {
        PlayerId = id;
        Debug.Log($"🆔 Player ID set: {id.Substring(0, Math.Min(8, id.Length))}...");
    }

    public bool IsHealthy()
    {
        return websocket != null && 
               websocket.State == WebSocketState.Open && 
               !isQuitting;
    }

    public int GetQueuedMessageCount()
    {
        return messageQueue.Count;
    }

    private void ShowConnectionError(string message)
    {
        // Mostra popup de erro (integrar com seu sistema de UI)
        if (UIManager.Instance != null)
        {
            UIManager.Instance.AddCombatLog($"<color=red>❌ {message}</color>");
        }
    }
}

// ===================================
// SCRIPTABLE OBJECT DE CONFIGURAÇÃO
// ===================================

[CreateAssetMenu(fileName = "NetworkConfig", menuName = "MMO/Network Config")]
public class NetworkConfig : ScriptableObject
{
    [Header("Server Settings")]
    public string serverIP = "localhost";
    public int serverPort = 8080;
    public string serverPath = "/game";
    public bool useSSL = false;

    [Header("Connection")]
    public bool autoConnect = true;
    public int maxReconnectAttempts = 5;
    public float reconnectDelay = 3f;
    public float pingInterval = 30f;

    [Header("Performance")]
    public int maxQueueSize = 100;
    public int messageBufferSize = 8192;

    public string GetServerUrl()
    {
        string protocol = useSSL ? "wss" : "ws";
        return $"{protocol}://{serverIP}:{serverPort}{serverPath}";
    }
}