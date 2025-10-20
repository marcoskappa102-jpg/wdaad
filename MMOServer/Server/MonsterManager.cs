using MMOServer.Models;
using System.Collections.Concurrent;

namespace MMOServer.Server
{
    public class MonsterManager
    {
        private static MonsterManager? instance;
        public static MonsterManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new MonsterManager();
                return instance;
            }
        }

        private ConcurrentDictionary<int, MonsterInstance> activeMonsters = new ConcurrentDictionary<int, MonsterInstance>();
        private Dictionary<int, MonsterTemplate> templates = new Dictionary<int, MonsterTemplate>();
        private Dictionary<int, SpawnAreaInfo> spawnAreaInfos = new Dictionary<int, SpawnAreaInfo>();
        
        private const float CHASE_UPDATE_INTERVAL = 0.3f;
        private const float MONSTER_HEIGHT_OFFSET = 1f;
        private Dictionary<int, float> lastChaseUpdate = new Dictionary<int, float>();
        private int nextInstanceId = 1;
        private Random random = new Random();

        public void Initialize()
        {
            Console.WriteLine("👹 MonsterManager: Initializing...");
            
            LoadTemplatesFromConfig();
            bool loadedFromDatabase = LoadInstancesFromDatabase();
            
            if (!loadedFromDatabase)
            {
                Console.WriteLine("📝 No instances in database, spawning from areas...");
                SpawnFromAreas();
                SaveAllMonsters();
            }
            else
            {
                ValidateAndAdjustExistingMonsters();
            }
            
            Console.WriteLine($"✅ MonsterManager: Loaded {activeMonsters.Count} monster instances");
        }

        private void LoadTemplatesFromConfig()
        {
            var monsterConfigs = ConfigManager.Instance.MonsterConfig.monsters;
            
            if (monsterConfigs.Count == 0)
            {
                Console.WriteLine("⚠️ No monsters found in config!");
                return;
            }

            foreach (var config in monsterConfigs)
            {
                var template = new MonsterTemplate
                {
                    id = config.id,
                    name = config.name,
                    level = config.level,
                    maxHealth = config.maxHealth,
                    attackPower = config.attackPower,
                    defense = config.defense,
                    experienceReward = config.experienceReward,
                    attackSpeed = config.attackSpeed,
                    movementSpeed = config.movementSpeed,
                    aggroRange = config.aggroRange,
                    
                    // 🆕 Sistema de Patrulha
                    prefabPath = config.prefabPath,
                    patrolBehavior = config.patrolBehavior,
                    patrolRadius = config.patrolRadius,
                    patrolInterval = config.patrolInterval,
                    idleTime = config.idleTime,
                    
                    spawnX = 0,
                    spawnY = 0,
                    spawnZ = 0,
                    spawnRadius = 0,
                    respawnTime = 30
                };
                
                templates[template.id] = template;
            }
            
            Console.WriteLine($"✅ Loaded {templates.Count} monster templates from JSON");
        }

        private bool LoadInstancesFromDatabase()
        {
            try
            {
                var instances = DatabaseHandler.Instance.LoadMonsterInstances();
                
                if (instances.Count == 0)
                    return false;

                foreach (var instance in instances)
                {
                    if (templates.TryGetValue(instance.templateId, out var template))
                    {
                        instance.template = template;
                    }
                    else
                    {
                        Console.WriteLine($"⚠️ Template {instance.templateId} not found for instance {instance.id}");
                        continue;
                    }
                    
                    instance.lastAttackTime = -999f;
                    instance.spawnPosition = new Position 
                    { 
                        x = instance.position.x, 
                        y = instance.position.y, 
                        z = instance.position.z 
                    };
                    
                    activeMonsters[instance.id] = instance;
                    lastChaseUpdate[instance.id] = 0f;
                    
                    if (instance.id >= nextInstanceId)
                        nextInstanceId = instance.id + 1;
                }
                
                Console.WriteLine($"✅ Loaded {activeMonsters.Count} monster instances from database");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error loading from database: {ex.Message}");
                return false;
            }
        }

        private void SpawnFromAreas()
        {
            var areas = SpawnAreaManager.Instance.GetAllAreas();
            
            if (areas.Count == 0)
            {
                Console.WriteLine("⚠️ No spawn areas defined!");
                return;
            }

            Console.WriteLine($"📍 Spawning monsters from {areas.Count} areas...");

            foreach (var area in areas)
            {
                Console.WriteLine($"\n📍 Area [{area.id}] {area.name}:");
                
                var areaInfo = new SpawnAreaInfo
                {
                    areaId = area.id,
                    areaName = area.name,
                    monsterInstances = new List<int>()
                };

                foreach (var spawnEntry in area.spawns)
                {
                    var template = GetMonsterTemplate(spawnEntry.monsterId);
                    
                    if (template == null)
                    {
                        Console.WriteLine($"  ⚠️ Template {spawnEntry.monsterId} not found!");
                        continue;
                    }

                    for (int i = 0; i < spawnEntry.count; i++)
                    {
                        var position = SpawnAreaManager.Instance.GetRandomPositionInArea(area);
                        
                        if (position == null)
                        {
                            Console.WriteLine($"  ⚠️ Could not find valid position for {template.name}");
                            continue;
                        }

                        var instance = new MonsterInstance
                        {
                            id = nextInstanceId++,
                            templateId = template.id,
                            template = template,
                            currentHealth = template.maxHealth,
                            position = position,
                            isAlive = true,
                            lastRespawn = DateTime.Now,
                            lastAttackTime = -999f,
                            spawnAreaId = area.id,
                            customRespawnTime = spawnEntry.respawnTime,
                            
                            // 🆕 Sistema de Patrulha
                            spawnPosition = new Position 
                            { 
                                x = position.x, 
                                y = position.y, 
                                z = position.z 
                            },
                            lastPatrolTime = 0f,
                            isIdle = true
                        };

                        activeMonsters[instance.id] = instance;
                        lastChaseUpdate[instance.id] = 0f;
                        areaInfo.monsterInstances.Add(instance.id);

                        Console.WriteLine($"  ✨ [{i+1}/{spawnEntry.count}] {template.name} (ID:{instance.id}) - {template.patrolBehavior}");
                    }
                }

                spawnAreaInfos[area.id] = areaInfo;
                Console.WriteLine($"  ✅ Spawned {areaInfo.monsterInstances.Count} monsters");
            }

            Console.WriteLine($"\n✅ Total monsters spawned: {activeMonsters.Count}");
        }

        private void ValidateAndAdjustExistingMonsters()
        {
            Console.WriteLine("🔍 Validating existing monsters...");

            var monstersToRespawn = new List<MonsterInstance>();

            foreach (var monster in activeMonsters.Values)
            {
                TerrainHeightmap.Instance.ClampToGround(monster.position, MONSTER_HEIGHT_OFFSET);

                if (monster.spawnAreaId == 0)
                {
                    var nearestArea = SpawnAreaManager.Instance.FindNearestArea(monster.position);
                    
                    if (nearestArea != null)
                    {
                        monster.spawnAreaId = nearestArea.id;
                        Console.WriteLine($"  📍 {monster.template.name} (ID:{monster.id}) assigned to area: {nearestArea.name}");
                    }
                }

                if (monster.spawnAreaId > 0)
                {
                    var area = SpawnAreaManager.Instance.GetArea(monster.spawnAreaId);
                    
                    if (area != null && !SpawnAreaManager.Instance.IsPositionInArea(monster.position, area))
                    {
                        Console.WriteLine($"  ⚠️ {monster.template.name} (ID:{monster.id}) outside spawn area, will respawn");
                        monstersToRespawn.Add(monster);
                    }
                }
                
                // 🆕 Configura spawn position se não estiver definido
                if (monster.spawnPosition == null || 
                    (monster.spawnPosition.x == 0 && monster.spawnPosition.z == 0))
                {
                    monster.spawnPosition = new Position 
                    { 
                        x = monster.position.x, 
                        y = monster.position.y, 
                        z = monster.position.z 
                    };
                }
            }

            foreach (var monster in monstersToRespawn)
            {
                var area = SpawnAreaManager.Instance.GetArea(monster.spawnAreaId);
                
                if (area != null)
                {
                    var newPos = SpawnAreaManager.Instance.GetRandomPositionInArea(area);
                    
                    if (newPos != null)
                    {
                        monster.position = newPos;
                        monster.spawnPosition = new Position 
                        { 
                            x = newPos.x, 
                            y = newPos.y, 
                            z = newPos.z 
                        };
                        Console.WriteLine($"  ✨ Respawned {monster.template.name} (ID:{monster.id}) at ({newPos.x:F1}, {newPos.z:F1})");
                    }
                }
            }

            Console.WriteLine($"✅ Validated {activeMonsters.Count} monsters ({monstersToRespawn.Count} respawned)");
        }

        public void Update(float deltaTime, float currentTime)
        {
            foreach (var kvp in activeMonsters)
            {
                var monster = kvp.Value;

                if (!monster.isAlive)
                {
                    CheckRespawn(monster);
                    continue;
                }

                UpdateMonsterAI(monster, deltaTime, currentTime);
            }
        }

        private void UpdateMonsterAI(MonsterInstance monster, float deltaTime, float currentTime)
        {
            if (!monster.inCombat)
            {
                var nearestPlayer = FindNearestPlayerInRange(monster);
                
                if (nearestPlayer != null && !nearestPlayer.character.isDead)
                {
                    monster.inCombat = true;
                    monster.targetPlayerId = nearestPlayer.sessionId;
                    monster.isIdle = false;
                    lastChaseUpdate[monster.id] = currentTime;
                    Console.WriteLine($"👹 {monster.template.name} (ID:{monster.id}) aggroed {nearestPlayer.character.nome}!");
                }
                else
                {
                    // 🆕 Sistema de Patrulha
                    UpdatePatrolMovement(monster, deltaTime, currentTime);
                }
            }
            else
            {
                UpdateCombatAI(monster, deltaTime, currentTime);
            }
        }

        /// <summary>
        /// 🆕 Sistema de Patrulha - Atualiza movimento quando não está em combate
        /// </summary>
        private void UpdatePatrolMovement(MonsterInstance monster, float deltaTime, float currentTime)
        {
            var template = monster.template;
            
            switch (template.patrolBehavior)
            {
                case "stationary":
                    UpdateStationaryBehavior(monster, currentTime);
                    break;
                    
                case "wander":
                    UpdateWanderBehavior(monster, deltaTime, currentTime);
                    break;
                    
                case "patrol":
                    UpdatePatrolBehavior(monster, deltaTime, currentTime);
                    break;
                    
                default:
                    UpdateWanderBehavior(monster, deltaTime, currentTime);
                    break;
            }
            
            // Move em direção ao target se houver
            if (monster.targetPosition != null && !monster.isIdle)
            {
                MoveTowardsTarget(monster, deltaTime);
            }
        }

        /// <summary>
        /// Comportamento Estacionário - Fica parado na posição de spawn
        /// </summary>
        private void UpdateStationaryBehavior(MonsterInstance monster, float currentTime)
        {
            // Verifica se está longe do spawn
            float distance = GetDistance2D(monster.position, monster.spawnPosition);
            
            if (distance > 2f)
            {
                // Retorna para spawn
                monster.targetPosition = monster.spawnPosition;
                monster.isMoving = true;
                monster.isIdle = false;
            }
            else
            {
                // Fica parado
                monster.isMoving = false;
                monster.isIdle = true;
                monster.targetPosition = null;
            }
        }

        /// <summary>
        /// Comportamento Wander - Vaga aleatoriamente
        /// </summary>
        private void UpdateWanderBehavior(MonsterInstance monster, float deltaTime, float currentTime)
        {
            var template = monster.template;
            
            // Está parado?
            if (monster.isIdle)
            {
                // Verifica se já passou o tempo de idle
                if (currentTime - monster.lastIdleTime >= template.idleTime)
                {
                    // Escolhe novo destino aleatório
                    var newTarget = GetRandomPatrolPoint(monster.spawnPosition, template.patrolRadius);
                    
                    if (newTarget != null)
                    {
                        monster.targetPosition = newTarget;
                        monster.isMoving = true;
                        monster.isIdle = false;
                        monster.lastPatrolTime = currentTime;
                    }
                }
            }
            else
            {
                // Está se movendo - verifica se chegou
                if (monster.targetPosition != null)
                {
                    float distance = GetDistance2D(monster.position, monster.targetPosition);
                    
                    if (distance < 0.5f)
                    {
                        // Chegou - fica idle
                        monster.isMoving = false;
                        monster.isIdle = true;
                        monster.lastIdleTime = currentTime;
                        monster.targetPosition = null;
                    }
                }
                
                // Timeout - escolhe novo destino
                if (currentTime - monster.lastPatrolTime >= template.patrolInterval * 2)
                {
                    var newTarget = GetRandomPatrolPoint(monster.spawnPosition, template.patrolRadius);
                    
                    if (newTarget != null)
                    {
                        monster.targetPosition = newTarget;
                        monster.lastPatrolTime = currentTime;
                    }
                }
            }
        }

        /// <summary>
        /// Comportamento Patrol - Segue pontos de patrulha predefinidos
        /// </summary>
        private void UpdatePatrolBehavior(MonsterInstance monster, float deltaTime, float currentTime)
        {
            var template = monster.template;
            
            if (monster.isIdle)
            {
                if (currentTime - monster.lastIdleTime >= template.idleTime)
                {
                    // Próximo ponto de patrulha
                    monster.patrolPointIndex = (monster.patrolPointIndex + 1) % 4; // 4 pontos
                    
                    var newTarget = GetPatrolPoint(monster.spawnPosition, template.patrolRadius, monster.patrolPointIndex);
                    
                    if (newTarget != null)
                    {
                        monster.targetPosition = newTarget;
                        monster.isMoving = true;
                        monster.isIdle = false;
                        monster.lastPatrolTime = currentTime;
                    }
                }
            }
            else
            {
                if (monster.targetPosition != null)
                {
                    float distance = GetDistance2D(monster.position, monster.targetPosition);
                    
                    if (distance < 0.5f)
                    {
                        monster.isMoving = false;
                        monster.isIdle = true;
                        monster.lastIdleTime = currentTime;
                        monster.targetPosition = null;
                    }
                }
            }
        }

        /// <summary>
        /// Obtém ponto aleatório dentro do raio de patrulha
        /// </summary>
        private Position? GetRandomPatrolPoint(Position center, float radius)
        {
            double angle = random.NextDouble() * Math.PI * 2;
            double distance = Math.Sqrt(random.NextDouble()) * radius;
            
            float x = center.x + (float)(Math.Cos(angle) * distance);
            float z = center.z + (float)(Math.Sin(angle) * distance);
            
            // Valida terreno
            if (TerrainHeightmap.Instance.IsValidSpawnPosition(x, z, 45f))
            {
                float y = TerrainHeightmap.Instance.GetHeightAt(x, z) + MONSTER_HEIGHT_OFFSET;
                return new Position { x = x, y = y, z = z };
            }
            
            return null;
        }

        /// <summary>
        /// Obtém ponto de patrulha predefinido (quadrado/círculo)
        /// </summary>
        private Position? GetPatrolPoint(Position center, float radius, int index)
        {
            // 4 pontos cardeais
            float angle = (index * 90) * (float)Math.PI / 180f;
            
            float x = center.x + (float)(Math.Cos(angle) * radius);
            float z = center.z + (float)(Math.Sin(angle) * radius);
            
            if (TerrainHeightmap.Instance.IsValidSpawnPosition(x, z, 45f))
            {
                float y = TerrainHeightmap.Instance.GetHeightAt(x, z) + MONSTER_HEIGHT_OFFSET;
                return new Position { x = x, y = y, z = z };
            }
            
            return null;
        }

        private void UpdateCombatAI(MonsterInstance monster, float deltaTime, float currentTime)
        {
            var targetPlayer = PlayerManager.Instance.GetPlayer(monster.targetPlayerId!);

            if (targetPlayer == null || targetPlayer.character.isDead)
            {
                ResetMonsterCombat(monster);
                Console.WriteLine($"👹 {monster.template.name} lost target");
                return;
            }

            float distance = CombatManager.Instance.GetDistance(monster.position, targetPlayer.position);
            float aggroRange = monster.template.aggroRange;
            float attackRange = CombatManager.Instance.GetAttackRange();
            
            if (distance > aggroRange * 1.5f)
            {
                ResetMonsterCombat(monster);
                Console.WriteLine($"👹 {monster.template.name} lost aggro (too far: {distance:F1}m)");
                ReturnToSpawnArea(monster);
                
                int healAmount = (int)(monster.template.maxHealth * 0.2f);
                monster.currentHealth = Math.Min(monster.currentHealth + healAmount, monster.template.maxHealth);
                return;
            }

            if (currentTime - lastChaseUpdate[monster.id] >= CHASE_UPDATE_INTERVAL)
            {
                monster.targetPosition = new Position
                {
                    x = targetPlayer.position.x,
                    y = targetPlayer.position.y,
                    z = targetPlayer.position.z
                };
                lastChaseUpdate[monster.id] = currentTime;
            }

            if (distance > attackRange)
            {
                MoveTowardsTarget(monster, deltaTime);
            }
            else
            {
                monster.isMoving = false;
                monster.targetPosition = null;
            }

            if (distance <= attackRange && monster.CanAttack(currentTime))
            {
                monster.Attack(currentTime);
                
                var result = CombatManager.Instance.MonsterAttackPlayer(monster, targetPlayer);
                
                WorldManager.Instance.BroadcastCombatResult(result);

                if (result.damage > 0)
                {
                    string critText = result.isCritical ? " CRIT!" : "";
                    Console.WriteLine($"👹 {monster.template.name} -> {targetPlayer.character.nome}: {result.damage}{critText} dmg");
                }

                if (result.targetDied)
                {
                    ResetMonsterCombat(monster);
                    Console.WriteLine($"💀 {monster.template.name} killed {targetPlayer.character.nome}!");
                    WorldManager.Instance.BroadcastPlayerDeath(targetPlayer);
                    ReturnToSpawnArea(monster);
                    monster.currentHealth = monster.template.maxHealth;
                }
            }
        }

        private void ResetMonsterCombat(MonsterInstance monster)
        {
            monster.inCombat = false;
            monster.targetPlayerId = null;
            monster.isMoving = false;
            monster.targetPosition = null;
            monster.isIdle = true;
            monster.lastIdleTime = 0f;
        }

        private void MoveTowardsTarget(MonsterInstance monster, float deltaTime)
        {
            if (monster.targetPosition == null)
                return;

            float dx = monster.targetPosition.x - monster.position.x;
            float dz = monster.targetPosition.z - monster.position.z;
            float distance = (float)Math.Sqrt(dx * dx + dz * dz);

            if (distance > 0.1f)
            {
                float moveDistance = monster.template.movementSpeed * deltaTime;
                
                if (moveDistance > distance)
                    moveDistance = distance;

                float dirX = dx / distance;
                float dirZ = dz / distance;

                monster.position.x += dirX * moveDistance;
                monster.position.z += dirZ * moveDistance;
                
                TerrainHeightmap.Instance.ClampToGround(monster.position, MONSTER_HEIGHT_OFFSET);
                
                monster.isMoving = true;
            }
            else
            {
                monster.isMoving = false;
                monster.targetPosition = null;
            }
        }

        private void ReturnToSpawnArea(MonsterInstance monster)
        {
            var area = SpawnAreaManager.Instance.GetArea(monster.spawnAreaId);
            
            if (area == null)
            {
                monster.isMoving = false;
                monster.targetPosition = null;
                return;
            }

            float y = TerrainHeightmap.Instance.GetHeightAt(area.centerX, area.centerZ) + MONSTER_HEIGHT_OFFSET;
            
            monster.targetPosition = new Position
            {
                x = area.centerX,
                y = y,
                z = area.centerZ
            };
            monster.isMoving = true;
        }

        private Player? FindNearestPlayerInRange(MonsterInstance monster)
        {
            var players = PlayerManager.Instance.GetAllPlayers();
            Player? nearest = null;
            float minDistance = monster.template.aggroRange;

            foreach (var player in players)
            {
                if (player.character.isDead)
                    continue;

                float distance = CombatManager.Instance.GetDistance(monster.position, player.position);
                
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = player;
                }
            }

            return nearest;
        }

        private void CheckRespawn(MonsterInstance monster)
        {
            int respawnTime = monster.customRespawnTime > 0 ? monster.customRespawnTime : monster.template.respawnTime;
            
            var timeSinceDeath = (DateTime.Now - monster.lastRespawn).TotalSeconds;
            
            if (timeSinceDeath >= respawnTime)
            {
                var area = SpawnAreaManager.Instance.GetArea(monster.spawnAreaId);
                
                if (area != null)
                {
                    var newPos = SpawnAreaManager.Instance.GetRandomPositionInArea(area);
                    
                    if (newPos != null)
                    {
                        monster.position = newPos;
                        monster.spawnPosition = new Position 
                        { 
                            x = newPos.x, 
                            y = newPos.y, 
                            z = newPos.z 
                        };
                    }
                }
                
                monster.Respawn();
                TerrainHeightmap.Instance.ClampToGround(monster.position, MONSTER_HEIGHT_OFFSET);
                
                monster.lastAttackTime = -999f;
                lastChaseUpdate[monster.id] = 0f;
                
                Console.WriteLine($"✨ {monster.template.name} (ID:{monster.id}) respawned at ({monster.position.x:F1}, {monster.position.y:F1}, {monster.position.z:F1})!");
                
                DatabaseHandler.Instance.UpdateMonsterInstance(monster);
            }
        }

        private float GetDistance2D(Position pos1, Position pos2)
        {
            float dx = pos1.x - pos2.x;
            float dz = pos1.z - pos2.z;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }

        public MonsterInstance? GetMonster(int monsterId)
        {
            activeMonsters.TryGetValue(monsterId, out var monster);
            return monster;
        }

        public MonsterTemplate? GetMonsterTemplate(int templateId)
        {
            templates.TryGetValue(templateId, out var template);
            return template;
        }

        public List<MonsterInstance> GetAllMonsters()
        {
            return activeMonsters.Values.ToList();
        }

        public List<MonsterInstance> GetAliveMonsters()
        {
            return activeMonsters.Values.Where(m => m.isAlive).ToList();
        }

        public List<MonsterStateData> GetAllMonsterStates()
        {
            return activeMonsters.Values.Select(m => new MonsterStateData
            {
                id = m.id,
                templateId = m.templateId,
                name = m.template.name,
                level = m.template.level,
                currentHealth = m.currentHealth,
                maxHealth = m.template.maxHealth,
                position = m.position,
                isAlive = m.isAlive,
                inCombat = m.inCombat,
                targetPlayerId = m.targetPlayerId,
                isMoving = m.isMoving,
                prefabPath = m.template.prefabPath // 🆕 Envia prefab path para cliente
            }).ToList();
        }

        public void SaveAllMonsters()
        {
            foreach (var monster in activeMonsters.Values)
            {
                try
                {
                    DatabaseHandler.Instance.UpdateMonsterInstance(monster);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving monster {monster.id}: {ex.Message}");
                }
            }
        }

        public void ReloadFromConfig()
        {
            Console.WriteLine("🔄 Reloading monster configurations...");
            
            templates.Clear();
            LoadTemplatesFromConfig();
            
            foreach (var instance in activeMonsters.Values)
            {
                if (templates.TryGetValue(instance.templateId, out var template))
                {
                    instance.template = template;
                    Console.WriteLine($"✅ Updated {instance.template.name} (ID:{instance.id}) with new config");
                }
            }
            
            Console.WriteLine("✅ Monster configurations reloaded!");
        }

        public Dictionary<int, SpawnAreaStats> GetSpawnAreaStats()
        {
            var stats = new Dictionary<int, SpawnAreaStats>();
            var areas = SpawnAreaManager.Instance.GetAllAreas();

            foreach (var area in areas)
            {
                var areaMonsters = activeMonsters.Values
                    .Where(m => m.spawnAreaId == area.id)
                    .ToList();

                stats[area.id] = new SpawnAreaStats
                {
                    areaId = area.id,
                    areaName = area.name,
                    totalMonsters = areaMonsters.Count,
                    aliveMonsters = areaMonsters.Count(m => m.isAlive),
                    deadMonsters = areaMonsters.Count(m => !m.isAlive),
                    inCombat = areaMonsters.Count(m => m.inCombat)
                };
            }

            return stats;
        }
    }

    public class SpawnAreaInfo
    {
        public int areaId { get; set; }
        public string areaName { get; set; } = "";
        public List<int> monsterInstances { get; set; } = new List<int>();
    }

    public class SpawnAreaStats
    {
        public int areaId { get; set; }
        public string areaName { get; set; } = "";
        public int totalMonsters { get; set; }
        public int aliveMonsters { get; set; }
        public int deadMonsters { get; set; }
        public int inCombat { get; set; }
    }

    public class MonsterStateData
    {
        public int id { get; set; }
        public int templateId { get; set; }
        public string name { get; set; } = "";
        public int level { get; set; }
        public int currentHealth { get; set; }
        public int maxHealth { get; set; }
        public Position position { get; set; } = new Position();
        public bool isAlive { get; set; }
        public bool inCombat { get; set; }
        public string? targetPlayerId { get; set; }
        public bool isMoving { get; set; }
        public string prefabPath { get; set; } = ""; // 🆕 Path do prefab
    }
}