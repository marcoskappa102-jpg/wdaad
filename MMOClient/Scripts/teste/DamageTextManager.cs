using UnityEngine;
using TMPro;
using System.Collections;

public class DamageTextManager : MonoBehaviour
{
    public static DamageTextManager Instance { get; private set; }

    [Header("Prefab")]
    public GameObject damageTextPrefab;

    [Header("Settings")]
    public float floatSpeed = 1f;
    public float floatDistance = 1.5f;
    public float fadeSpeed = 1.2f;
    public float lifetime = 2f;
    
    [Header("Animation")]
    public bool enableBounce = true;
    public float bounceScale = 1.3f;
    public AnimationCurve bounceCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Colors")]
    public Color normalDamageColor = Color.white;
    public Color criticalDamageColor = new Color(1f, 0.2f, 0.2f); // Vermelho
    public Color healColor = new Color(0.2f, 1f, 0.2f); // Verde
    public Color missColor = new Color(0.7f, 0.7f, 0.7f); // Cinza
    public Color magicDamageColor = new Color(0.5f, 0.5f, 1f); // Azul

    [Header("Random Offset")]
    public float horizontalSpread = 0.5f;
    public float verticalSpread = 0.3f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Mostra dano normal ou crítico
    /// </summary>
    public void ShowDamage(Vector3 worldPosition, int damage, bool isCritical)
    {
        ShowText(worldPosition, damage.ToString(), isCritical ? criticalDamageColor : normalDamageColor, isCritical);
    }

    /// <summary>
    /// Mostra dano com tipo customizado
    /// </summary>
    public void ShowDamage(Vector3 worldPosition, int damage, bool isCritical, DamageType damageType)
    {
        Color color = damageType switch
        {
            DamageType.Physical => isCritical ? criticalDamageColor : normalDamageColor,
            DamageType.Magical => magicDamageColor,
            _ => normalDamageColor
        };

        string text = damage.ToString();
        if (isCritical) text += "!";

        ShowText(worldPosition, text, color, isCritical);
    }

    /// <summary>
    /// Mostra cura
    /// </summary>
    public void ShowHeal(Vector3 worldPosition, int amount)
    {
        ShowText(worldPosition, $"+{amount}", healColor, false);
    }

    /// <summary>
    /// Mostra MISS
    /// </summary>
    public void ShowMiss(Vector3 worldPosition)
    {
        ShowText(worldPosition, "MISS", missColor, false, 32);
    }

    /// <summary>
    /// Mostra BLOCK/DODGE/etc
    /// </summary>
    public void ShowStatus(Vector3 worldPosition, string status, Color color)
    {
        ShowText(worldPosition, status, color, false, 32);
    }

    /// <summary>
    /// Mostra XP ganho
    /// </summary>
    public void ShowExperience(Vector3 worldPosition, int xp)
    {
        ShowText(worldPosition, $"+{xp} XP", new Color(1f, 1f, 0.3f), false, 28);
    }

    /// <summary>
    /// Método genérico para mostrar texto
    /// </summary>
    private void ShowText(Vector3 worldPosition, string text, Color color, bool isBig, int fontSize = 36)
    {
        if (damageTextPrefab == null)
        {
            Debug.LogWarning("DamageTextPrefab is not assigned!");
            return;
        }

        // Adiciona offset aleatório para evitar sobreposição
        Vector3 randomOffset = new Vector3(
            Random.Range(-horizontalSpread, horizontalSpread),
            Random.Range(0f, verticalSpread),
            Random.Range(-horizontalSpread, horizontalSpread)
        );

        GameObject textObj = Instantiate(damageTextPrefab, worldPosition + randomOffset, Quaternion.identity);
        TextMeshProUGUI textComponent = textObj.GetComponentInChildren<TextMeshProUGUI>();

        if (textComponent != null)
        {
            textComponent.text = text;
            textComponent.color = color;
            
            // Tamanho baseado em tipo
            if (isBig)
            {
                textComponent.fontSize = fontSize * 1.5f; // 50% maior para crítico
            }
            else
            {
                textComponent.fontSize = fontSize;
            }
        }

        StartCoroutine(AnimateDamageText(textObj, textComponent, isBig));
    }

    private IEnumerator AnimateDamageText(GameObject textObj, TextMeshProUGUI textComponent, bool isBig)
    {
        float elapsed = 0f;
        Vector3 startPos = textObj.transform.position;
        
        // Críticos sobem mais alto
        float actualFloatDistance = isBig ? floatDistance * 1.3f : floatDistance;
        Vector3 endPos = startPos + Vector3.up * actualFloatDistance;

        // Curva de movimento (para cima com desaceleração)
        AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        Color startColor = textComponent != null ? textComponent.color : Color.white;
        Color endColor = startColor;
        endColor.a = 0f;

        // Escala inicial e máxima
        Vector3 startScale = textObj.transform.localScale;
        Vector3 maxScale = startScale * bounceScale;

        while (elapsed < lifetime)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / lifetime;

            // Move para cima com curva suave
            float curvedProgress = moveCurve.Evaluate(progress);
            textObj.transform.position = Vector3.Lerp(startPos, endPos, curvedProgress);

            // Fade out (começa a desaparecer nos últimos 50% do tempo)
            if (progress > 0.5f)
            {
                float fadeProgress = (progress - 0.5f) * 2f; // 0 a 1
                if (textComponent != null)
                {
                    textComponent.color = Color.Lerp(startColor, endColor, fadeProgress * fadeSpeed);
                }
            }

            // Bounce effect (só no início)
            if (enableBounce && progress < 0.3f)
            {
                float bounceProgress = progress / 0.3f; // 0 a 1 nos primeiros 30%
                float bounce = bounceCurve.Evaluate(bounceProgress);
                textObj.transform.localScale = Vector3.Lerp(maxScale, startScale, bounce);
            }

            // Sempre olha para câmera (billboard effect)
            if (Camera.main != null)
            {
                textObj.transform.LookAt(Camera.main.transform);
                textObj.transform.Rotate(0, 180, 0);
            }

            yield return null;
        }

        Destroy(textObj);
    }
}

/// <summary>
/// Tipos de dano para diferentes cores
/// </summary>
public enum DamageType
{
    Physical,
    Magical,
    True // Dano verdadeiro (ignora defesa)
}