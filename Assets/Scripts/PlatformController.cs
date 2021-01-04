using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformController : RaycastCollisionController
{
    public LayerMask passengerMask;
    public Vector3 move;


    public override void Start() {
        base.Start();
    }

    private void Update() {
        UpdateRaycastOrigins();
        Vector3 velocity = move * Time.deltaTime;
        MovePassengers(velocity);
        transform.Translate(velocity);
    }

    void MovePassengers(Vector3 velocity) {
        var passengersMovedThisFrame = new HashSet<int>();

        float directionX = Mathf.Sign(velocity.x);
        float directionY = Mathf.Sign(velocity.y);

        // Vertically moving platform
        if (velocity.y != 0) {
            float rayLength = Mathf.Abs(velocity.y) + _skinWidth;

            for (int i = 0; i < verticalRayCount; i++) {
                Vector2 rayOrigin = (directionY == -1) ? _raycastOrigins.botLeft : _raycastOrigins.topLeft;
                rayOrigin += Vector2.right * (_verticalRaySpacing * i);
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, passengerMask);

                if (hit) {
                    if (!passengersMovedThisFrame.Contains(hit.transform.GetInstanceID())) {
                        passengersMovedThisFrame.Add(hit.transform.GetInstanceID());
                        float pushX = (directionY == 1) ? velocity.x : 0;
                        float pushY = velocity.y - (hit.distance - _skinWidth) * directionY;

                        hit.transform.Translate(new Vector3(pushX, pushY));
                    }
                }
            }
        }

        // Horizontally moving platform
        if (velocity.x != 0) {
            float rayLength = Mathf.Abs(velocity.x) + _skinWidth;

            for (int i = 0; i < horizontalRayCount; i++) {
                Vector2 rayOrigin = (directionX == -1) ? _raycastOrigins.botLeft : _raycastOrigins.botRight;
                rayOrigin += Vector2.up * (_horizontalRaySpacing * i);
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, directionX * Vector2.right, rayLength, passengerMask);
                if (hit) {
                    if (!passengersMovedThisFrame.Contains(hit.transform.GetInstanceID())) {
                        passengersMovedThisFrame.Add(hit.transform.GetInstanceID());
                        float pushX = velocity.x - (hit.distance - _skinWidth) * directionX;
                        float pushY = 0f;

                        hit.transform.Translate(new Vector3(pushX, pushY));
                    }
                }
            }
        }

        // Passenger on top of a horizontal or downward moving platform
        if (directionY == -1 || (velocity.y == 0 && velocity.x != 0)) {
            float rayLength = _skinWidth * 2;

            for (int i = 0; i < verticalRayCount; i++) {
                Vector2 rayOrigin = _raycastOrigins.topLeft;
                rayOrigin += Vector2.right * (_verticalRaySpacing * i);
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up, rayLength, passengerMask);

                if (hit) {
                    if (!passengersMovedThisFrame.Contains(hit.transform.GetInstanceID())) {
                        passengersMovedThisFrame.Add(hit.transform.GetInstanceID());
                        float pushX = velocity.x;
                        float pushY = velocity.y;

                        hit.transform.Translate(new Vector3(pushX, pushY));
                    }
                }
            }
        }
    }
}
