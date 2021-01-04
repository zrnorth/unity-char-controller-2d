using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformController : RaycastCollisionController
{
    public LayerMask passengerMask;
    public Vector3 move;

    List<PassengerMovement> passengerMovements;
    Dictionary<Transform, Controller2D> passengerComponentDictionary;

    public override void Start() {
        base.Start();
        passengerMovements = new List<PassengerMovement>();
        passengerComponentDictionary = new Dictionary<Transform, Controller2D>();
    }

    private void Update() {
        UpdateRaycastOrigins();
        Vector3 velocity = move * Time.deltaTime;
        CalculatePassengerMovement(velocity);
        MovePassengers(true);
        transform.Translate(velocity);
        MovePassengers(false);
    }

    void MovePassengers(bool beforeMovePlatform) {
        foreach (PassengerMovement p in passengerMovements) {
            if (p.moveBeforePlatform == beforeMovePlatform) {
                if (!passengerComponentDictionary.ContainsKey(p.transform)) {
                    passengerComponentDictionary.Add(p.transform, p.transform.GetComponent<Controller2D>());
                }
                passengerComponentDictionary[p.transform].Move(p.velocity, p.standingOnPlatform);
            }
        }
    }

    void CalculatePassengerMovement(Vector3 velocity) {
        var passengersMovedThisFrame = new HashSet<int>();
        passengerMovements.Clear();

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

                        passengerMovements.Add(new PassengerMovement(hit.transform, new Vector3(pushX, pushY), (directionY == 1), true));
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
                        float pushY = -_skinWidth; // Small cheat to tell the passenger to check below itself for grounding

                        passengerMovements.Add(new PassengerMovement(hit.transform, new Vector3(pushX, pushY), false, true));
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

                        passengerMovements.Add(new PassengerMovement(hit.transform, new Vector3(pushX, pushY), true, false));
                    }
                }
            }
        }
    }

    struct PassengerMovement
    {
        public Transform transform;
        public Vector3 velocity;
        public bool standingOnPlatform;
        public bool moveBeforePlatform; // if moving up, we want to move passenger first and then platform; if moving down, we do the opposite

        public PassengerMovement(Transform transform, Vector3 velocity, bool standingOnPlatform, bool moveBeforePlatform) {
            this.transform = transform;
            this.velocity = velocity;
            this.standingOnPlatform = standingOnPlatform;
            this.moveBeforePlatform = moveBeforePlatform;
        }
    }
}
