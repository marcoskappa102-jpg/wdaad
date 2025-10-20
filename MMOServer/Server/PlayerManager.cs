using MMOServer.Models;
using System.Collections.Concurrent;

namespace MMOServer.Server
{
    public class PlayerManager
    {
        private static PlayerManager? instance;
        public static PlayerManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new PlayerManager();
                return instance;
            }
        }

        private ConcurrentDictionary<string, Player> activePlayers = new ConcurrentDictionary<string, Player>();
        
        private const float MOVE_SPEED = 5.0f; // 5 unidades/segundo
        private const float STOP_THRESHOLD = 0.2f; // Para quando chegar perto
        private const float CHARACTER_HEIGHT_OFFSET = 0f; // Altura do personagem acima do chão

public bool AddPlayer(string sessionId, Player player)
{
    // ✅ GARANTIA: Ajusta posição inicial ao terreno (caso venha errado do banco)
    TerrainHeightmap.Instance.ClampToGround(player.position, CHARACTER_HEIGHT_OFFSET);
    
    // ✅ Sincroniza com character.position
    player.character.position.x = player.position.x;
    player.character.position.y = player.position.y;
    player.character.position.z = player.position.z;
    
    Console.WriteLine($"👤 {player.character.nome} spawned at ({player.position.x:F1}, {player.position.y:F1}, {player.position.z:F1})");
    
    return activePlayers.TryAdd(sessionId, player);
}

        public void RemovePlayer(string sessionId)
        {
            if (activePlayers.TryRemove(sessionId, out var player))
            {
                try
                {
                    DatabaseHandler.Instance.UpdateCharacter(player.character);
                    Console.WriteLine($"✅ Saved {player.character.nome} on disconnect");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error saving character: {ex.Message}");
                }
            }
        }

        public Player? GetPlayer(string sessionId)
        {
            activePlayers.TryGetValue(sessionId, out var player);
            return player;
        }

        public List<Player> GetAllPlayers()
        {
            return activePlayers.Values.ToList();
        }

        public bool SetPlayerTarget(string sessionId, Position targetPosition)
        {
            if (activePlayers.TryGetValue(sessionId, out var player))
            {
                // Ajusta altura do target position ao terreno
                TerrainHeightmap.Instance.ClampToGround(targetPosition, CHARACTER_HEIGHT_OFFSET);
                
                player.targetPosition = targetPosition;
                player.isMoving = true;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Atualiza movimento de todos os players
        /// </summary>
        public void UpdateAllPlayersMovement(float deltaTime)
        {
            foreach (var kvp in activePlayers)
            {
                var player = kvp.Value;
                
                if (player.character.isDead)
                {
                    player.isMoving = false;
                    player.targetPosition = null;
                    continue;
                }
                
                if (player.isMoving && player.targetPosition != null)
                {
                    UpdatePlayerMovement(player, deltaTime);
                }
            }
        }

        /// <summary>
        /// Atualiza movimento individual do player
        /// Move suavemente em direção ao target, ajustando altura do terreno
        /// </summary>
        private void UpdatePlayerMovement(Player player, float deltaTime)
        {
            if (player.targetPosition == null) return;

            // Ajusta altura do target (caso o terreno tenha mudado)
            TerrainHeightmap.Instance.ClampToGround(player.targetPosition, CHARACTER_HEIGHT_OFFSET);

            // Calcula distância 2D (ignora Y)
            float dx = player.targetPosition.x - player.position.x;
            float dz = player.targetPosition.z - player.position.z;
            float distance = (float)Math.Sqrt(dx * dx + dz * dz);

            // Chegou perto o suficiente?
            if (distance < STOP_THRESHOLD)
            {
                // Posiciona exatamente no destino
                player.position.x = player.targetPosition.x;
                player.position.z = player.targetPosition.z;
                player.position.y = player.targetPosition.y; // Já está ajustado ao terreno
                
                // Atualiza position do character também
                player.character.position.x = player.position.x;
                player.character.position.y = player.position.y;
                player.character.position.z = player.position.z;
                
                // Só para se NÃO estiver em combate
                // Se estiver em combate, WorldManager gerencia a perseguição
                if (!player.inCombat)
                {
                    player.isMoving = false;
                    player.targetPosition = null;
                }
            }
            else
            {
                // Move em direção ao alvo
                float moveDistance = MOVE_SPEED * deltaTime;
                
                // Não ultrapassa o destino
                if (moveDistance > distance)
                {
                    moveDistance = distance;
                }

                // Calcula direção normalizada (2D)
                float dirX = dx / distance;
                float dirZ = dz / distance;

                // Aplica movimento
                player.position.x += dirX * moveDistance;
                player.position.z += dirZ * moveDistance;
                
                // Ajusta Y para seguir o terreno
                TerrainHeightmap.Instance.ClampToGround(player.position, CHARACTER_HEIGHT_OFFSET);
                
                // Atualiza character position também
                player.character.position.x = player.position.x;
                player.character.position.y = player.position.y;
                player.character.position.z = player.position.z;
            }
        }

        /// <summary>
        /// Obtém estados de todos os players para broadcast
        /// </summary>
        public List<PlayerState> GetAllPlayerStates()
        {
            return activePlayers.Select(kvp => new PlayerState
            {
                playerId = kvp.Key,
                characterName = kvp.Value.character.nome,
                position = kvp.Value.position,
                raca = kvp.Value.character.raca,
                classe = kvp.Value.character.classe,
                isMoving = kvp.Value.isMoving,
                targetPosition = kvp.Value.targetPosition
            }).ToList();
        }
    }

    public class PlayerState
    {
        public string playerId { get; set; } = "";
        public string characterName { get; set; } = "";
        public Position position { get; set; } = new Position();
        public string raca { get; set; } = "";
        public string classe { get; set; } = "";
        public bool isMoving { get; set; } = false;
        public Position? targetPosition { get; set; }
    }
}