using System.Timers;
using Newtonsoft.Json;
using MMOServer.Models;

namespace MMOServer.Server
{
    public class WorldManager
    {
        private static WorldManager? instance;
        public static WorldManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new WorldManager();
                return instance;
            }
        }

        private System.Timers.Timer? updateTimer;
        private const int UPDATE_INTERVAL = 50; // 50ms = 20 ticks/segundo
        private const int SAVE_INTERVAL = 5000; // Salva a cada 5 segundos

        private long lastSaveTime = 0;
        private object broadcastLock = new object();
        
        private DateTime serverStartTime = DateTime.UtcNow;

public void Initialize()
{
    Console.WriteLine("WorldManager initialized - Authoritative Server Mode (Ragnarok-style)");
    
    serverStartTime = DateTime.UtcNow;
    
    // 🆕 ADICIONE ESTA LINHA
    SkillManager.Instance.Initialize();
    
    MonsterManager.Instance.Initialize();
    
    updateTimer = new System.Timers.Timer(UPDATE_INTERVAL);
    updateTimer.Elapsed += OnWorldUpdate;
    updateTimer.AutoReset = true;
    updateTimer.Start();
    
    lastSaveTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    
    Console.WriteLine("✅ Combat System: Ragnarok-style auto-attack enabled");
    Console.WriteLine("   - Click monster to start attacking");
    Console.WriteLine("   - Click ground/another monster to stop");
    Console.WriteLine("   - Attack speed based on character ASPD");
    Console.WriteLine("✅ Loot System: Monster drops enabled");
    Console.WriteLine("   - Gold and items drop on monster death");
    Console.WriteLine("✅ Skill System: Skills enabled");
    Console.WriteLine("   - Learn skills at trainer NPCs");
    Console.WriteLine("   - Cast skills with cooldowns");
}

private void OnWorldUpdate(object? sender, ElapsedEventArgs e)
{
    lock (broadcastLock)
    {
        long currentTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        
        float currentTime = (float)(DateTime.UtcNow - serverStartTime).TotalSeconds;
        float deltaTime = UPDATE_INTERVAL / 1000f;

        // 1. Atualiza movimento de players
        PlayerManager.Instance.UpdateAllPlayersMovement(deltaTime);
        
        // 🆕 ADICIONE ESTAS LINHAS
        // 2. Atualiza buffs/debuffs ativos
        SkillManager.Instance.UpdateBuffs(deltaTime);
        
        // 3. Processa combate automático (estilo Ragnarok)
        ProcessPlayerCombat(currentTime, deltaTime);
        
        // 4. Atualiza monstros (AI e combate)
        MonsterManager.Instance.Update(deltaTime, currentTime);
        
        // 5. Broadcast do estado do mundo
        BroadcastWorldState();
        
        // 6. Salva periodicamente
        if (currentTimeMs - lastSaveTime >= SAVE_INTERVAL)
        {
            SaveWorldState();
            lastSaveTime = currentTimeMs;
        }
    }
}

        private void ProcessPlayerCombat(float currentTime, float deltaTime)
        {
            var players = PlayerManager.Instance.GetAllPlayers();
            
            foreach (var player in players)
            {
                // Ignora players mortos
                if (player.character.isDead)
                {
                    if (player.inCombat)
                    {
                        player.CancelCombat();
                    }
                    continue;
                }
                
                if (!player.inCombat || !player.targetMonsterId.HasValue)
                    continue;

                var monster = MonsterManager.Instance.GetMonster(player.targetMonsterId.Value);
                
                if (monster == null || !monster.isAlive)
                {
                    player.CancelCombat();
                    Console.WriteLine($"⚠️ {player.character.nome} stopped attacking (target died)");
                    continue;
                }

                float distance = GetDistance2D(player.position, monster.position);
                float attackRange = CombatManager.Instance.GetAttackRange();
                
                if (distance > attackRange)
                {
                    player.targetPosition = new Position 
                    { 
                        x = monster.position.x, 
                        y = monster.position.y, 
                        z = monster.position.z 
                    };
                    player.isMoving = true;
                    
                    if (player.lastAttackTime < 0)
                    {
                        player.lastAttackTime = currentTime - player.character.attackSpeed;
                    }
                }
                else
                {
                    player.isMoving = false;
                    player.targetPosition = null;
                    
				if (player.CanAttack(currentTime))
					{
						player.Attack(currentTime);
    
						// 🆕 ADICIONE ESTA LINHA ANTES DO COMBATE
						BroadcastPlayerAttack(player, monster);
    
						var result = CombatManager.Instance.PlayerAttackMonster(player, monster);
    
							BroadcastCombatResult(result);

                        if (result.damage > 0)
                        {
                            string critText = result.isCritical ? " CRIT!" : "";
                            float timeSinceLastAttack = currentTime - (player.lastAttackTime - player.character.attackSpeed);
                            
                            Console.WriteLine($"⚔️ {player.character.nome} -> {monster.template.name}: " +
                                            $"{result.damage}{critText} dmg " +
                                            $"(HP: {result.remainingHealth}/{monster.template.maxHealth}) " +
                                            $"[ASPD: {player.character.attackSpeed:F2}s] " +
                                            $"[Cooldown OK: {timeSinceLastAttack:F2}s]");
                        }
                        else
                        {
                            Console.WriteLine($"❌ {player.character.nome} MISSED {monster.template.name}!");
                        }

                        // 💰 Se matou, gera loot
                        if (result.targetDied)
                        {
                            player.CancelCombat();
                            
                            Console.WriteLine($"💀 {player.character.nome} killed {monster.template.name}! " +
                                            $"XP: +{result.experienceGained}");
                            
                            // 🆕 Gera e distribui loot
                            ProcessMonsterLoot(player, monster);
                            
                            if (result.leveledUp)
                            {
                                BroadcastLevelUp(player, result.newLevel);
                            }
                        }
                    }
                }
            }
        }
		
/// <summary>
/// 🆕 Notifica clientes sobre ataque do player
/// </summary>
private void BroadcastPlayerAttack(Player player, MonsterInstance monster)
{
    var message = new
    {
        type = "playerAttack",
        playerId = player.sessionId,
        characterName = player.character.nome,
        monsterId = monster.id,
        monsterName = monster.template.name,
        attackerPosition = player.position,
        targetPosition = monster.position
    };

    string json = JsonConvert.SerializeObject(message);
    GameServer.BroadcastToAll(json);
}

        // 🆕 SISTEMA DE LOOT
        private void ProcessMonsterLoot(Player player, MonsterInstance monster)
        {
            Console.WriteLine($"💰 Generating loot for {monster.template.name} (Template ID: {monster.templateId})...");
            
            var loot = ItemManager.Instance.GenerateLoot(monster.templateId);
            
            Console.WriteLine($"  - Gold rolled: {loot.gold}");
            Console.WriteLine($"  - Items rolled: {loot.items.Count}");
            
            if (loot.gold == 0 && loot.items.Count == 0)
            {
                Console.WriteLine($"  💨 No loot dropped (bad luck)");
                return;
            }

            var inventory = ItemManager.Instance.LoadInventory(player.character.id);
            
            // Adiciona gold
            if (loot.gold > 0)
            {
                inventory.gold += loot.gold;
                Console.WriteLine($"  💰 +{loot.gold} gold");
            }

            // Adiciona itens
            List<LootedItem> addedItems = new List<LootedItem>();
            
            foreach (var lootedItem in loot.items)
            {
                var template = ItemManager.Instance.GetItemTemplate(lootedItem.itemId);
                
                if (template == null)
                    continue;

                // Verifica se tem espaço
                if (!inventory.HasSpace() && template.maxStack == 1)
                {
                    Console.WriteLine($"  ⚠️ Inventory full! Could not loot {template.name}");
                    continue;
                }

                var itemInstance = ItemManager.Instance.CreateItemInstance(lootedItem.itemId, lootedItem.quantity);
                
                if (itemInstance != null && inventory.AddItem(itemInstance, template))
                {
                    addedItems.Add(lootedItem);
                    Console.WriteLine($"  📦 +{lootedItem.quantity}x {template.name}");
                }
            }

            // Salva inventário
            ItemManager.Instance.SaveInventory(inventory);

            // Broadcast de loot
            if (loot.gold > 0 || addedItems.Count > 0)
            {
                BroadcastLoot(player, loot.gold, addedItems);
            }
        }

        private void BroadcastLoot(Player player, int gold, List<LootedItem> items)
        {
            var message = new
            {
                type = "lootReceived",
                playerId = player.sessionId,
                characterName = player.character.nome,
                gold = gold,
                items = items
            };

            string json = JsonConvert.SerializeObject(message);
            GameServer.BroadcastToAll(json);
        }

        private float GetDistance2D(Position pos1, Position pos2)
        {
            float dx = pos1.x - pos2.x;
            float dz = pos1.z - pos2.z;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }

        private void BroadcastWorldState()
        {
            var players = PlayerManager.Instance.GetAllPlayers();
            var monsters = MonsterManager.Instance.GetAllMonsterStates();
            
            if (players.Count == 0) return;

            var playerStates = players.Select(p => new
            {
                playerId = p.sessionId,
                characterName = p.character.nome,
                position = p.position,
                raca = p.character.raca,
                classe = p.character.classe,
                level = p.character.level,
                health = p.character.health,
                maxHealth = p.character.maxHealth,
                mana = p.character.mana,
                maxMana = p.character.maxMana,
                experience = p.character.experience,
                statusPoints = p.character.statusPoints,
                isMoving = p.isMoving,
                targetPosition = p.targetPosition,
                inCombat = p.inCombat,
                targetMonsterId = p.targetMonsterId,
                isDead = p.character.isDead
            }).ToList();

            var worldState = new
            {
                type = "worldState",
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                players = playerStates,
                monsters = monsters
            };

            string json = JsonConvert.SerializeObject(worldState);
            GameServer.BroadcastToAll(json);
        }

        public void BroadcastCombatResult(CombatResult result)
        {
            var message = new
            {
                type = "combatResult",
                data = result
            };

            string json = JsonConvert.SerializeObject(message);
            GameServer.BroadcastToAll(json);
        }
		public void BroadcastPlayerStatsUpdate(Player player)
{
    var message = new
    {
        type = "playerStatsUpdate",
        playerId = player.sessionId,
        health = player.character.health,
        maxHealth = player.character.maxHealth,
        mana = player.character.mana,
        maxMana = player.character.maxMana
    };

    string json = JsonConvert.SerializeObject(message);
    GameServer.BroadcastToAll(json);
}

        private void BroadcastLevelUp(Player player, int newLevel)
        {
            var message = new
            {
                type = "levelUp",
                playerId = player.sessionId,
                characterName = player.character.nome,
                newLevel = newLevel,
                statusPoints = player.character.statusPoints,
                experience = player.character.experience,
                requiredExp = player.character.GetRequiredExp(),
                newStats = new
                {
                    maxHealth = player.character.maxHealth,
                    maxMana = player.character.maxMana,
                    attackPower = player.character.attackPower,
                    magicPower = player.character.magicPower,
                    defense = player.character.defense,
                    attackSpeed = player.character.attackSpeed,
                    strength = player.character.strength,
                    intelligence = player.character.intelligence,
                    dexterity = player.character.dexterity,
                    vitality = player.character.vitality
                }
            };

            string json = JsonConvert.SerializeObject(message);
            GameServer.BroadcastToAll(json);
        }

        public void BroadcastPlayerDeath(Player player)
        {
            var message = new
            {
                type = "playerDeath",
                playerId = player.sessionId,
                characterName = player.character.nome
            };

            string json = JsonConvert.SerializeObject(message);
            GameServer.BroadcastToAll(json);
        }

        public void BroadcastPlayerRespawn(Player player)
        {
            var message = new
            {
                type = "playerRespawn",
                playerId = player.sessionId,
                characterName = player.character.nome,
                position = player.position,
                health = player.character.health,
                maxHealth = player.character.maxHealth
            };

            string json = JsonConvert.SerializeObject(message);
            GameServer.BroadcastToAll(json);
        }

        private void SaveWorldState()
        {
            var players = PlayerManager.Instance.GetAllPlayers();
            foreach (var player in players)
            {
                try
                {
                    DatabaseHandler.Instance.UpdateCharacter(player.character);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving character {player.character.nome}: {ex.Message}");
                }
            }

            MonsterManager.Instance.SaveAllMonsters();
        }

        public void Shutdown()
        {
            Console.WriteLine("WorldManager: Saving all data before shutdown...");
            SaveWorldState();
            
            updateTimer?.Stop();
            updateTimer?.Dispose();
            Console.WriteLine("WorldManager shutdown complete");
        }
		public void BroadcastSkillCast(SkillCastResult result)
{
    var message = new
    {
        type = "skillCastBroadcast",
        casterId = result.casterId,
        casterName = result.casterName,
        casterType = result.casterType,
        skillId = result.skillId,
        skillName = result.skillName,
        success = result.success,
        failReason = result.failReason,
        targetResults = result.targetResults.Select(tr => new
        {
            targetId = tr.targetId,
            targetName = tr.targetName,
            targetType = tr.targetType,
            damage = tr.damage,
            heal = tr.heal,
            isCritical = tr.isCritical,
            isMiss = tr.isMiss,
            isEvaded = tr.isEvaded,
            remainingHealth = tr.remainingHealth,
            died = tr.died,
            appliedBuffs = tr.appliedBuffs.Select(b => new
            {
                buffId = b.buffId,
                buffName = b.buffName,
                effectType = b.effectType,
                affectedStat = b.affectedStat,
                statBoost = b.statBoost,
                remainingDuration = b.remainingDuration
            }).ToList()
        }).ToList()
    };

    string json = JsonConvert.SerializeObject(message);
    GameServer.BroadcastToAll(json);
}

// ============================================
// NOVO MÉTODO: BroadcastBuffUpdate
// ============================================

public void BroadcastBuffUpdate(int characterId, List<ActiveBuff> buffs)
{
    var message = new
    {
        type = "buffsUpdate",
        characterId = characterId,
        buffs = buffs.Select(b => new
        {
            buffId = b.buffId,
            buffName = b.buffName,
            effectType = b.effectType,
            affectedStat = b.affectedStat,
            statBoost = b.statBoost,
            remainingDuration = b.remainingDuration,
            isActive = b.isActive
        }).ToList()
    };

    string json = JsonConvert.SerializeObject(message);
    GameServer.BroadcastToAll(json);
}
    }
}