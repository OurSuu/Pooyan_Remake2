using System;
using UnityEngine;

/// <summary>
/// Horizontal projectile â€” no gravity. Handles balloon/shield/projectile collisions.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Arrow : MonoBehaviour
{
    [SerializeField] private float speed = GameConstants.ArrowSpeed;
    [SerializeField] private float destroyX = -12f;
    [SerializeField] private int damage = 1; // Explicit Arrow damage

    private float direction;
    private Action<Arrow> onDestroy;
    private bool isDeflected;

    public void Initialize(float moveDirection, Action<Arrow> destroyCallback)
    {
        direction = Mathf.Sign(moveDirection);
        if (direction == 0f) direction = -1f;
        onDestroy = destroyCallback;

        gameObject.tag = GameConstants.TagArrow;

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.linearVelocity = new Vector2(direction * speed, 0f);
        }
    }

    private void Update()
    {
        // Don't update position manually; let Rigidbody2D handle it so collisions work reliably.

        if (direction < 0f && transform.position.x < destroyX)
            DestroySelf();
        else if (direction > 0f && transform.position.x > -destroyX)
            DestroySelf();
        
        if (transform.position.y < -6f)
            DestroySelf();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Arrow hit: " + other.gameObject.name + " with tag: " + other.tag);

        if (other.CompareTag(GameConstants.TagBalloon))
        {
            var boss = other.GetComponentInParent<BossWolf>();
            if (boss != null)
            {
                boss.TryTakeArrowHit();
                DestroySelf();
                return;
            }

            var balloon = other.GetComponent<Balloon>() ?? other.GetComponentInParent<Balloon>();
            if (balloon != null)
                balloon.TakeDamage(damage); // Pass explicit damage
            DestroySelf();
            return;
        }

        if (other.CompareTag(GameConstants.TagShield))
        {
            if (isDeflected) return;
                        isDeflected = true;
            
            // หันหัวลูกธนูดิ่งลงพื้น (หมุน 90 องศา)
            transform.rotation = Quaternion.Euler(0, 0, 90f);
            
            AudioManager.Instance?.PlayShieldBlock();
            // à¸ªà¸°à¸—à¹‰à¸­à¸™à¸¥à¸‡à¸¥à¹ˆà¸²à¸‡à¹€à¸«à¸¡à¸·à¸­à¸™à¸à¸±à¸™
            speed = 0;
            direction = 0;
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(0f, -3f);
                rb.gravityScale = 1.5f;
            }
            return;
        }

        if (other.CompareTag(GameConstants.TagEnemy))
        {
            if (isDeflected) return; // Ignore wolf bodies if already deflected, allowing it to hit the balloon below!
                        isDeflected = true;
            
            // หันหัวลูกธนูดิ่งลงพื้น (หมุน 90 องศา)
            transform.rotation = Quaternion.Euler(0, 0, 90f);

            other.SendMessage("PlayDeflectAnimation", SendMessageOptions.DontRequireReceiver);

            speed = 0;
            direction = 0;
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // à¸ªà¸°à¸—à¹‰à¸­à¸™à¸¥à¸‡à¹€à¸žà¸·à¹ˆà¸­à¹ƒà¸«à¹‰à¹„à¸›à¹‚à¸”à¸™à¸¥à¸¹à¸à¹‚à¸›à¹ˆà¸‡à¸•à¸±à¸§à¸¥à¹ˆà¸²à¸‡à¹„à¸”à¹‰
                rb.linearVelocity = new Vector2(0f, -3f);
                rb.gravityScale = 1.5f;
            }
            return;
        }

        if (other.CompareTag(GameConstants.TagEnemyProjectile))
        {
            var proj = other.GetComponent<WolfProjectile>();
            if (proj != null) proj.DestroyByArrow();
            DestroySelf();
        }
    }

    private bool isDestroyed = false;
    private void DestroySelf()
    {
        if (isDestroyed) return;
        isDestroyed = true;
        onDestroy?.Invoke(this);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (!isDestroyed)
        {
            onDestroy?.Invoke(this);
        }
    }
}

