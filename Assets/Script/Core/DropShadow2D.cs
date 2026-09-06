using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class DropShadow2D : MonoBehaviour
{
    [Header("Shadow Settings")]
    public Vector2 shadowOffset = new Vector2(0.15f, -0.15f);
    public Color shadowColor = new Color(0f, 0f, 0f, 0.5f);
    public int sortingOrderOffset = -1;

    private SpriteRenderer parentRenderer;
    private SpriteRenderer shadowRenderer;

    void Start()
    {
        parentRenderer = GetComponent<SpriteRenderer>();

        GameObject shadowGO = new GameObject("DropShadow");
        shadowGO.transform.SetParent(transform);
        shadowGO.transform.localPosition = shadowOffset;
        shadowGO.transform.localRotation = Quaternion.identity;
        shadowGO.transform.localScale = Vector3.one;

        shadowRenderer = shadowGO.AddComponent<SpriteRenderer>();
        shadowRenderer.sprite = parentRenderer.sprite;
        shadowRenderer.color = shadowColor;
        shadowRenderer.sortingLayerID = parentRenderer.sortingLayerID;
        shadowRenderer.sortingLayerName = parentRenderer.sortingLayerName;
        shadowRenderer.sortingOrder = parentRenderer.sortingOrder + sortingOrderOffset;
    }

    void LateUpdate()
    {
        if (shadowRenderer == null || parentRenderer == null) return;
        
        // Sync animation frames
        if (shadowRenderer.sprite != parentRenderer.sprite)
        {
            shadowRenderer.sprite = parentRenderer.sprite;
        }
        
        // Sync flips
        shadowRenderer.flipX = parentRenderer.flipX;
        shadowRenderer.flipY = parentRenderer.flipY;
    }
}
