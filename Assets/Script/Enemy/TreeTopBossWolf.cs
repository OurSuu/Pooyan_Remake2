using UnityEngine;

/// <summary>
/// Stage 3+ tree-top boss — walks and throws fruit downward.
/// </summary>
public class TreeTopBossWolf : MonoBehaviour
{
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float walkMinX = -3f;
    [SerializeField] private float walkMaxX = 3f;
    [SerializeField] private float fruitThrowInterval = 2.5f;
    [SerializeField] private GameObject fruitPrefab;
    [SerializeField] private float topY = 4f;

    private int walkDirection = 1;
    private float fruitTimer;

    private void Start()
    {
        transform.position = new Vector3(walkMinX, topY, 0f);
        fruitTimer = fruitThrowInterval;
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

        UpdateWalk();
        UpdateFruitThrow();
    }

    private void UpdateWalk()
    {
        transform.position += Vector3.right * (walkDirection * walkSpeed * Time.deltaTime);

        if (transform.position.x >= walkMaxX)
            walkDirection = -1;
        else if (transform.position.x <= walkMinX)
            walkDirection = 1;
    }

    private void UpdateFruitThrow()
    {
        fruitTimer -= Time.deltaTime;
        if (fruitTimer > 0f || fruitPrefab == null) return;

        fruitTimer = fruitThrowInterval;
        var fruit = Instantiate(fruitPrefab, transform.position, Quaternion.identity);
        fruit.GetComponent<WolfProjectile>()?.LaunchDown(WolfProjectileType.Fruit);
    }

    public void Activate(bool active)
    {
        gameObject.SetActive(active);
    }
}
