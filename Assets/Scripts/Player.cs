using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Controller2D))]
public class Player : MonoBehaviour
{
    public float jumpHeight = 4f;
    public float timeToJumpApex = 0.4f;
    public float accelerationTimeAirborne = 0.2f;
    public float accelerationTimeGrounded = 0.1f;
    public float moveSpeed = 6f;
    public bool wallSlidingEnabled = false;
    public Vector2 wallJumpClimb, wallJumpOff, wallLeap; // Different movements you can do on the wall
    public float wallSlideSpeedMax = 3f;
    public float wallStickTime = 0.25f;


    float _gravity; // This isn't directly settable; we calculate from jumpHeight and timeToJumpApex
    float _jumpVelocity; // similarly we calculate this
    Vector3 _velocity;
    float _velocityXSmoothing;
    float _wallUnstickTimer = 0f;
    Controller2D _controller;
    void Start() {
        _controller = GetComponent<Controller2D>();
        _gravity = -(2 * jumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        _jumpVelocity = Mathf.Abs(_gravity) * timeToJumpApex;
    }

    void Update() {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        int wallDirX = (_controller.collisions.left) ? -1 : 1;

        float targetVelocityX = input.x * moveSpeed;
        _velocity.x = Mathf.SmoothDamp(
            _velocity.x,
            targetVelocityX,
            ref _velocityXSmoothing,
            (_controller.collisions.below) ? accelerationTimeGrounded : accelerationTimeAirborne);

        bool slidingOnWall = false;
        if (wallSlidingEnabled) {
            if ((_controller.collisions.left || _controller.collisions.right) && !_controller.collisions.below && _velocity.y < 0) {
                slidingOnWall = true;

                if (_velocity.y < -wallSlideSpeedMax) {
                    _velocity.y = -wallSlideSpeedMax;
                }

                // If we're already stuck to the wall
                if (_wallUnstickTimer > 0) {
                    _velocityXSmoothing = 0;
                    _velocity.x = 0;
                    if (input.x != wallDirX && input.x != 0) { // we're not attempting to leap
                        _wallUnstickTimer -= Time.deltaTime;
                    } else {
                        _wallUnstickTimer = wallStickTime;
                    }
                } else {
                    _wallUnstickTimer = wallStickTime;
                }
            }
        }

        if (_controller.collisions.above || _controller.collisions.below) {
            _velocity.y = 0;
        }
        if (Input.GetKeyDown(KeyCode.Space)) {
            if (wallSlidingEnabled && slidingOnWall) { // Different jumps off the wall
                Vector2 movementToApply;
                // Different wall inputs. can either climb, jump or leap sideways
                if (input.x == wallDirX) {
                    movementToApply = wallJumpClimb;
                    Debug.Log("wall climb");
                } else if (input.x == 0) {
                    movementToApply = wallJumpOff;
                    Debug.Log("wall jump");

                } else {
                    movementToApply = wallLeap;
                    Debug.Log("wall leap");

                }
                _velocity.x = -wallDirX * movementToApply.x;
                _velocity.y = movementToApply.y;
            }
            if (_controller.collisions.below) { // Jump off ground
                _velocity.y = _jumpVelocity;
            }
        }


        _velocity.y += _gravity * Time.deltaTime;
        _controller.Move(_velocity * Time.deltaTime);
    }
}
