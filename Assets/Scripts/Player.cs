using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Controller2D))]
public class Player : MonoBehaviour
{
    Controller2D _controller;
    void Start() {
        _controller = GetComponent<Controller2D>();
    }
}
