namespace MMOServer.Models
{
    public class Player
    {
        public string sessionId { get; set; } = "";
        public Character character { get; set; } = new Character();
        public Position position { get; set; } = new Position();
        public bool isOnline { get; set; } = true;
        
        // Movimento
        public bool isMoving { get; set; } = false;
        public Position? targetPosition { get; set; }
        
        // Combate
        public int? targetMonsterId { get; set; }
        public bool inCombat { get; set; } = false;
        
        // ✅ CORREÇÃO #18: Inicializa com -999 para permitir ataque imediato
        public float lastAttackTime { get; set; } = -999f;
        
        // ✅ CORREÇÃO #19: Verifica cooldown baseado em ASPD do personagem
        public bool CanAttack(float currentTime)
        {
            if (character.isDead)
                return false;
            
            // Se nunca atacou, permite imediatamente
            if (lastAttackTime < 0)
                return true;
                
            float cooldown = character.attackSpeed; // Usa ASPD do personagem
            float timeSinceLastAttack = currentTime - lastAttackTime;
            
            return timeSinceLastAttack >= cooldown;
        }
        
        public void Attack(float currentTime)
        {
            lastAttackTime = currentTime;
        }
        
        // ✅ CORREÇÃO #20: Cancela combate (chamado quando clica em outro lugar)
        public void CancelCombat()
        {
            inCombat = false;
            targetMonsterId = null;
            // NÃO limpa isMoving nem targetPosition - o movimento continua
        }
    }
}