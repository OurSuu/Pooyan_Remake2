using UnityEngine;

public enum WolfProjectileType
{
    Rock,
    Fruit
}

/// <summary>
/// Rocks and fruits thrown by wolves — arrow destroys for points, player hit loses life.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class WolfProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float destroyY = -6f;

    private WolfProjectileType type;
    private Vector2 velocity;
    private bool destroyed;

    public void LaunchAtPlayer(WolfProjectileType projectileType)
    {
        type = projectileType;
        gameObject.tag = GameConstants.TagEnemyProjectile;

        var player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            Vector2 dir = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
            velocity = dir * speed;
        }
        else
        {
            velocity = Vector2.left * speed;
        }
    }

    public void LaunchDown(WolfProjectileType projectileType, float fallSpeed = 4f)
    {
        type = projectileType;
        gameObject.tag = GameConstants.TagEnemyProjectile;
        velocity = Vector2.down * fallSpeed;
    }

    private void Update()
    {
        transform.position += (Vector3)(velocity * Time.deltaTime);

        if (transform.position.y < destroyY || transform.position.x < -12f || transform.position.x > 12f)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (destroyed) return;

        if (other.CompareTag(GameConstants.TagPlayer))
        {
            // เช็คว่าโดนตรงขอบบน/ล่างของกระเช้าหรือไม่
            float diffY = transform.position.y - other.transform.position.y;
            if (Mathf.Abs(diffY) > 0.45f)
            {
                // โดนกระเช้า กันได้
                AudioManager.Instance?.PlayShieldBlock();
            }
            else
            {
                // โดนแม่หมูเต็มๆ
                other.GetComponent<PlayerController>()?.OnProjectileHit();
            }
            
            Destroy(gameObject);
            return;
        }
    }

    public void DestroyByArrow()
    {
        if (destroyed) return;
        destroyed = true;

        if (type == WolfProjectileType.Rock)
            ScoreManager.Instance?.AddRockDestroyScore(transform.position);
        else
            ScoreManager.Instance?.AddFruitDestroyScore(transform.position);

        AudioManager.Instance?.PlayRockDestroy();
        Destroy(gameObject);
    }
}
