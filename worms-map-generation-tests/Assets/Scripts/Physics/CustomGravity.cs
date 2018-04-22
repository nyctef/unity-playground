using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CustomGravity : MonoBehaviour
{
    public float GravityScale = 1.0f;

    private static readonly float GlobalGravity = -9.81f;

    Rigidbody _rb;

    void OnEnable()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
    }

    void FixedUpdate()
    {
        var gravity = GlobalGravity * GravityScale * Vector3.up;
        _rb.AddForce(gravity, ForceMode.Acceleration);
    }
}
