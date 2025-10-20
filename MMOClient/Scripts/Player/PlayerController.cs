using UnityEngine;
using Newtonsoft.Json;
using TMPro;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float interpolationSpeed = 10f;
    public float characterHeightOffset = 0f;
	
    [Header("Character Info")]
    public string characterName;
    public string playerId;
    public bool isLocalPlayer;
    
    [Header("Combat")]
    public int currentHealth;
    public int maxHealth;
    public int level = 1;
    public bool isDead = false;
    public bool inCombat = false;
    
    [Header("UI")]
    public TextMeshProUGUI nameText;
    public GameObject healthBarCanvas;
    public UnityEngine.UI.Image healthBarFill;
    public TextMeshProUGUI healthText;
    public GameObject combatIcon;
    
    [Header("Billboard")]
    public bool enableBillboard = true;
    public Transform billboardTransform;

    [Header("Visual Feedback")]
    public GameObject attackEffectPrefab;
    public Transform attackEffectPoint;

    [Header("Animation")]
    public Animator animator;
    public float attackAnimationDuration = 1.0f;

    private Vector3 serverPosition;
    private Vector3 serverTargetPosition;
    private bool serverIsMoving = false;
    private bool serverInCombat = false;
    private Vector3 displayPosition;
    
    private CharacterController characterController;
    private MonsterController currentTarget;
    private Camera mainCamera;

    private float lastClickTime = 0f;
    private const float CLICK_COOLDOWN = 0.3f;
    private int currentTargetMonsterId = -1;

    private bool isAttacking = false;
    private float lastAttackTime = 0f;

    // ✅ NOVO: Controle de estado de animação
    private bool wasDeadLastFrame = false;
    private bool wasMovingLastFrame = false;
    private bool wasInCombatLastFrame = false;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
            characterController.center = new Vector3(0, 1, 0);
            characterController.radius = 0.5f;
            characterController.height = 2f;
        }
        
        characterController.enabled = true;

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
            if (animator == null)
            {
                Debug.LogWarning($"⚠️ PlayerController: No Animator found on {gameObject.name}!");
            }
        }
    }

    private void Start()
    {
        mainCamera = Camera.main;

        serverPosition = transform.position;
        displayPosition = transform.position;
        
        if (billboardTransform == null && healthBarCanvas != null)
        {
            billboardTransform = healthBarCanvas.transform;
        }

        if (healthBarCanvas != null)
        {
            var raycaster = healthBarCanvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (raycaster != null)
                Destroy(raycaster);
        }

        if (attackEffectPoint == null)
        {
            var point = new GameObject("AttackEffectPoint");
            point.transform.SetParent(transform);
            point.transform.localPosition = Vector3.up * 1.5f;
            attackEffectPoint = point.transform;
        }
        
        if (MessageHandler.Instance != null)
        {
            MessageHandler.Instance.OnPlayerAttack += HandlePlayerAttackEvent;
        }
        
        AdjustToTerrainHeight();
        UpdateHealthBar();
        
        if (combatIcon != null)
            combatIcon.SetActive(false);

        // ✅ Inicializa animador no estado correto
        InitializeAnimator();

        Debug.Log($"✅ PlayerController Start: {characterName} - CharacterController enabled: {characterController.enabled}");
    }

    private void Update()
    {
        if (isLocalPlayer && !isDead)
        {
            HandleInput();
        }

        InterpolateToServerPosition();
        UpdateAnimations();
        UpdateCombatVisual();
        UpdateBillboard();
        AdjustToTerrainHeight();
    }
	
    private void HandlePlayerAttackEvent(PlayerAttackData data)
    {
        if (data.playerId == playerId)
        {
            PlayAttackAnimation();
            
            var monster = GameObject.Find($"Monster_{data.monsterName}_{data.monsterId}");
            if (monster != null)
            {
                RotateTowards(monster.transform.position);
            }
        }
    }

    private void RotateTowards(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0;
        
        if (direction.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = targetRotation;
        }
    }

    private void AdjustToTerrainHeight()
    {
        if (TerrainHelper.Instance != null)
        {
            Vector3 pos = transform.position;
            pos.y = TerrainHelper.Instance.GetHeightAt(pos.x, pos.z) + characterHeightOffset;
            transform.position = pos;
        }
        else
        {
            Vector3 pos = transform.position;
            pos.y = characterHeightOffset;
            transform.position = pos;
        }
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (UIManager.IsPointerOverUI())
                return;

            if (Time.time - lastClickTime < CLICK_COOLDOWN)
                return;

            lastClickTime = Time.time;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            int monsterLayer = LayerMask.GetMask("Monster");

            if (Physics.Raycast(ray, out hit, 2000f, monsterLayer))
            {
                var monster = hit.collider.GetComponent<MonsterController>();
                
                if (monster != null && monster.isAlive)
                {
                    AttackMonster(monster);
                    return;
                }
            }

            if (TerrainHelper.Instance != null)
            {
                Vector3 hitPoint;
                if (TerrainHelper.Instance.RaycastTerrain(ray, out hitPoint))
                {
                    SendMoveRequestToServer(hitPoint);
                    
                    currentTargetMonsterId = -1;
                    currentTarget = null;
                    
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.HideTargetPanel();
                    }
                }
            }
        }
    }

    private void SendMoveRequestToServer(Vector3 targetPosition)
    {
        if (TerrainHelper.Instance != null)
        {
            targetPosition = TerrainHelper.Instance.ClampToGround(targetPosition, characterHeightOffset);
        }

        var message = new
        {
            type = "moveRequest",
            targetPosition = new
            {
                x = targetPosition.x,
                y = targetPosition.y,
                z = targetPosition.z
            }
        };

        string json = JsonConvert.SerializeObject(message);
        ClientManager.Instance.SendMessage(json);
    }

    private void AttackMonster(MonsterController monster)
    {
        currentTarget = monster;
        currentTargetMonsterId = monster.monsterId;
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowTargetPanel(monster);
        }
        
        var message = new
        {
            type = "attackMonster",
            monsterId = monster.monsterId
        };

        string json = JsonConvert.SerializeObject(message);
        ClientManager.Instance.SendMessage(json);
        
        Debug.Log($"⚔️ Attacking {monster.monsterName} (ID:{monster.monsterId})");
    }

    private void UpdateBillboard()
    {
        if (!enableBillboard || billboardTransform == null || mainCamera == null)
            return;

        billboardTransform.LookAt(billboardTransform.position + mainCamera.transform.rotation * Vector3.forward,
                                  mainCamera.transform.rotation * Vector3.up);
    }

    public void UpdateFromServer(Vector3 position, Vector3? targetPos, bool isMoving, int health, int maxHp, bool dead, bool combat)
    {
        if (TerrainHelper.Instance != null)
        {
            position = TerrainHelper.Instance.ClampToGround(position, characterHeightOffset);
            
            if (targetPos.HasValue)
            {
                Vector3 target = targetPos.Value;
                target = TerrainHelper.Instance.ClampToGround(target, characterHeightOffset);
                serverTargetPosition = target;
            }
        }
        else
        {
            position.y = characterHeightOffset;
            if (targetPos.HasValue)
            {
                Vector3 target = targetPos.Value;
                target.y = characterHeightOffset;
                serverTargetPosition = target;
            }
        }
        
        serverPosition = position;
        serverIsMoving = isMoving;
        serverInCombat = combat;
        currentHealth = health;
        maxHealth = maxHp;
        isDead = dead;
        inCombat = combat;
        
        if (!combat && currentTargetMonsterId != -1)
        {
            currentTargetMonsterId = -1;
            currentTarget = null;
            
            if (isLocalPlayer && UIManager.Instance != null)
            {
                UIManager.Instance.HideTargetPanel();
            }
        }

        if (combat && targetPos.HasValue && currentTarget != null)
        {
            Vector3 directionToCombat = targetPos.Value - serverPosition;
            directionToCombat.y = 0;
            
            if (directionToCombat.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToCombat);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 15f * Time.deltaTime);
            }
        }
        
        UpdateHealthBar();
        
        if (isLocalPlayer && currentTarget != null && UIManager.Instance != null)
        {
            UIManager.Instance.UpdateTargetHealth(currentTarget.currentHealth, currentTarget.maxHealth);
        }
    }

    private void InterpolateToServerPosition()
    {
        float distance = Vector3.Distance(displayPosition, serverPosition);

        if (distance > 5f)
        {
            displayPosition = serverPosition;
            transform.position = displayPosition;
            return;
        }

        displayPosition = Vector3.Lerp(displayPosition, serverPosition, interpolationSpeed * Time.deltaTime);

        Vector3 movement = displayPosition - transform.position;
        if (movement.magnitude > 0.001f)
        {
            if (characterController != null && characterController.enabled)
            {
                characterController.Move(movement);
            }
            else
            {
                transform.position = displayPosition;
            }
        }

        if (serverIsMoving)
        {
            Vector3 direction = (serverTargetPosition - serverPosition);
            direction.y = 0;
            direction.Normalize();
            
            if (direction.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
            }
        }
    }

    private void OnDestroy()
    {
        if (MessageHandler.Instance != null)
        {
            MessageHandler.Instance.OnPlayerAttack -= HandlePlayerAttackEvent;
        }
    }

    // ========================================
    // ✅ SISTEMA DE ANIMAÇÕES CORRIGIDO
    // ========================================

    /// <summary>
    /// Inicializa o Animator no estado correto
    /// </summary>
    private void InitializeAnimator()
    {
        if (animator == null)
        {
            Debug.LogWarning($"⚠️ {characterName}: Animator is null!");
            return;
        }

        // Reseta todos os parâmetros
        animator.SetBool("isWalking", false);
        animator.SetBool("inCombat", false);
        animator.SetBool("isDead", false);
        
        // Se tiver trigger de ataque, reseta
        if (HasParameter(animator, "Attack"))
        {
            animator.ResetTrigger("Attack");
        }

        // Define estado inicial
        if (isDead)
        {
            animator.SetBool("isDead", true);
        }

        Debug.Log($"🎬 {characterName}: Animator initialized - Dead:{isDead}, Walking:{serverIsMoving}, Combat:{serverInCombat}");
    }

    /// <summary>
    /// Atualiza animações baseado no estado do servidor
    /// </summary>
    private void UpdateAnimations()
    {
        if (animator == null)
            return;

        // ✅ PRIORIDADE: Morte tem prioridade máxima
        if (isDead)
        {
            // Se acabou de morrer
            if (!wasDeadLastFrame)
            {
                Debug.Log($"💀 {characterName}: Setting death animation");
                animator.SetBool("isDead", true);
                animator.SetBool("isWalking", false);
                animator.SetBool("inCombat", false);
                
                if (HasParameter(animator, "Attack"))
                {
                    animator.ResetTrigger("Attack");
                }
            }
            
            wasDeadLastFrame = true;
            wasMovingLastFrame = false;
            wasInCombatLastFrame = false;
            return; // Não processa mais nada se está morto
        }

        // ✅ Se estava morto e agora não está mais (respawn)
        if (wasDeadLastFrame && !isDead)
        {
            Debug.Log($"✨ {characterName}: Respawned - Resetting animator");
            
            // FORÇA o reset completo
            animator.SetBool("isDead", false);
            animator.SetBool("isWalking", false);
            animator.SetBool("inCombat", false);
            
            if (HasParameter(animator, "Attack"))
            {
                animator.ResetTrigger("Attack");
            }

            // Força transição para Idle
            animator.Play("Idle", 0, 0f);
            
            wasDeadLastFrame = false;
        }

        // ✅ Atualiza animações normais (só se não estiver morto)
        bool shouldWalk = serverIsMoving && !isDead;
        bool shouldCombat = serverInCombat && !isDead;

        // Detecta mudanças de estado
        if (shouldWalk != wasMovingLastFrame)
        {
            Debug.Log($"🚶 {characterName}: Walking changed to {shouldWalk}");
            animator.SetBool("isWalking", shouldWalk);
            wasMovingLastFrame = shouldWalk;
        }

        if (shouldCombat != wasInCombatLastFrame)
        {
            Debug.Log($"⚔️ {characterName}: Combat changed to {shouldCombat}");
            animator.SetBool("inCombat", shouldCombat);
            wasInCombatLastFrame = shouldCombat;
        }

        // Atualiza flags de ataque
        if (isAttacking && Time.time - lastAttackTime >= attackAnimationDuration)
        {
            isAttacking = false;
        }
    }

    /// <summary>
    /// Toca animação de ataque
    /// </summary>
    public void PlayAttackAnimation()
    {
        if (animator == null || isDead)
            return;

        if (HasParameter(animator, "Attack"))
        {
            animator.SetTrigger("Attack");
            isAttacking = true;
            lastAttackTime = Time.time;
            
            Debug.Log($"▶️ {characterName}: Playing attack animation");
        }
        else
        {
            Debug.LogWarning($"⚠️ {characterName}: Animator has no 'Attack' trigger!");
        }
    }

    /// <summary>
    /// Verifica se o Animator tem um parâmetro específico
    /// </summary>
    private bool HasParameter(Animator anim, string paramName)
    {
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName)
                return true;
        }
        return false;
    }

    private void UpdateCombatVisual()
    {
        if (combatIcon != null)
        {
            combatIcon.SetActive(serverInCombat && !isDead);
        }
        
        if (isLocalPlayer && UIManager.Instance != null)
        {
            UIManager.Instance.ShowCombatStatus(serverInCombat && !isDead);
        }
    }

    public void UpdateHealthBar()
    {
        if (healthBarFill != null)
        {
            float healthPercent = maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
            healthBarFill.fillAmount = healthPercent;
            
            if (healthPercent > 0.5f)
                healthBarFill.color = Color.green;
            else if (healthPercent > 0.25f)
                healthBarFill.color = Color.yellow;
            else
                healthBarFill.color = Color.red;
        }

        if (healthText != null)
        {
            healthText.text = $"{currentHealth}/{maxHealth}";
        }

        if (healthBarCanvas != null && !isLocalPlayer)
        {
            healthBarCanvas.SetActive(currentHealth < maxHealth || isDead);
        }
        
        if (isLocalPlayer && UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealthBar(currentHealth, maxHealth);
        }
    }

    public void ShowDamage(int damage, bool isCritical)
    {
        if (DamageTextManager.Instance != null)
        {
            DamageTextManager.Instance.ShowDamage(
                transform.position + Vector3.up * 2f,
                damage,
                isCritical
            );
        }

        if (attackEffectPrefab != null && attackEffectPoint != null)
        {
            var effect = Instantiate(attackEffectPrefab, attackEffectPoint.position, Quaternion.identity);
            Destroy(effect, 1f);
        }
    }

    public void Initialize(string id, string name, bool local, int hp, int maxHp, int lvl)
    {
        playerId = id;
        characterName = name;
        isLocalPlayer = local;
        currentHealth = hp;
        maxHealth = maxHp;
        level = lvl;

        if (nameText != null)
        {
            nameText.text = $"{characterName}\nLv.{level}";
        }

        if (isLocalPlayer)
        {
            gameObject.tag = "Player";
            gameObject.layer = LayerMask.NameToLayer("Player");
            
            var collider = GetComponent<Collider>();
            if (collider != null && !(collider is CharacterController))
            {
                collider.enabled = false;
            }

            if (characterController != null)
            {
                characterController.enabled = true;
            }
        }
        else
        {
            gameObject.layer = LayerMask.NameToLayer("OtherPlayers");
            
            if (characterController != null)
            {
                characterController.enabled = false;
            }
        }
        
        AdjustToTerrainHeight();
        UpdateHealthBar();
        InitializeAnimator();
    }

    /// <summary>
    /// Chamado quando o player morre
    /// </summary>
    public void OnDeath()
    {
        Debug.Log($"💀 {characterName}: OnDeath called");
        
        isDead = true;
        inCombat = false;
        currentTargetMonsterId = -1;
        
        if (animator != null)
        {
            Debug.Log($"💀 {characterName}: Setting death animation");
            animator.SetBool("isDead", true);
            animator.SetBool("inCombat", false);
            animator.SetBool("isWalking", false);
            
            if (HasParameter(animator, "Attack"))
            {
                animator.ResetTrigger("Attack");
            }
        }

        if (combatIcon != null)
        {
            combatIcon.SetActive(false);
        }

        if (isLocalPlayer)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowRespawnButton();
                UIManager.Instance.HideTargetPanel();
                UIManager.Instance.ShowCombatStatus(false);
            }
            currentTarget = null;
        }

        wasDeadLastFrame = true;
    }

    /// <summary>
    /// Chamado quando o player renasce
    /// </summary>
    public void OnRespawn(Vector3 position)
    {
        Debug.Log($"✨ {characterName}: OnRespawn called at ({position.x:F1}, {position.y:F1}, {position.z:F1})");
        
        isDead = false;
        inCombat = false;
        currentTargetMonsterId = -1;
        
        if (TerrainHelper.Instance != null)
        {
            position = TerrainHelper.Instance.ClampToGround(position, characterHeightOffset);
        }
        else
        {
            position.y = characterHeightOffset;
        }
        
        serverPosition = position;
        displayPosition = position;
        transform.position = position;
        
        if (animator != null)
        {
            Debug.Log($"✨ {characterName}: Resetting animator after respawn");
            
            // FORÇA reset completo
            animator.SetBool("isDead", false);
            animator.SetBool("inCombat", false);
            animator.SetBool("isWalking", false);
            
            if (HasParameter(animator, "Attack"))
            {
                animator.ResetTrigger("Attack");
            }

            // Força estado Idle imediatamente
            animator.Play("Idle", 0, 0f);
        }
        
        UpdateHealthBar();
        
        if (isLocalPlayer && UIManager.Instance != null)
        {
            UIManager.Instance.ShowCombatStatus(false);
            UIManager.Instance.HideDeathPanel();
        }

        wasDeadLastFrame = false;
        wasMovingLastFrame = false;
        wasInCombatLastFrame = false;
    }
}