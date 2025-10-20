using MMOServer.Server;
using MMOServer.Configuration;
using WebSocketSharp.Server;

namespace MMOServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=================================");
            Console.WriteLine("===   MMO Server Starting     ===");
            Console.WriteLine("=================================");
            Console.WriteLine();
            
            // [0/7] Carrega appsettings.json PRIMEIRO
            Console.WriteLine("[0/7] Loading application settings...");
            ConfigLoader.Instance.LoadConfiguration();
            
            // [1/7] Carrega configurações JSON
            Console.WriteLine("[1/7] Loading JSON configurations...");
            ConfigManager.Instance.Initialize();
            
            // [2/7] Inicializa banco de dados
            Console.WriteLine("[2/7] Initializing database...");
            DatabaseHandler.Instance.Initialize();
            
            // [3/7] Carrega heightmap do terreno
            Console.WriteLine("[3/7] Loading terrain heightmap...");
            TerrainHeightmap.Instance.Initialize();
            
			Console.WriteLine("[3/7] Initializing skill system...");
SkillManager.Instance.Initialize();

            // [4/7] Inicializa sistema de itens
            Console.WriteLine("[4/7] Initializing item system...");
            ItemManager.Instance.Initialize();
            
            // [5/7] Inicializa gerenciadores
            Console.WriteLine("[5/7] Initializing managers...");
            WorldManager.Instance.Initialize();
            
            // [6/7] Inicia servidor WebSocket
            Console.WriteLine("[6/7] Starting WebSocket server...");
            
            // Usa configuração do appsettings.json
            var settings = ConfigLoader.Instance.Settings.ServerSettings;
            string serverUrl = $"ws://{settings.Host}:{settings.Port}";
            
            var wssv = new WebSocketServer(serverUrl);
            wssv.AddWebSocketService<GameServer>("/game");
            
            wssv.Start();
            
            Console.WriteLine();
            Console.WriteLine("=================================");
            Console.WriteLine($"✓ Server running on {serverUrl}/game");
            Console.WriteLine("=================================");
            Console.WriteLine();
            Console.WriteLine("Features enabled:");
            Console.WriteLine("  • JSON Configuration System");
            Console.WriteLine("  • 3D Terrain Heightmap Support");
            Console.WriteLine("  • Authoritative Movement");
            Console.WriteLine("  • Combat System (Ragnarok-style)");
            Console.WriteLine("  • Monster AI with Terrain Awareness");
            Console.WriteLine("  • Experience & Leveling");
            Console.WriteLine("  • Death & Respawn");
            Console.WriteLine("  • Item & Inventory System");
            Console.WriteLine("  • Loot System with Drop Tables");
            Console.WriteLine("  • Area-Based Monster Spawning");
            Console.WriteLine();
            
            if (TerrainHeightmap.Instance.IsLoaded)
            {
                Console.WriteLine("Terrain Status:");
                Console.WriteLine(TerrainHeightmap.Instance.GetTerrainInfo());
            }
            else
            {
                Console.WriteLine("Terrain Status: Using flat ground (Y=0)");
                Console.WriteLine("  Export heightmap from Unity: MMO > Export Terrain Heightmap");
            }
            
            Console.WriteLine();
            Console.WriteLine("Configuration files:");
            Console.WriteLine("  • appsettings.json - Server & Database settings");
            Console.WriteLine("  • Config/monsters.json - Monster templates");
            Console.WriteLine("  • Config/classes.json - Class configurations");
            Console.WriteLine("  • Config/terrain_heightmap.json - Terrain data");
            Console.WriteLine("  • Config/items.json - Item definitions");
            Console.WriteLine("  • Config/loot_tables.json - Monster drop tables");
            Console.WriteLine("  • Config/spawn_areas.json - Spawn area definitions");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  • 'reload'   - Reload JSON configurations");
            Console.WriteLine("  • 'config'   - Show current configuration");
            Console.WriteLine("  • 'terrain'  - Show terrain info");
            Console.WriteLine("  • 'status'   - Show server status");
            Console.WriteLine("  • 'items'    - Show item statistics");
            Console.WriteLine("  • 'loot'     - Test loot tables");
            Console.WriteLine("  • 'monsters' - List all monsters");
            Console.WriteLine("  • 'areas'    - Show spawn area statistics");
            Console.WriteLine("  • 'respawn'  - Force respawn all dead monsters");
            Console.WriteLine("  • 'combat'   - Show combat statistics");
            Console.WriteLine("  • 'balance'  - Test combat balance");
            Console.WriteLine("  • 'help'     - Show all commands");
            Console.WriteLine("  • 'exit'     - Stop the server");
            Console.WriteLine();
            
            // Loop de comandos
            bool running = true;
            while (running)
            {
                string? input = Console.ReadLine();
                
                if (string.IsNullOrEmpty(input))
                    continue;
                
                switch (input.ToLower().Trim())
                {
                    case "reload":
                        Console.WriteLine();
                        Console.WriteLine("🔄 Reloading configurations...");
                        ConfigManager.Instance.ReloadConfigs();
                        MonsterManager.Instance.ReloadFromConfig();
                        ItemManager.Instance.ReloadConfigs();
                        Console.WriteLine("✅ All configurations reloaded!");
                        Console.WriteLine();
                        break;
                    
                    case "config":
                        Console.WriteLine();
                        Console.WriteLine("📋 Current Configuration:");
                        var serverSettings = ConfigLoader.Instance.Settings.ServerSettings;
                        Console.WriteLine($"  Server Host: {serverSettings.Host}");
                        Console.WriteLine($"  Server Port: {serverSettings.Port}");
                        Console.WriteLine($"  Max Connections: {serverSettings.MaxConnections}");
                        Console.WriteLine($"  Update Rate: {serverSettings.UpdateRate}ms");
                        Console.WriteLine();
                        var dbSettings = ConfigLoader.Instance.Settings.DatabaseSettings;
                        Console.WriteLine($"  Database Server: {dbSettings.Server}:{dbSettings.Port}");
                        Console.WriteLine($"  Database Name: {dbSettings.Database}");
                        Console.WriteLine($"  Database User: {dbSettings.UserId}");
                        Console.WriteLine();
                        break;
                    
                    case "terrain":
                        Console.WriteLine();
                        if (TerrainHeightmap.Instance.IsLoaded)
                        {
                            Console.WriteLine(TerrainHeightmap.Instance.GetTerrainInfo());
                        }
                        else
                        {
                            Console.WriteLine("Terrain: Not loaded (using flat ground)");
                            Console.WriteLine("Export heightmap from Unity: MMO > Export Terrain Heightmap");
                        }
                        Console.WriteLine();
                        break;
                    
                    case "items":
                        Console.WriteLine();
                        Console.WriteLine("📦 Item System Statistics:");
                        var players = PlayerManager.Instance.GetAllPlayers();
                        
                        if (players.Count == 0)
                        {
                            Console.WriteLine("  No players online");
                        }
                        else
                        {
                            foreach (var player in players)
                            {
                                var inv = ItemManager.Instance.LoadInventory(player.character.id);
                                Console.WriteLine($"  {player.character.nome} (CharID: {player.character.id}):");
                                Console.WriteLine($"    Gold: {inv.gold}");
                                Console.WriteLine($"    Items: {inv.items.Count}/{inv.maxSlots}");
                                
                                string weaponStatus = inv.weaponId.HasValue ? inv.weaponId.Value.ToString() : "None";
                                string armorStatus = inv.armorId.HasValue ? inv.armorId.Value.ToString() : "None";
                                Console.WriteLine($"    Equipped: Weapon={weaponStatus}, Armor={armorStatus}");
                                
                                if (inv.items.Count > 0)
                                {
                                    Console.WriteLine($"    Item List:");
                                    foreach (var item in inv.items)
                                    {
                                        string equipped = item.isEquipped ? " [EQUIPPED]" : "";
                                        string itemName = item.template?.name ?? "Unknown";
                                        Console.WriteLine($"      - ID:{item.instanceId} | {itemName} x{item.quantity}{equipped}");
                                    }
                                }
                            }
                        }
                        Console.WriteLine();
                        break;
                    
                    case "loot":
                        Console.WriteLine();
                        Console.WriteLine("💰 Loot Tables:");
                        var monsters = MonsterManager.Instance.GetAllMonsters();
                        foreach (var m in monsters.Take(5)) // Mostra só 5 para não poluir
                        {
                            Console.WriteLine($"  [{m.templateId}] {m.template.name}:");
                            var testLoot = ItemManager.Instance.GenerateLoot(m.templateId);
                        }
                        Console.WriteLine();
                        break;
                    
                    case "monsters":
                        Console.WriteLine();
                        Console.WriteLine("👹 Active Monsters:");
                        var allMonsters = MonsterManager.Instance.GetAllMonsters();
                        foreach (var m in allMonsters)
                        {
                            Console.WriteLine($"  [{m.id}] {m.template.name} (Template: {m.templateId})");
                            Console.WriteLine($"      HP: {m.currentHealth}/{m.template.maxHealth}");
                            Console.WriteLine($"      Alive: {m.isAlive}, In Combat: {m.inCombat}");
                            Console.WriteLine($"      Pos: ({m.position.x:F1}, {m.position.z:F1})");
                            Console.WriteLine($"      Spawn Area: {m.spawnAreaId}");
                        }
                        Console.WriteLine();
                        break;
                    
                    case "areas":
                        Console.WriteLine();
                        Console.WriteLine("📍 Spawn Area Statistics:");
                        var areas = SpawnAreaManager.Instance.GetAllAreas();
                        var areaStats = MonsterManager.Instance.GetSpawnAreaStats();
                        
                        foreach (var area in areas)
                        {
                            Console.WriteLine($"\n  [{area.id}] {area.name}");
                            Console.WriteLine($"      Type: {area.shape}");
                            Console.WriteLine($"      Center: ({area.centerX:F1}, {area.centerZ:F1})");
                            
                            if (area.shape == "circle")
                                Console.WriteLine($"      Radius: {area.radius}m");
                            else
                                Console.WriteLine($"      Size: {area.width}x{area.length}m");
                            
                            Console.WriteLine($"      Max Slope: {area.maxSlope}°");
                            Console.WriteLine($"      Configured Spawns: {area.spawns.Count} types");
                            
                            if (areaStats.TryGetValue(area.id, out var stats))
                            {
                                Console.WriteLine($"      Active Monsters: {stats.aliveMonsters}/{stats.totalMonsters}");
                                Console.WriteLine($"      Dead: {stats.deadMonsters}, In Combat: {stats.inCombat}");
                            }
                            
                            foreach (var spawn in area.spawns)
                            {
                                Console.WriteLine($"        • {spawn.count}x {spawn.monsterName} (Respawn: {spawn.respawnTime}s)");
                            }
                        }
                        Console.WriteLine();
                        break;
                    
                    case "respawn":
                        Console.WriteLine();
                        Console.WriteLine("✨ Force respawning all dead monsters...");
                        int respawned = 0;
                        foreach (var monster in MonsterManager.Instance.GetAllMonsters())
                        {
                            if (!monster.isAlive)
                            {
                                var area = SpawnAreaManager.Instance.GetArea(monster.spawnAreaId);
                                
                                if (area != null)
                                {
                                    var newPos = SpawnAreaManager.Instance.GetRandomPositionInArea(area);
                                    
                                    if (newPos != null)
                                    {
                                        monster.position = newPos;
                                    }
                                }
                                
                                monster.Respawn();
                                TerrainHeightmap.Instance.ClampToGround(monster.position, 1f);
                                DatabaseHandler.Instance.UpdateMonsterInstance(monster);
                                respawned++;
                            }
                        }
                        Console.WriteLine($"✅ Respawned {respawned} monsters!");
                        Console.WriteLine();
                        break;
                    
                    case "combat":
                        Console.WriteLine();
                        Console.WriteLine("⚔️ Combat System Statistics:");
                        var allPlayers = PlayerManager.Instance.GetAllPlayers();
                        
                        if (allPlayers.Count == 0)
                        {
                            Console.WriteLine("  No players online");
                        }
                        else
                        {
                            foreach (var player in allPlayers)
                            {
                                Console.WriteLine(CombatManager.Instance.GetCombatStats(player));
                                Console.WriteLine();
                            }
                        }
                        
                        Console.WriteLine("👹 Monster Stats:");
                        var activeMonsters = MonsterManager.Instance.GetAliveMonsters();
                        
                        if (activeMonsters.Count == 0)
                        {
                            Console.WriteLine("  No monsters alive");
                        }
                        else
                        {
                            var uniqueMonsters = activeMonsters
                                .GroupBy(m => m.templateId)
                                .Select(g => g.First())
                                .ToList();
                            
                            foreach (var monster in uniqueMonsters)
                            {
                                Console.WriteLine(CombatManager.Instance.GetMonsterStats(monster));
                                Console.WriteLine();
                            }
                        }
                        Console.WriteLine();
                        break;
                    
                    case "balance":
                        Console.WriteLine();
                        Console.WriteLine("⚖️ Combat Balance Test:");
                        Console.WriteLine();
                        
                        var testPlayer = PlayerManager.Instance.GetAllPlayers().FirstOrDefault();
                        if (testPlayer == null)
                        {
                            Console.WriteLine("  No players online to test");
                            break;
                        }
                        
                        var testMonster = MonsterManager.Instance.GetAliveMonsters().FirstOrDefault();
                        if (testMonster == null)
                        {
                            Console.WriteLine("  No monsters alive to test");
                            break;
                        }
                        
                        Console.WriteLine($"Testing: {testPlayer.character.nome} (Lv.{testPlayer.character.level}) vs {testMonster.template.name} (Lv.{testMonster.template.level})");
                        Console.WriteLine();
                        
                        // Simula 10 ataques do player
                        Console.WriteLine("Player → Monster (10 simulated attacks):");
                        int playerHits = 0;
                        int playerCrits = 0;
                        int totalPlayerDamage = 0;
                        
                        for (int i = 0; i < 10; i++)
                        {
                            var result = CombatManager.Instance.PlayerAttackMonster(testPlayer, testMonster);
                            if (result.damage > 0)
                            {
                                playerHits++;
                                totalPlayerDamage += result.damage;
                                if (result.isCritical) playerCrits++;
                                testMonster.currentHealth = testMonster.template.maxHealth;
                            }
                        }
                        
                        Console.WriteLine($"  Hits: {playerHits}/10 ({playerHits * 10}%)");
                        Console.WriteLine($"  Crits: {playerCrits}/10");
                        Console.WriteLine($"  Avg Damage: {(playerHits > 0 ? totalPlayerDamage / playerHits : 0)}");
                        Console.WriteLine($"  Total Damage: {totalPlayerDamage}");
                        Console.WriteLine();
                        
                        // Simula 10 ataques do monstro
                        Console.WriteLine("Monster → Player (10 simulated attacks):");
                        int monsterHits = 0;
                        int monsterCrits = 0;
                        int totalMonsterDamage = 0;
                        int originalPlayerHP = testPlayer.character.health;
                        
                        for (int i = 0; i < 10; i++)
                        {
                            var result = CombatManager.Instance.MonsterAttackPlayer(testMonster, testPlayer);
                            if (result.damage > 0)
                            {
                                monsterHits++;
                                totalMonsterDamage += result.damage;
                                if (result.isCritical) monsterCrits++;
                                testPlayer.character.health = originalPlayerHP;
                            }
                        }
                        
                        Console.WriteLine($"  Hits: {monsterHits}/10 ({monsterHits * 10}%)");
                        Console.WriteLine($"  Crits: {monsterCrits}/10");
                        Console.WriteLine($"  Avg Damage: {(monsterHits > 0 ? totalMonsterDamage / monsterHits : 0)}");
                        Console.WriteLine($"  Total Damage: {totalMonsterDamage}");
                        Console.WriteLine();
                        
                        // Análise
                        Console.WriteLine("Analysis:");
                        if (playerHits == 0)
                            Console.WriteLine("  ⚠️ Player can't hit this monster!");
                        if (monsterHits == 0)
                            Console.WriteLine("  ✅ Player dodges all attacks from this monster!");
                        
                        if (playerHits > 0 && monsterHits > 0)
                        {
                            int playerDPS = totalPlayerDamage / 10;
                            int monsterDPS = totalMonsterDamage / 10;
                            
                            if (playerDPS > monsterDPS * 2)
                                Console.WriteLine("  ⚔️ Player dominates this fight");
                            else if (monsterDPS > playerDPS * 2)
                                Console.WriteLine("  👹 Monster dominates this fight");
                            else
                                Console.WriteLine("  ⚖️ Balanced fight");
                        }
                        
                        Console.WriteLine();
                        break;
                    
                    case "status":
                        Console.WriteLine();
                        Console.WriteLine("🖥️ Server Status:");
                        Console.WriteLine($"  Players online: {PlayerManager.Instance.GetAllPlayers().Count}");
                        Console.WriteLine($"  Active monsters: {MonsterManager.Instance.GetAliveMonsters().Count}");
                        Console.WriteLine($"  Total monster instances: {MonsterManager.Instance.GetAllMonsters().Count}");
                        Console.WriteLine($"  Monster templates: {ConfigManager.Instance.MonsterConfig.monsters.Count}");
                        Console.WriteLine($"  Spawn areas: {SpawnAreaManager.Instance.GetAllAreas().Count}");
                        Console.WriteLine($"  Available classes: {ConfigManager.Instance.ClassConfig.classes.Count}");
                        
                        bool itemsLoaded = ItemManager.Instance.GetItemTemplate(1) != null;
                        Console.WriteLine($"  Item templates: {(itemsLoaded ? "Loaded" : "Not loaded")}");
                        Console.WriteLine($"  Terrain loaded: {(TerrainHeightmap.Instance.IsLoaded ? "Yes" : "No")}");
                        Console.WriteLine();
                        break;
                    
                    case "help":
                        Console.WriteLine();
                        Console.WriteLine("📖 Available Commands:");
                        Console.WriteLine("  reload   - Reload JSON configurations");
                        Console.WriteLine("  config   - Show current configuration from appsettings.json");
                        Console.WriteLine("  terrain  - Show terrain information");
                        Console.WriteLine("  status   - Show server status");
                        Console.WriteLine("  items    - Show item statistics for online players");
                        Console.WriteLine("  loot     - Test loot generation");
                        Console.WriteLine("  monsters - List all monster instances");
                        Console.WriteLine("  areas    - Show spawn area statistics");
                        Console.WriteLine("  respawn  - Force respawn all dead monsters");
                        Console.WriteLine("  combat   - Show combat statistics");
                        Console.WriteLine("  balance  - Test combat balance (simulate fights)");
                        Console.WriteLine("  help     - Show this help");
                        Console.WriteLine("  exit     - Stop the server");
                        Console.WriteLine();
                        break;
                    
                    case "exit":
                    case "quit":
                    case "stop":
                        running = false;
                        break;
                    
                    default:
                        Console.WriteLine($"❌ Unknown command: '{input}'");
                        Console.WriteLine("   Type 'help' for available commands");
                        break;
                }
            }
            
            Console.WriteLine();
            Console.WriteLine("🛑 Shutting down server...");
            Console.WriteLine("   Saving all data...");
            WorldManager.Instance.Shutdown();
            wssv.Stop();
            Console.WriteLine("✅ Server stopped successfully.");
        }
    }
}