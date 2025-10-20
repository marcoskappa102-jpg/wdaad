using UnityEngine;
using TMPro;

public class MonsterController : MonoBehaviour
{
    [Header("Monster Info")]
    public int monsterId;
    public string monsterName;
    public int level;
    public int currentHealth;
    public int maxHealth;
    public bool isAlive = true;
    
    [Header("Position Settings")]
    public float characterHeightOffset = 1f;
    
    [Header("UI")]
    public TextMeshProUGUI nameText;
    public GameObject healthBarCanvas;
    public UnityEngine.UI.Image healthBarFill;
    public TextMeshProUGUI healthText;
    
    [Header("Visual")]
    public Color normalColor = Color.white;
    public Color aggroColor = Color.red;
    public Renderer modelRenderer;
    
    [Header("Billboard")]
    public bool enableBillboard = true;
    public Transform billboardTransform;

    // 🆕 ANIMAÇÕES
    [Header("Animation")]
    public Animator animator;

    private Vector3 serverPosition;
    private Vector3 displayPosition;
    private bool serverIsMoving;
    private bool inCombat;
    private Camera mainCamera;
    
    public float interpolationSpeed = 8f;

    private void Start()
    {
        mainCamera = Camera.main;
        
        serverPosition = transform.position;
        displayPosition = transform.position;
        
        if (modelRenderer == null)
        {
            modelRenderer = GetComponentInChildren<Renderer>();
        }

        // 🆕 Busca Animator se não atribuído
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
            if (animator == null)
            {
                Debug.LogWarning($"⚠️ Monster {monsterName}: No Animator found!");
            }
        }
        
        if (billboardTransform == null && healthBarCanvas != null)
        {
            billboardTransform = healthBarCanvas.transform;
        }
        
        ConfigureCollider();
        AdjustToTerrainHeight();
        UpdateHealthBar();
        UpdateNameDisplay();
    }

    private void ConfigureCollider()
    {
        var collider = GetComponent<Collider>();
        if (collider == null)
        {
            var capsule = gameObject.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0, 1, 0);
            capsule.radius = 0.5f;
            capsule.height = 2f;
            capsule.isTrigger = false;
        }
        
        gameObject.layer = LayerMask.NameToLayer("Monster");
    }

    private void Update()
    {
        InterpolateToServerPosition();
        UpdateBillboard();
        AdjustToTerrainHeight();
        UpdateAnimations(); // 🆕
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

    private void UpdateBillboard()
    {
        if (!enableBillboard || billboardTransform == null || mainCamera == null)
            return;

        billboardTransform.LookAt(billboardTransform.position + mainCamera.transform.rotation * Vector3.forward,
                                  mainCamera.transform.rotation * Vector3.up);
    }

    public void Initialize(int id, string name, int lvl, int hp, int maxHp, bool alive)
    {
        monsterId = id;
        monsterName = name;
        level = lvl;
        currentHealth = hp;
        maxHealth = maxHp;
        isAlive = alive;
        
        UpdateHealthBar();
        UpdateNameDisplay();
        AdjustToTerrainHeight();
    }
// <summary>
/// 🆕 Encontra o player mais próximo
/// </summary>
private PlayerController FindNearestPlayer()
{
    var players = GameObject.FindGameObjectsWithTag("Player");
    PlayerController nearest = null;
    float minDistance = float.MaxValue;
    
    foreach (var playerObj in players)
    {
        float distance = Vector3.Distance(transform.position, playerObj.transform.position);
        if (distance < minDistance)
        {
            minDistance = distance;
            nearest = playerObj.GetComponent<PlayerController>();
        }
    }
    
    return nearest;
}
public void UpdateFromServer(Vector3 position, int hp, bool alive, bool moving, bool combat)
{
    if (TerrainHelper.Instance != null)
    {
        position = TerrainHelper.Instance.ClampToGround(position, characterHeightOffset);
    }
    else
    {
        position.y = characterHeightOffset;
    }
    
    serverPosition = position;
    currentHealth = hp;
    isAlive = alive;
    serverIsMoving = moving;
    inCombat = combat;
    
  if (combat && isAlive)
{
    var player = FindNearestPlayer();
    if (player != null)
    {
        Vector3 directionToPlayer = player.transform.position - serverPosition;
        directionToPlayer.y = 0;
        
        if (directionToPlayer.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
        }
    }
}
    
    UpdateHealthBar();
    UpdateCombatVisual();
    
    if (!isAlive)
    {
        gameObject.SetActive(false);
    }
    else if (!gameObject.activeSelf)
    {
        gameObject.SetActive(true);
        AdjustToTerrainHeight();
    }
}

    private void InterpolateToServerPosition()
    {
        if (!isAlive) return;

        float distance = Vector3.Distance(displayPosition, serverPosition);

        if (distance > 10f)
        {
            displayPosition = serverPosition;
            transform.position = displayPosition;
            return;
        }

        displayPosition = Vector3.Lerp(displayPosition, serverPosition, interpolationSpeed * Time.deltaTime);
        transform.position = displayPosition;

        // 🆕 Rotaciona na direção do movimento
        if (serverIsMoving)
        {
            Vector3 direction = (serverPosition - displayPosition).normalized;
            if (direction.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
            }
        }
    }

    // 🆕 SISTEMA DE ANIMAÇÕES
    private void UpdateAnimations()
    {
        if (animator == null) return;

        animator.SetBool("isWalking", serverIsMoving && isAlive);
        animator.SetBool("inCombat", inCombat && isAlive);
        animator.SetBool("isDead", !isAlive);
    }

    private void UpdateHealthBar()
    {
        if (healthBarFill != null)
        {
            float healthPercent = maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
            healthBarFill.fillAmount = healthPercent;
        }

        if (healthText != null)
        {
            healthText.text = $"{currentHealth}/{maxHealth}";
        }

        if (healthBarCanvas != null)
        {
            healthBarCanvas.SetActive(isAlive && currentHealth < maxHealth);
        }
    }

    private void UpdateNameDisplay()
    {
        if (nameText != null)
        {
            nameText.text = $"{monsterName}\nLv.{level}";
        }
    }

    private void UpdateCombatVisual()
    {
        if (modelRenderer != null)
        {
            Color targetColor = inCombat ? aggroColor : normalColor;
            modelRenderer.material.color = Color.Lerp(
                modelRenderer.material.color,
                targetColor,
                Time.deltaTime * 5f
            );
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
    }
}