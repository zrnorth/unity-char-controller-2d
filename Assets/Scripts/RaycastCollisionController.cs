using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class RaycastCollisionController : MonoBehaviour
{
    protected struct RaycastOrigins
    {
        public Vector2 topLeft, topRight, botLeft, botRight;
    }

    public LayerMask collisionMask;
    public int horizontalRayCount = 4;
    public int verticalRayCount = 4;

    protected const float _skinWidth = .015f;
    protected float _horizontalRaySpacing, _verticalRaySpacing;

    // Components
    protected BoxCollider2D _collider;
    protected RaycastOrigins _raycastOrigins;

    public virtual void Start() {
        _collider = GetComponent<BoxCollider2D>();
        CalculateRaySpacing();
    }

    protected void UpdateRaycastOrigins() {
        Bounds bounds = _collider.bounds;
        bounds.Expand(_skinWidth * -2); // shrinking in the bounds by the width of the skin.
        _raycastOrigins.botLeft = new Vector2(bounds.min.x, bounds.min.y);
        _raycastOrigins.botRight = new Vector2(bounds.max.x, bounds.min.y);
        _raycastOrigins.topLeft = new Vector2(bounds.min.x, bounds.max.y);
        _raycastOrigins.topRight = new Vector2(bounds.max.x, bounds.max.y);
    }

    protected void CalculateRaySpacing() {
        Bounds bounds = _collider.bounds;
        bounds.Expand(_skinWidth * -2); // shrinking in the bounds by the width of the skin.

        // Need at least 2 rays in each direction
        horizontalRayCount = Mathf.Max(horizontalRayCount, 2);
        verticalRayCount = Mathf.Max(verticalRayCount, 2);

        _horizontalRaySpacing = bounds.size.y / (horizontalRayCount - 1);
        _verticalRaySpacing = bounds.size.x / (verticalRayCount - 1);
    }
}
