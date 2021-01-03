using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Controller2D))]
public class Player : MonoBehaviour
{
    float _moveSpeed = 6f;
    float _gravity = -20f;
    Vector3 _velocity;
    Controller2D _controller;
    void Start() {
        _controller = GetComponent<Controller2D>();
    }

    void Update() {
        if (_controller.collisions.above || _controller.collisions.below) {
            _velocity.y = 0;
        }
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        _velocity.x = input.x * _moveSpeed;
        _velocity.y += _gravity * Time.deltaTime;
        _controller.Move(_velocity * Time.deltaTime);
    }
}
