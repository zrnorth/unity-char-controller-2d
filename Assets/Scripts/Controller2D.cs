using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Controller2D : MonoBehaviour
{
    struct RaycastOrigins
    {
        public Vector2 topLeft, topRight, botLeft, botRight;
    }

    public int horizontalRayCount = 4;
    public int verticalRayCount = 4;
    float _horizontalRaySpacing, _verticalRaySpacing;
    // Consts
    const float _skinWidth = .015f;
    // Components
    BoxCollider2D _collider;
    RaycastOrigins _raycastOrigins;

    void Start() {
        _collider = GetComponent<BoxCollider2D>();
    }

    void Update() {
        UpdateRaycastOrigins();
        CalculateRaySpacing();

        for (int i = 0; i < verticalRayCount; i++) {
            Debug.DrawRay(
                _raycastOrigins.botLeft + Vector2.right * _verticalRaySpacing * i,
                Vector2.up * -2,
                Color.red);
        }
    }


    void UpdateRaycastOrigins() {
        Bounds bounds = _collider.bounds;
        bounds.Expand(_skinWidth * -2); // shrinking in the bounds by the width of the skin.
        _raycastOrigins.botLeft = new Vector2(bounds.min.x, bounds.min.y);
        _raycastOrigins.botRight = new Vector2(bounds.max.x, bounds.min.y);
        _raycastOrigins.topLeft = new Vector2(bounds.min.x, bounds.max.y);
        _raycastOrigins.topRight = new Vector2(bounds.max.x, bounds.max.y);
    }

    void CalculateRaySpacing() {
        Bounds bounds = _collider.bounds;
        bounds.Expand(_skinWidth * -2); // shrinking in the bounds by the width of the skin.

        // Need at least 2 rays in each direction
        horizontalRayCount = Mathf.Max(horizontalRayCount, 2);
        verticalRayCount = Mathf.Max(verticalRayCount, 2);

        _horizontalRaySpacing = bounds.size.y / (horizontalRayCount - 1);
        _verticalRaySpacing = bounds.size.x / (verticalRayCount - 1);
    }
}
