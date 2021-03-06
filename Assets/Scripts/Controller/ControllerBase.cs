using UnityEngine;

public enum ForceDir
{
    Self, // Move() called by the object itself
    Below, Above,
    Left, Right
}

[RequireComponent(typeof(BoxCollider2D))]
public abstract class ControllerBase : MonoBehaviour
{
    // Collisions
    [SerializeField] private new BoxCollider2D collider;

    // Shell
    protected const float SHELL_WIDTH = 0.015f;

    // Rays
    protected RaycastOrigins raycastOrigins;

    protected uint HORIZONTAL_RAY_COUNT = 14;
    protected uint VERTICAL_RAY_COUNT = 14;

    protected float horizontalRaySpacing;
    protected float verticalRaySpacing;

    protected virtual void Start()
    {
        CalculateRaySpacing();
    }

    public abstract void Move(Vector2 velocity, ForceDir forceDir);

    protected void UpdateRaycastOrigins()
    {
        Bounds bounds = collider.bounds;
        bounds.Expand(-SHELL_WIDTH * 2);

        raycastOrigins.topLeft = new Vector2(bounds.min.x, bounds.max.y);
        raycastOrigins.topRight = new Vector2(bounds.max.x, bounds.max.y);
        raycastOrigins.bottomLeft = new Vector2(bounds.min.x, bounds.min.y);
        raycastOrigins.bottomRight = new Vector2(bounds.max.x, bounds.min.y);
    }

    private void CalculateRaySpacing()
    {
        Bounds bounds = collider.bounds;
        bounds.Expand(-SHELL_WIDTH * 2);

        horizontalRaySpacing = bounds.size.y / (HORIZONTAL_RAY_COUNT - 1);
        verticalRaySpacing= bounds.size.x / (VERTICAL_RAY_COUNT - 1);
    }

    protected struct RaycastOrigins
    {
        public Vector2 topLeft, topRight;
        public Vector2 bottomLeft, bottomRight;
    }
}
