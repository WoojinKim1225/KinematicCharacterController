using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class GroundMover : MonoBehaviour
{
    Rigidbody rb;
    Vector3 initialPosition;

    void Awake() {
        rb = GetComponent<Rigidbody>();
        initialPosition = rb.position;
    }

    void FixedUpdate() {
        rb.MovePosition(initialPosition + Mathf.Abs(Time.fixedTime % 6f - 3f) * 3f * Vector3.forward);
    }
}
