﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller2D : RaycastCollisionController
{
    public enum Facing
    {
        Left,
        Right,
    }

    public struct CollisionInfo
    {
        public bool above, below, left, right;
        public bool climbingSlope, descendingSlope;
        public float slopeAngle, slopeAnglePrevFrame;
        public Vector3 velocityOld;
        public Facing facing;
        public bool fallingThroughPlatform;
        public void Reset() {
            above = below = left = right = false;
            climbingSlope = descendingSlope = false;
            slopeAnglePrevFrame = slopeAngle;
            slopeAngle = 0f;
            velocityOld = new Vector3();
        }
    }

    public CollisionInfo collisions;

    public float maxClimbAngle = 80f;
    public float maxDescendAngle = 75f;

    Vector2 _playerInput; // Used for falling through platforms.

    public override void Start() {
        base.Start();
        collisions.facing = Facing.Left;
    }

    void ResetFallingThroughPlatform() {
        collisions.fallingThroughPlatform = false;
    }

    void HorizontalCollisions(ref Vector3 velocity) {
        float directionX = (collisions.facing == Facing.Right) ? 1 : -1;
        float rayLength = Mathf.Abs(velocity.x) + _skinWidth;
        if (Mathf.Abs(velocity.x) < _skinWidth) {
            rayLength = 2 * _skinWidth;
        }

        for (int i = 0; i < horizontalRayCount; i++) {
            Vector2 rayOrigin = (directionX == -1) ? _raycastOrigins.botLeft : _raycastOrigins.botRight;
            rayOrigin += Vector2.up * (_horizontalRaySpacing * i);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, directionX * Vector2.right, rayLength, collisionMask);
            Debug.DrawRay(
                rayOrigin, Vector2.right * directionX * rayLength, Color.blue);

            if (hit) {
                if (hit.distance == 0) continue; // if we're inside something don't modify velocity
                // note: angle between vector.up and the slope's normal is equal to the angle between 
                // vector.right and the slope.
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

                if (i == 0 && slopeAngle <= maxClimbAngle) {
                    // If we are descending some other slope, we're now climbing this one, so undo
                    // all the descending work we did earlier.
                    if (collisions.descendingSlope) {
                        collisions.descendingSlope = false;
                        velocity = collisions.velocityOld;
                    }
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

    // Same as above, but when you are going downwards to avoid jumping off the side of the slope.
    void DescendSlope(ref Vector3 velocity) {
        float directionX = Mathf.Sign(velocity.x);
        // cast a ray downwards from the point touching the slope
        Vector2 rayOrigin = (directionX == -1) ? _raycastOrigins.botRight : _raycastOrigins.botLeft;
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, Mathf.Infinity, collisionMask);
        if (hit) {
            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            // if flat surface or too steep, don't descend
            if (slopeAngle == 0f || slopeAngle > maxDescendAngle) return;
            // if going in opposite direction, we're ascending not descending
            if (Mathf.Sign(hit.normal.x) != directionX) return;
            // if we're too far from the slope, we're falling not descending
            if (hit.distance - _skinWidth > Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x)) return;

            float moveDistance = Mathf.Abs(velocity.x);
            float descendVelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
            velocity.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(velocity.x);
            velocity.y -= descendVelocityY;
            collisions.slopeAngle = slopeAngle;
            collisions.descendingSlope = true;
            // since we are descending a slope we are on the ground
            collisions.below = true;
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
                // if we are jumping up and our collision is with a Through platform, just pass through.
                if (hit.collider.tag == "Through") {
                    if (directionY == 1 || hit.distance == 0) {
                        continue;
                    }
                    if (collisions.fallingThroughPlatform) {
                        continue;
                    }
                    if (_playerInput.y == -1) {
                        collisions.fallingThroughPlatform = true;
                        Invoke("ResetFallingThroughPlatform", 0.5f);
                        continue;
                    }
                }

                velocity.y = (hit.distance - _skinWidth) * directionY;
                rayLength = hit.distance; // ensure that other rays can't go past this collision.
                if (collisions.climbingSlope) {
                    velocity.x = velocity.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(velocity.x);
                }
                collisions.below = (directionY == -1);
                collisions.above = (directionY == 1);
            }
        }

        if (collisions.climbingSlope) {
            // fire a horizontal ray from the pt on the y axis where we will be once we move
            // to check if there is a new slope there
            float directionX = Mathf.Sign(velocity.x);
            rayLength = Mathf.Abs(velocity.x) + _skinWidth;
            Vector2 rayOrigin = ((directionX == -1) ? _raycastOrigins.botLeft : _raycastOrigins.botRight) + (Vector2.up * velocity.y);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, directionX * Vector2.right, rayLength, collisionMask);
            if (hit) {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                if (slopeAngle != collisions.slopeAngle) {  // if we've hit a new slope
                    velocity.x = (hit.distance - _skinWidth) * directionX;
                    collisions.slopeAngle = slopeAngle;
                }
            }

        }
    }

    public void Move(Vector3 velocity, bool standingOnPlatform = false) {
        Move(velocity, Vector2.zero, standingOnPlatform);
    }

    public void Move(Vector3 velocity, Vector2 input, bool standingOnPlatform = false) {
        _playerInput = input;

        UpdateRaycastOrigins();
        collisions.Reset();
        collisions.velocityOld = velocity;
        if (velocity.x != 0) {
            collisions.facing = (Mathf.Sign(velocity.x) == 1) ? Facing.Right : Facing.Left;
        }
        if (velocity.y < 0) {
            DescendSlope(ref velocity);
        }
        HorizontalCollisions(ref velocity);

        if (velocity.y != 0) {
            VerticalCollisions(ref velocity);
        }

        transform.Translate(velocity);
        if (standingOnPlatform) {
            collisions.below = true;
        }
    }
}
