using UnityEngine;

/// <summary>
/// Entry point — call from scene or main menu to begin gameplay.
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private bool autoStart = true;

    private void Start()
    {
        if (!autoStart) return;

        ScoreManager.Instance?.ResetScore();
        GameManager.Instance?.StartGame();
    }
}
