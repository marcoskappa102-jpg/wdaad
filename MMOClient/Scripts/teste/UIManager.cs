using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Newtonsoft.Json;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD Principal")]
    public GameObject hudPanel;
    public TextMeshProUGUI characterNameText;
    public TextMeshProUGUI levelText;
    
    [Header("Barras de Status - USAR SLIDER")]
    public Slider healthBarSlider;
    public TextMeshProUGUI healthText;
    
    public Slider manaBarSlider;
    public TextMeshProUGUI manaText;
    
    public Slider expBarSlider;
    public TextMeshProUGUI expText;
    
    [Header("Status Points")]
    public GameObject statusPointsPanel;
    public TextMeshProUGUI availablePointsText;
    public Button strButton;
    public Button intButton;
    public Button dexButton;
    public Button vitButton;
    public Button toggleStatsButton;
    
    [Header("Stats Display")]
    public TextMeshProUGUI strText;
    public TextMeshProUGUI intText;
    public TextMeshProUGUI dexText;
    public TextMeshProUGUI vitText;
    public TextMeshProUGUI atkText;
    public TextMeshProUGUI defText;
    public TextMeshProUGUI aspdText;

    [Header("Death/Respawn")]
    public GameObject deathPanel;
    public Button respawnButton;
    public TextMeshProUGUI deathMessageText;

    [Header("Combat Log")]
    public GameObject combatLogPanel;
    public TextMeshProUGUI combatLogText;
    public ScrollRect combatLogScrollRect;
    private System.Collections.Generic.List<string> combatMessages = new System.Collections.Generic.List<string>();
    private const int MAX_COMBAT_MESSAGES = 50;

    [Header("Level Up Effect")]
    public GameObject levelUpPanel;
    public TextMeshProUGUI levelUpText;
    public ParticleSystem levelUpParticles;

    [Header("Target Info")]
    public GameObject targetPanel;
    public TextMeshProUGUI targetNameText;
    public Slider targetHealthBarSlider;
    public TextMeshProUGUI targetHealthText;
    public TextMeshProUGUI targetLevelText;

    [Header("Combat Status")]
    public GameObject combatStatusIcon;
    public TextMeshProUGUI combatStatusText;

    [Header("Attack Cooldown")]
    public GameObject cooldownPanel;
    public Image cooldownFillImage;
    public TextMeshProUGUI cooldownText;

    private CharacterData localCharacterData;
    private bool statsVisible = false;
    private MonsterController currentTarget;
    private float attackCooldownTimer = 0f;
    private float maxAttackCooldown = 1.0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Configura botões
        if (respawnButton != null)
            respawnButton.onClick.AddListener(OnRespawnButtonClick);
        
        if (toggleStatsButton != null)
            toggleStatsButton.onClick.AddListener(ToggleStatsPanel);
        
        if (strButton != null)
            strButton.onClick.AddListener(() => AddStatusPoint("str"));
        if (intButton != null)
            intButton.onClick.AddListener(() => AddStatusPoint("int"));
        if (dexButton != null)
            dexButton.onClick.AddListener(() => AddStatusPoint("dex"));
        if (vitButton != null)
            vitButton.onClick.AddListener(() => AddStatusPoint("vit"));

        // Inicializa sliders
        InitializeSliders();

        // Inicializa UI
        HideDeathPanel();
        HideLevelUpPanel();
        HideTargetPanel();
        if (statusPointsPanel != null)
            statusPointsPanel.SetActive(false);
        if (combatStatusIcon != null)
            combatStatusIcon.SetActive(false);
        if (cooldownPanel != null)
            cooldownPanel.SetActive(false);

        LoadCharacterData();
        RegisterEvents();
    }

    private void Update()
    {
        // ✅ Hotkey para stats
        if (Input.GetKeyDown(KeyCode.C))
        {
            ToggleStatsPanel();
        }

        // ✅ Atualiza cooldown visual
        UpdateCooldownDisplay();
    }

    private void InitializeSliders()
    {
        if (healthBarSlider != null)
        {
            healthBarSlider.minValue = 0;
            healthBarSlider.maxValue = 1;
            healthBarSlider.value = 1;
        }
        
        if (manaBarSlider != null)
        {
            manaBarSlider.minValue = 0;
            manaBarSlider.maxValue = 1;
            manaBarSlider.value = 1;
        }
        
        if (expBarSlider != null)
        {
            expBarSlider.minValue = 0;
            expBarSlider.maxValue = 1;
            expBarSlider.value = 0;
        }

        if (targetHealthBarSlider != null)
        {
            targetHealthBarSlider.minValue = 0;
            targetHealthBarSlider.maxValue = 1;
            targetHealthBarSlider.value = 1;
        }

        if (cooldownFillImage != null)
        {
            cooldownFillImage.fillAmount = 0f;
        }
    }

    private void RegisterEvents()
    {
        if (MessageHandler.Instance != null)
        {
            MessageHandler.Instance.OnCombatResult += HandleCombatResult;
            MessageHandler.Instance.OnLevelUp += HandleLevelUp;
            MessageHandler.Instance.OnPlayerDeath += HandlePlayerDeath;
            MessageHandler.Instance.OnAttackStarted += HandleAttackStarted;
        }
    }

    private void LoadCharacterData()
    {
        if (PlayerPrefs.HasKey("PendingCharacterData"))
        {
            string jsonData = PlayerPrefs.GetString("PendingCharacterData");
            try
            {
                var selectData = JsonUtility.FromJson<SelectCharacterResponseData>(jsonData);
                if (selectData.character != null)
                {
                    UpdateLocalCharacterData(selectData.character);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load character data: {e.Message}");
            }
        }
    }

    public void UpdateLocalCharacterData(CharacterData character)
    {
        localCharacterData = character;
        maxAttackCooldown = character.attackSpeed;
        UpdateHUD();
    }

    public void UpdateHUD()
    {
        if (localCharacterData == null) return;

        if (characterNameText != null)
            characterNameText.text = localCharacterData.nome;

        if (levelText != null)
            levelText.text = $"Level {localCharacterData.level}";

        UpdateHealthBar(localCharacterData.health, localCharacterData.maxHealth);
        UpdateManaBar(localCharacterData.mana, localCharacterData.maxMana);
        UpdateExpBar(localCharacterData.experience, localCharacterData.GetRequiredExp());

        UpdateStatsDisplay();
        UpdateStatusPointsDisplay();
    }

    private void UpdateStatsDisplay()
    {
        if (localCharacterData == null) return;

        if (strText != null)
            strText.text = $"STR: {localCharacterData.strength}";
        if (intText != null)
            intText.text = $"INT: {localCharacterData.intelligence}";
        if (dexText != null)
            dexText.text = $"DEX: {localCharacterData.dexterity}";
        if (vitText != null)
            vitText.text = $"VIT: {localCharacterData.vitality}";
        if (atkText != null)
            atkText.text = $"ATK: {localCharacterData.attackPower}";
        if (defText != null)
            defText.text = $"DEF: {localCharacterData.defense}";
        if (aspdText != null)
            aspdText.text = $"ASPD: {localCharacterData.attackSpeed:F2}s";
    }

    private void UpdateStatusPointsDisplay()
    {
        if (availablePointsText != null)
        {
            int points = localCharacterData?.statusPoints ?? 0;
            availablePointsText.text = $"Pontos: {points}";
            
            if (points > 0 && toggleStatsButton != null)
            {
                toggleStatsButton.GetComponent<Image>().color = Color.yellow;
            }
            else if (toggleStatsButton != null)
            {
                toggleStatsButton.GetComponent<Image>().color = Color.white;
            }
        }

        bool hasPoints = (localCharacterData?.statusPoints ?? 0) > 0;
        if (strButton != null) strButton.interactable = hasPoints;
        if (intButton != null) intButton.interactable = hasPoints;
        if (dexButton != null) dexButton.interactable = hasPoints;
        if (vitButton != null) vitButton.interactable = hasPoints;
    }

    private void ToggleStatsPanel()
    {
        if (statusPointsPanel == null) return;
        
        statsVisible = !statsVisible;
        statusPointsPanel.SetActive(statsVisible);
    }

    private void AddStatusPoint(string stat)
    {
        if (localCharacterData == null || localCharacterData.statusPoints <= 0)
            return;

        var message = new
        {
            type = "addStatusPoint",
            stat = stat
        };

        string json = JsonConvert.SerializeObject(message);
        ClientManager.Instance.SendMessage(json);
        
        AddCombatLog($"<color=cyan>Adicionando ponto em {stat.ToUpper()}...</color>");
    }

    // ==================== BARRAS DE STATUS ====================

    public void UpdateHealthBar(int current, int max)
    {
        if (healthBarSlider != null)
        {
            float healthPercent = max > 0 ? (float)current / max : 0f;
            healthBarSlider.value = healthPercent;
            
            var fillImage = healthBarSlider.fillRect?.GetComponent<Image>();
            if (fillImage != null)
            {
                if (healthPercent > 0.5f)
                    fillImage.color = Color.green;
                else if (healthPercent > 0.25f)
                    fillImage.color = Color.yellow;
                else
                    fillImage.color = Color.red;
            }
        }

        if (healthText != null)
        {
            healthText.text = $"{current}/{max}";
        }

        if (localCharacterData != null)
        {
            localCharacterData.health = current;
            localCharacterData.maxHealth = max;
        }
    }

    public void UpdateManaBar(int current, int max)
    {
        if (manaBarSlider != null)
        {
            float manaPercent = max > 0 ? (float)current / max : 0f;
            manaBarSlider.value = manaPercent;
        }

        if (manaText != null)
        {
            manaText.text = $"{current}/{max}";
        }

        if (localCharacterData != null)
        {
            localCharacterData.mana = current;
            localCharacterData.maxMana = max;
        }
    }

    public void UpdateExpBar(int current, int required)
    {
        if (expBarSlider != null)
        {
            float expPercent = required > 0 ? (float)current / required : 0f;
            expBarSlider.value = expPercent;
        }

        if (expText != null)
        {
            expText.text = $"{current}/{required}";
        }

        if (localCharacterData != null)
        {
            localCharacterData.experience = current;
        }
    }

    // ==================== TARGET ====================

    public void ShowTargetPanel(MonsterController monster)
    {
        currentTarget = monster;
        
        if (targetPanel != null)
        {
            targetPanel.SetActive(true);
        }

        if (targetNameText != null)
        {
            targetNameText.text = monster.monsterName;
        }
        
        if (targetLevelText != null)
        {
            targetLevelText.text = $"Lv. {monster.level}";
        }

        UpdateTargetHealth(monster.currentHealth, monster.maxHealth);
    }

    public void UpdateTargetHealth(int current, int max)
    {
        if (targetHealthBarSlider != null)
        {
            float healthPercent = max > 0 ? (float)current / max : 0f;
            targetHealthBarSlider.value = healthPercent;
        }

        if (targetHealthText != null)
        {
            targetHealthText.text = $"{current}/{max}";
        }
        
        if (current <= 0)
        {
            HideTargetPanel();
        }
    }

    public void HideTargetPanel()
    {
        currentTarget = null;
        if (targetPanel != null)
        {
            targetPanel.SetActive(false);
        }
    }

    // ==================== COMBAT ====================

    private void HandleAttackStarted(AttackStartedData data)
    {
        ShowCombatStatus(true);
        AddCombatLog($"<color=yellow>⚔️ Atacando {data.monsterName}...</color>");
        
        // ✅ Inicia cooldown visual
        attackCooldownTimer = maxAttackCooldown;
        if (cooldownPanel != null)
            cooldownPanel.SetActive(true);
    }

    private void HandleCombatResult(CombatResultData data)
    {
        string localPlayerId = ClientManager.Instance.PlayerId;
        bool isLocalPlayer = (data.attackerId == localPlayerId || data.targetId == localPlayerId);
        
        if (data.attackerType == "player" && data.targetType == "monster")
        {
            if (data.attackerId == localPlayerId)
            {
                string critText = data.isCritical ? " <color=red>CRÍTICO!</color>" : "";
                string damageColor = data.isCritical ? "red" : "orange";
                
                if (data.damage > 0)
                {
                    string logMessage = $"Você causou <color={damageColor}>{data.damage}</color> de dano{critText}";

                    if (data.targetDied)
                    {
                        logMessage += " <color=lime>💀 (MORTO)</color>";
                        ShowCombatStatus(false);
                        
                        if (data.experienceGained > 0)
                        {
                            logMessage += $" +<color=cyan>{data.experienceGained} XP</color>";
                            
                            if (localCharacterData != null)
                            {
                                localCharacterData.experience += data.experienceGained;
                                UpdateExpBar(localCharacterData.experience, localCharacterData.GetRequiredExp());
                            }
                        }

                        // ✅ Para cooldown visual
                        if (cooldownPanel != null)
                            cooldownPanel.SetActive(false);
                    }
                    else
                    {
                        // ✅ Reinicia cooldown visual
                        attackCooldownTimer = maxAttackCooldown;
                    }

                    AddCombatLog(logMessage);
                }
                else
                {
                    AddCombatLog("<color=gray>❌ ERROU!</color>");
                }
            }
        }
        else if (data.attackerType == "monster" && data.targetType == "player")
        {
            if (isLocalPlayer)
            {
                if (data.damage > 0)
                {
                    string critText = data.isCritical ? " <color=red>CRÍTICO!</color>" : "";
                    AddCombatLog($"<color=red>👹 Você recebeu {data.damage} de dano{critText}</color>");
                    UpdateHealthBar(data.remainingHealth, localCharacterData.maxHealth);
                }
                else
                {
                    AddCombatLog("<color=lime>✨ Monstro ERROU!</color>");
                }
            }
        }
    }

    private void HandleLevelUp(LevelUpData data)
    {
        string localPlayerId = ClientManager.Instance.PlayerId;
        
        if (data.playerId == localPlayerId && localCharacterData != null)
        {
            localCharacterData.level = data.newLevel;
            localCharacterData.maxHealth = data.newStats.maxHealth;
            localCharacterData.health = data.newStats.maxHealth;
            localCharacterData.maxMana = data.newStats.maxMana;
            localCharacterData.mana = data.newStats.maxMana;
            localCharacterData.attackPower = data.newStats.attackPower;
            localCharacterData.magicPower = data.newStats.magicPower;
            localCharacterData.defense = data.newStats.defense;
            localCharacterData.attackSpeed = data.newStats.attackSpeed;
            localCharacterData.strength = data.newStats.strength;
            localCharacterData.intelligence = data.newStats.intelligence;
            localCharacterData.dexterity = data.newStats.dexterity;
            localCharacterData.vitality = data.newStats.vitality;
            localCharacterData.statusPoints = data.statusPoints;
            localCharacterData.experience = data.experience;
            
            maxAttackCooldown = data.newStats.attackSpeed;
            
            UpdateHUD();
            ShowLevelUpEffect(data.characterName, data.newLevel);
        }
    }

    private void HandlePlayerDeath(PlayerDeathData data)
    {
        string localPlayerId = ClientManager.Instance.PlayerId;
        
        if (data.playerId == localPlayerId)
        {
            ShowCombatStatus(false);
            AddCombatLog("<color=red>💀 Você morreu!</color>");
            
            if (cooldownPanel != null)
                cooldownPanel.SetActive(false);
        }
    }

    public void ShowCombatStatus(bool inCombat)
    {
        if (combatStatusIcon != null)
        {
            combatStatusIcon.SetActive(inCombat);
        }
        
        if (combatStatusText != null)
        {
            combatStatusText.text = inCombat ? "⚔️ EM COMBATE" : "";
        }
    }

    // ==================== COOLDOWN VISUAL ====================

    private void UpdateCooldownDisplay()
    {
        if (attackCooldownTimer > 0)
        {
            attackCooldownTimer -= Time.deltaTime;
            
            if (cooldownFillImage != null)
            {
                float percent = Mathf.Clamp01(attackCooldownTimer / maxAttackCooldown);
                cooldownFillImage.fillAmount = percent;
            }

            if (cooldownText != null)
            {
                cooldownText.text = $"{attackCooldownTimer:F1}s";
            }

            if (attackCooldownTimer <= 0 && cooldownPanel != null)
            {
                cooldownPanel.SetActive(false);
            }
        }
    }

    // ==================== LEVEL UP ====================

    public void ShowLevelUpEffect(string characterName, int newLevel)
    {
        if (levelUpPanel != null && levelUpText != null)
        {
            levelUpPanel.SetActive(true);
            levelUpText.text = $"⭐ LEVEL UP! ⭐\n{characterName}\nLevel {newLevel}";
            
            if (levelUpParticles != null)
            {
                levelUpParticles.Play();
            }
            
            Invoke(nameof(HideLevelUpPanel), 3f);
        }

        AddCombatLog($"<color=yellow>⭐ {characterName} alcançou o Level {newLevel}!</color>");
    }

    private void HideLevelUpPanel()
    {
        if (levelUpPanel != null)
        {
            levelUpPanel.SetActive(false);
        }
    }

    // ==================== DEATH/RESPAWN ====================

    public void ShowRespawnButton()
    {
        if (deathPanel != null)
        {
            deathPanel.SetActive(true);
        }

        if (deathMessageText != null)
        {
            deathMessageText.text = "Você morreu!\nClique para renascer.";
        }
    }

    public void HideDeathPanel()
    {
        if (deathPanel != null)
        {
            deathPanel.SetActive(false);
        }
    }

    private void OnRespawnButtonClick()
    {
        var message = new
        {
            type = "respawnRequest"
        };

        string json = JsonConvert.SerializeObject(message);
        ClientManager.Instance.SendMessage(json);

        HideDeathPanel();
        AddCombatLog("<color=lime>✨ Renascendo...</color>");
    }

    // ==================== COMBAT LOG ====================

    public void AddCombatLog(string message)
    {
        if (combatLogText == null) return;

        string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
        string formattedMessage = $"[{timestamp}] {message}";
        
        combatMessages.Insert(0, formattedMessage);
        
        if (combatMessages.Count > MAX_COMBAT_MESSAGES)
        {
            combatMessages.RemoveAt(combatMessages.Count - 1);
        }
        
        combatLogText.text = string.Join("\n", combatMessages);
        
        if (combatLogScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            combatLogScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    // ✅ Verifica se clicou em UI
    public static bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private void OnDestroy()
    {
        if (MessageHandler.Instance != null)
        {
            MessageHandler.Instance.OnCombatResult -= HandleCombatResult;
            MessageHandler.Instance.OnLevelUp -= HandleLevelUp;
            MessageHandler.Instance.OnPlayerDeath -= HandlePlayerDeath;
            MessageHandler.Instance.OnAttackStarted -= HandleAttackStarted;
        }
    }
}