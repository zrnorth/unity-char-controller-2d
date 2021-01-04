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
        public bool climbingSlope;
        public float slopeAngle, slopeAnglePrevFrame;
        public void Reset() {
            above = below = left = right = false;
            climbingSlope = false;
            slopeAnglePrevFrame = slopeAngle;
            slopeAngle = 0f;
        }
    }

    public LayerMask collisionMask;
    public int horizontalRayCount = 4;
    public int verticalRayCount = 4;
    public float maxClimbAngle = 80f;
    public CollisionInfo collisions;

    float _horizontalRaySpacing, _verticalRaySpacing;
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

                // note: angle between vector.up and the slope's normal is equal to the angle between 
                // vector.right and the slope.
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

                if (i == 0 && slopeAngle <= maxClimbAngle) {
                    float distanceToSlopeStart = 0f;
                    // If we are starting a new slope climb
                    if (slopeAngle != collisions.slopeAnglePrevFrame) {
                        // Close the remaining distance to the slope this frame.
                        // Without this we will start climbing the slope on the tip of the ray instead
                        // of on the player's edge.
                        distanceToSlopeStart = hit.distance - _skinWidth;
                        velocity.x -= distanceToSlopeStart * directionX;
                    }
                    ClimbSlope(ref velocity, slopeAngle);
                    velocity.x += distanceToSlopeStart * directionX;
                }

                if (!collisions.climbingSlope || slopeAngle > maxClimbAngle) {
                    velocity.x = (hit.distance - _skinWidth) * directionX;
                    rayLength = hit.distance; // ensure that other rays can't go past this collision.

                    // if we are climbing a slope but we hit an angle we can't climb (i.e. obstacle)
                    if (collisions.climbingSlope) {
                        velocity.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x);
                    }
                    collisions.left = (directionX == -1);
                    collisions.right = (directionX == 1);
                }
            }
        }
    }

    // Translates the horizontal speed (x only) into movement up a slope of the given angle
    // such that the overall speed stays the same.
    void ClimbSlope(ref Vector3 velocity, float slopeAngle) {
        float moveDistance = Mathf.Abs(velocity.x);
        float climbVelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
        if (velocity.y > climbVelocityY) {
            // We are already moving upwards (probably jumping)
            return;
        }
        velocity.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(velocity.x);
        velocity.y = climbVelocityY;
        // Since we are climbing a slope, we're standing on the ground
        collisions.below = true;
        collisions.climbingSlope = true;
        collisions.slopeAngle = slopeAngle;
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
                if (collisions.climbingSlope) {
                    velocity.x = velocity.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(velocity.x);
                }
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
