using UnityEngine;

/// <summary>
/// Meat weapon â€” lobbed in a parabolic arc (matching original Pooyan arcade).
/// Piercing, bypasses shields, meat combo scoring.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class MeatWeapon : MonoBehaviour
{
    [Header("Parabolic Arc")]
    [SerializeField] private float lobSpeedX = -13f;        // Horizontal speed (forward)
    [SerializeField] private float lobSpeedY = 5f;         // Upward lob force
    [SerializeField] private float gravityScale = 1.5f;    // How fast it curves down
    [SerializeField] private float destroyY = -6f;

    private readonly System.Collections.Generic.HashSet<Wolf> hitWolves = new();

    private void Awake()
    {
        gameObject.tag = GameConstants.TagMeat;
        var rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = gravityScale;
        rb.linearVelocity = new Vector2(lobSpeedX, lobSpeedY);
    }

    private void Update()
    {
        if (transform.position.y < destroyY)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(GameConstants.TagArrow) || other.CompareTag(GameConstants.TagMeat))
            return;

        var wolf = other.GetComponent<Wolf>() ?? other.GetComponentInParent<Wolf>();
        var balloon = other.GetComponent<Balloon>() ?? other.GetComponentInParent<Balloon>();

        // If it hit an empty balloon (no wolf attached)
        if (balloon != null && wolf == null)
        {
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0.5f);
            return;
        }

        // If it hit a wolf (either its body collider OR its balloon collider)
        if (wolf != null && !hitWolves.Contains(wolf))
        {
            hitWolves.Add(wolf);
            ScoreManager.Instance?.AddMeatComboScore(wolf.transform.position);
            AudioManager.Instance?.PlayMeatHit();

            // Cascade combo chain: reduce horizontal velocity so it falls steeper, small bump up
            var rbHit = GetComponent<Rigidbody2D>();
            // ไม่มีการเด้งหรือลดความเร็วใดๆ เนื้อจะร่วงตกลงมาตามแรงโน้มถ่วงเป็นเส้นโค้งปกติ (Piercing Arc)

            wolf.OnMeatHit();
        }
    }
}




