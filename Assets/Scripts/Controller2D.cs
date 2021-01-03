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

    public struct CollisionInfo
    {
        public bool above, below, left, right;
        public void Reset() {
            above = below = left = right = false;
        }
    }

    public LayerMask collisionMask;
    public int horizontalRayCount = 4;
    public int verticalRayCount = 4;
    float _horizontalRaySpacing, _verticalRaySpacing;
    public CollisionInfo collisions;
    // Consts
    const float _skinWidth = .015f;
    // Components
    BoxCollider2D _collider;
    RaycastOrigins _raycastOrigins;

    void Start() {
        _collider = GetComponent<BoxCollider2D>();
        CalculateRaySpacing();
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

    void HorizontalCollisions(ref Vector3 velocity) {
        float directionX = Mathf.Sign(velocity.x);
        float rayLength = Mathf.Abs(velocity.x) + _skinWidth;

        for (int i = 0; i < horizontalRayCount; i++) {
            Vector2 rayOrigin = (directionX == -1) ? _raycastOrigins.botLeft : _raycastOrigins.botRight;
            rayOrigin += Vector2.up * (_horizontalRaySpacing * i);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, directionX * Vector2.right, rayLength, collisionMask);
            Debug.DrawRay(
                rayOrigin, Vector2.right * directionX * rayLength, Color.blue);

            if (hit) {
                velocity.x = (hit.distance - _skinWidth) * directionX;
                rayLength = hit.distance; // ensure that other rays can't go past this collision.
                collisions.left = (directionX == -1);
                collisions.right = (directionX == 1);
            }
        }
    }

    void VerticalCollisions(ref Vector3 velocity) {
        float directionY = Mathf.Sign(velocity.y);
        float rayLength = Mathf.Abs(velocity.y) + _skinWidth;

        for (int i = 0; i < verticalRayCount; i++) {
            Vector2 rayOrigin = (directionY == -1) ? _raycastOrigins.botLeft : _raycastOrigins.topLeft;
            rayOrigin += Vector2.right * (_verticalRaySpacing * i + velocity.x);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, directionY * Vector2.up, rayLength, collisionMask);

            Debug.DrawRay(
                rayOrigin, Vector2.up * directionY * rayLength, Color.blue);

            if (hit) {
                velocity.y = (hit.distance - _skinWidth) * directionY;
                rayLength = hit.distance; // ensure that other rays can't go past this collision.
                collisions.below = (directionY == -1);
                collisions.above = (directionY == 1);
            }
        }
    }

    public void Move(Vector3 velocity) {
        UpdateRaycastOrigins();
        collisions.Reset();
        if (velocity.x != 0) {
            HorizontalCollisions(ref velocity);
        }
        if (velocity.y != 0) {
            VerticalCollisions(ref velocity);
        }

        transform.Translate(velocity);
    }
}
