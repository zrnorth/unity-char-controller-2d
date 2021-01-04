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

    float _gravity; // This isn't directly settable; we calculate from jumpHeight and timeToJumpApex
    float _jumpVelocity; // similarly we calculate this
    Vector3 _velocity;
    float _velocityXSmoothing;
    Controller2D _controller;
    void Start() {
        _controller = GetComponent<Controller2D>();
        _gravity = -(2 * jumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        _jumpVelocity = Mathf.Abs(_gravity) * timeToJumpApex;
    }

    void Update() {
        if (_controller.collisions.above || _controller.collisions.below) {
            _velocity.y = 0;
        }
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (Input.GetKeyDown(KeyCode.Space) && _controller.collisions.below) {
            _velocity.y = _jumpVelocity;
        }

        float targetVelocityX = input.x * moveSpeed;
        _velocity.x = Mathf.SmoothDamp(
            _velocity.x,
            targetVelocityX,
            ref _velocityXSmoothing,
            (_controller.collisions.below) ? accelerationTimeGrounded : accelerationTimeAirborne);
        _velocity.y += _gravity * Time.deltaTime;
        _controller.Move(_velocity * Time.deltaTime);
    }
}
