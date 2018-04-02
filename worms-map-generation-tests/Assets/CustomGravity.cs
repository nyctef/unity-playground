using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CustomGravity : MonoBehaviour
{

    public float gravityScale = 1.0f;

    public static float globalGravity = -9.81f;

    Rigidbody _rb;

    void OnEnable()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
    }

    void FixedUpdate()
    {
        Vector3 gravity = globalGravity * gravityScale * Vector3.up;
        _rb.AddForce(gravity, ForceMode.Acceleration);
    }
}
