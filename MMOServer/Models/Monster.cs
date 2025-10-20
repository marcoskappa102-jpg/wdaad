namespace MMOServer.Models
{
    public class MonsterTemplate
    {
        public int id { get; set; }
        public string name { get; set; } = "";
        public int level { get; set; }
        public int maxHealth { get; set; }
        public int attackPower { get; set; }
        public int defense { get; set; }
        public int experienceReward { get; set; }
        public float attackSpeed { get; set; } = 1.5f;
        public float movementSpeed { get; set; } = 3.0f;
        public float aggroRange { get; set; } = 10.0f;
        
        // 🆕 Campos de spawn (mantidos para compatibilidade, mas não mais usados)
        public float spawnX { get; set; }
        public float spawnY { get; set; }
        public float spawnZ { get; set; }
        public float spawnRadius { get; set; } = 5.0f;
        public int respawnTime { get; set; } = 30;
        
        // 🆕 Sistema de Patrulha
        public string prefabPath { get; set; } = ""; // Caminho do prefab no Unity (Resources)
        public string patrolBehavior { get; set; } = "wander"; // wander, patrol, stationary
        public float patrolRadius { get; set; } = 10.0f; // Raio de patrulha
        public float patrolInterval { get; set; } = 5.0f; // Intervalo entre movimentos (segundos)
        public float idleTime { get; set; } = 3.0f; // Tempo parado em cada ponto
    }
    
    public class MonsterInstance
    {
        public int id { get; set; }
        public int templateId { get; set; }
        public MonsterTemplate template { get; set; } = new MonsterTemplate();
        
        public int currentHealth { get; set; }
        public Position position { get; set; } = new Position();
        public bool isAlive { get; set; } = true;
        
        // Combat state
        public string? targetPlayerId { get; set; }
        public bool inCombat { get; set; } = false;
        public float lastAttackTime { get; set; } = 0;
        public DateTime lastRespawn { get; set; } = DateTime.Now;
        
        // AI state
        public Position? targetPosition { get; set; }
        public bool isMoving { get; set; } = false;
        
        // 🆕 Informações de spawn por área
        public int spawnAreaId { get; set; } = 0;
        public int customRespawnTime { get; set; } = 0;
        
        // 🆕 Sistema de Patrulha
        public Position spawnPosition { get; set; } = new Position(); // Posição inicial (centro da patrulha)
        public float lastPatrolTime { get; set; } = 0f; // Última vez que mudou de destino
        public float lastIdleTime { get; set; } = 0f; // Última vez que ficou parado
        public bool isIdle { get; set; } = false; // Está parado no momento
        public int patrolPointIndex { get; set; } = 0; // Índice do ponto de patrulha (para patrol behavior)
        
        public int TakeDamage(int damage)
        {
            int actualDamage = Math.Max(1, damage - (template.defense / 3));
            currentHealth -= actualDamage;
            
            if (currentHealth <= 0)
            {
                currentHealth = 0;
                isAlive = false;
                lastRespawn = DateTime.Now;
            }
            
            return actualDamage;
        }
        
        public bool CanAttack(float currentTime)
        {
            return currentTime - lastAttackTime >= template.attackSpeed;
        }
        
        public void Attack(float currentTime)
        {
            lastAttackTime = currentTime;
        }
        
        public void Respawn()
        {
            isAlive = true;
            currentHealth = template.maxHealth;
            targetPlayerId = null;
            inCombat = false;
            targetPosition = null;
            isMoving = false;
            lastAttackTime = 0;
            lastRespawn = DateTime.Now;
            
            // 🆕 Reset patrulha
            lastPatrolTime = 0f;
            lastIdleTime = 0f;
            isIdle = false;
            patrolPointIndex = 0;
        }
    }
}