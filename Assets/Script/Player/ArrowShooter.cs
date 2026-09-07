using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns arrows with max-2-on-screen limit and cooldown.
/// Input is driven by PlayerController.
/// </summary>
public class ArrowShooter : MonoBehaviour
{
    [Header("Shooting")]
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private Transform shootPoint;
    [SerializeField] private float shootCooldown = 0.15f;
    [SerializeField] private float shootDirection = -1f;

    private float shootTimer;
    private bool canShoot = true;
    private readonly List<Arrow> activeArrows = new();

    private void Update()
    {
        shootTimer -= Time.deltaTime;
    }

    public void SetCanShoot(bool value) => canShoot = value;

    public bool TryShoot()
    {
        if (!canShoot || shootTimer > 0f) return false;

        CleanupDestroyed();
        if (activeArrows.Count >= GameConstants.MaxArrowsOnScreen) return false;
        if (arrowPrefab == null || shootPoint == null) return false;

        var go = Instantiate(arrowPrefab, shootPoint.position, Quaternion.identity);
        var arrow = go.GetComponent<Arrow>();
        if (arrow != null)
        {
            arrow.Initialize(shootDirection, UnregisterArrow);
            activeArrows.Add(arrow);
        }

        shootTimer = shootCooldown;
        AudioManager.Instance?.PlayArrowFire();
        return true;
    }

    private void UnregisterArrow(Arrow arrow)
    {
        activeArrows.Remove(arrow);
    }

    private void CleanupDestroyed()
    {
        activeArrows.RemoveAll(a => a == null);
    }
}

