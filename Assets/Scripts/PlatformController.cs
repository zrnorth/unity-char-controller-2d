using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformController : RaycastCollisionController
{
    public LayerMask passengerMask;
    public Vector3[] localWaypoints;
    public float speed;
    public bool cyclic; // Should we reverse directions at the last waypoint, or do a circuit?
    public float waitTime; // Time to wait at each waypoint
    [Range(0, 2)]
    public float easeAmount; // 0 is no easing

    float _nextMoveTime;
    int _fromWaypointIndex;
    float _percentBetweenWaypoints;
    Vector3[] _globalWaypoints;
    List<PassengerMovement> _passengerMovements;
    Dictionary<Transform, Controller2D> _passengerComponentDictionary;

    public override void Start() {
        base.Start();
        _globalWaypoints = new Vector3[localWaypoints.Length];
        for (int i = 0; i < localWaypoints.Length; i++) {
            _globalWaypoints[i] = localWaypoints[i] + transform.position;
        }
        _passengerMovements = new List<PassengerMovement>();
        _passengerComponentDictionary = new Dictionary<Transform, Controller2D>();
    }

    private void Update() {
        UpdateRaycastOrigins();
        Vector3 velocity = CalculatePlatformMovement();
        CalculatePassengerMovement(velocity);
        MovePassengers(true);
        transform.Translate(velocity);
        MovePassengers(false);
    }

    float Ease(float x) {
        float a = easeAmount + 1;
        return Mathf.Pow(x, a) / (Mathf.Pow(x, a) + Mathf.Pow(1 - x, a));
    }

    Vector3 CalculatePlatformMovement() {
        if (Time.time < _nextMoveTime) {
            return Vector3.zero;
        }

        _fromWaypointIndex %= _globalWaypoints.Length; // reset to 0 if we've reached the end
        int toWaypointIdx = (_fromWaypointIndex + 1) % _globalWaypoints.Length;
        float distanceBetweenWaypoints = Vector3.Distance(_globalWaypoints[_fromWaypointIndex], _globalWaypoints[toWaypointIdx]);
        _percentBetweenWaypoints += Time.deltaTime * speed / distanceBetweenWaypoints;
        _percentBetweenWaypoints = Mathf.Clamp01(_percentBetweenWaypoints);
        float easedPercentBetweenWaypoints = Ease(_percentBetweenWaypoints);

        Vector3 newPos = Vector3.Lerp(_globalWaypoints[_fromWaypointIndex], _globalWaypoints[toWaypointIdx], easedPercentBetweenWaypoints);

        // If we have reached the next waypoint
        if (_percentBetweenWaypoints >= 1f) {
            _percentBetweenWaypoints = 0;
            _fromWaypointIndex++;
            // if we've reached the last waypoint, turn around if not cyclic
            if (!cyclic) {
                if (_fromWaypointIndex >= _globalWaypoints.Length - 1) {
                    _fromWaypointIndex = 0;
                    System.Array.Reverse(_globalWaypoints);
                }
            }
            // if it is cyclic, just increment like normal and we'll return to 0th index

            _nextMoveTime = Time.time + waitTime;
        }

        return newPos - transform.position;
    }

    void MovePassengers(bool beforeMovePlatform) {
        foreach (PassengerMovement p in _passengerMovements) {
            if (p.moveBeforePlatform == beforeMovePlatform) {
                if (!_passengerComponentDictionary.ContainsKey(p.transform)) {
                    _passengerComponentDictionary.Add(p.transform, p.transform.GetComponent<Controller2D>());
                }
                _passengerComponentDictionary[p.transform].Move(p.velocity, p.standingOnPlatform);
            }
        }
    }

    void CalculatePassengerMovement(Vector3 velocity) {
        var passengersMovedThisFrame = new HashSet<int>();
        _passengerMovements.Clear();

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

                        _passengerMovements.Add(new PassengerMovement(hit.transform, new Vector3(pushX, pushY), (directionY == 1), true));
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

                        _passengerMovements.Add(new PassengerMovement(hit.transform, new Vector3(pushX, pushY), false, true));
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

                        _passengerMovements.Add(new PassengerMovement(hit.transform, new Vector3(pushX, pushY), true, false));
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
    private void OnDrawGizmos() {
        if (localWaypoints != null) {
            Gizmos.color = Color.red;
            float size = 0.3f;

            for (int i = 0; i < localWaypoints.Length; i++) {
                Vector3 globalWaypointPos = (Application.isPlaying) ? _globalWaypoints[i] : localWaypoints[i] + transform.position;
                Gizmos.DrawLine(globalWaypointPos - Vector3.up * size, globalWaypointPos + Vector3.up * size);
                Gizmos.DrawLine(globalWaypointPos - Vector3.left * size, globalWaypointPos + Vector3.left * size);
            }
        }
    }
}
