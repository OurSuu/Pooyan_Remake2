using System.Collections;
using UnityEngine;

/// <summary>
/// Balloon HP, visual feedback, pop on zero HP.
/// </summary>
public class Balloon : MonoBehaviour
{
    [SerializeField] private Sprite[] damageStages;
    [SerializeField] private float flashInterval = 0.15f;

    private int maxHP;
    private int currentHP;
    private bool isBossFlash;
    private SpriteRenderer spriteRenderer;
    private Wolf owner;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        gameObject.tag = GameConstants.TagBalloon;
    }

    public void Initialize(Wolf wolf, int hp, bool bossFlash = false)
    {
        owner = wolf;
        maxHP = Mathf.Max(1, hp);
        currentHP = maxHP;
        isBossFlash = bossFlash;

        if (isBossFlash)
            StartCoroutine(BossFlashRoutine());

        UpdateVisual();
    }

    public bool TakeDamage(int damage = 1)
    {
        if (currentHP <= 0) return true;

        currentHP -= damage;
        UpdateVisual();

        if (currentHP <= 0)
        {
            Pop();
            return true;
        }

        return false;
    }

    public void PopInstant() => Pop();

    public void ReleaseInstant()
    {
        StopAllCoroutines();
        // Unparent so it flies up independently of the falling wolf
        transform.SetParent(null);
        StartCoroutine(ReleaseAnimationRoutine());
    }

    private IEnumerator ReleaseAnimationRoutine()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        float duration = 1.0f;
        float time = 0f;
        Vector3 startPos = transform.position;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            // Fly up quickly
            transform.position = startPos + new Vector3(0, t * 5f, 0); 
            
            // Fade out
            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = Mathf.Lerp(1f, 0f, t);
                spriteRenderer.color = c;
            }
            yield return null;
        }

        Destroy(gameObject);
    }

    private void Pop()
    {
        StopAllCoroutines();
        ScoreManager.Instance?.AddBalloonPopScore(transform.position);
        AudioManager.Instance?.PlayBalloonPop();
        owner?.OnBalloonPopped();
        
        StartCoroutine(PopAnimationRoutine());
    }

    private IEnumerator PopAnimationRoutine()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        float duration = 0.15f;
        float time = 0f;
        Vector3 startScale = transform.localScale;
        Vector3 endScale = startScale * 1.5f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            transform.localScale = Vector3.Lerp(startScale, endScale, t);
            
            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = Mathf.Lerp(1f, 0f, t);
                spriteRenderer.color = c;
            }
            yield return null;
        }

        Destroy(gameObject);
    }

    private void UpdateVisual()
    {
        if (spriteRenderer == null) return;

        float scale = 1f - (maxHP - currentHP) * 0.05f;
        transform.localScale = Vector3.one * scale;

        if (damageStages == null || damageStages.Length == 0) return;

        int stageIndex = maxHP <= 1
            ? 0
            : Mathf.Clamp(Mathf.FloorToInt((1f - (float)currentHP / maxHP) * (damageStages.Length - 1)), 0, damageStages.Length - 1);

        spriteRenderer.sprite = damageStages[stageIndex];
    }

    private IEnumerator BossFlashRoutine()
    {
        var normalColor = Color.white;
        var flashColor = new Color(1f, 0.2f, 0.2f);

        while (true)
        {
            spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(flashInterval);
            spriteRenderer.color = normalColor;
            yield return new WaitForSeconds(flashInterval);
        }
    }
}
