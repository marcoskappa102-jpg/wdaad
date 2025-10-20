using WebSocketSharp;
using WebSocketSharp.Server;
using MMOServer.Utils;
using MMOServer.Models;
using System.Collections.Concurrent;

namespace MMOServer.Server
{
    public class GameServer : WebSocketBehavior
    {
        private string? playerId;
        private static ConcurrentDictionary<string, GameServer> activeConnections = new ConcurrentDictionary<string, GameServer>();

        protected override void OnOpen()
        {
            Console.WriteLine($"Client connected: {ID}");
            activeConnections.TryAdd(ID, this);
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            try
            {
                var response = MessageHandler.HandleMessage(e.Data, ID);
                
                if (response != null)
                {
                    if (response.StartsWith("BROADCAST:"))
                    {
                        if (response.Contains("|||"))
                        {
                            var parts = response.Split(new[] { "|||" }, StringSplitOptions.None);
                            var broadcastMsg = parts[0].Substring(10);
                            BroadcastToAll(broadcastMsg);
                            
                            if (parts.Length > 1)
                            {
                                var individualMsg = parts[1];
                                try
                                {
                                    var jsonObj = Newtonsoft.Json.Linq.JObject.Parse(individualMsg);
                                    var playerIdFromJson = jsonObj["playerId"]?.ToString();
                                    if (!string.IsNullOrEmpty(playerIdFromJson))
                                    {
                                        playerId = playerIdFromJson;
                                    }
                                }
                                catch { }
                                
                                Send(individualMsg);
                            }
                        }
                        else
                        {
                            var message = response.Substring(10);
                            BroadcastToAll(message);
                        }
                    }
                    else
                    {
                        Send(response);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling message: {ex.Message}");
                Send($"{{\"type\":\"error\",\"message\":\"{ex.Message}\"}}");
            }
        }

        protected override void OnClose(CloseEventArgs e)
        {
            Console.WriteLine($"Client disconnected: {ID}");
            activeConnections.TryRemove(ID, out _);
            
            if (!string.IsNullOrEmpty(playerId))
            {
                PlayerManager.Instance.RemovePlayer(playerId);
                
                var updateMessage = new
                {
                    type = "playerDisconnected",
                    playerId = playerId
                };
                
                BroadcastToAll(Newtonsoft.Json.JsonConvert.SerializeObject(updateMessage));
            }
        }

        protected override void OnError(WebSocketSharp.ErrorEventArgs e)
        {
            Console.WriteLine($"Error: {e.Message}");
        }

        // Método estático para broadcast global
        public static void BroadcastToAll(string message)
        {
            var connectionsToRemove = new List<string>();

            foreach (var kvp in activeConnections)
            {
                try
                {
                    // Verifica se a conexão ainda está aberta antes de enviar
                    if (kvp.Value.State == WebSocketState.Open)
                    {
                        kvp.Value.Send(message);
                    }
                    else
                    {
                        // Marca para remoção se não estiver aberta
                        connectionsToRemove.Add(kvp.Key);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error broadcasting to client {kvp.Key}: {ex.Message}");
                    connectionsToRemove.Add(kvp.Key);
                }
            }

            // Remove conexões mortas
            foreach (var id in connectionsToRemove)
            {
                activeConnections.TryRemove(id, out _);
            }
        }
    }
}