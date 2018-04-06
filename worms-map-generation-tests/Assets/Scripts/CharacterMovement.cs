using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CharacterMovement : MonoBehaviour
{

    private CharacterController _controller;

    public float Speed = 1f;
    public Vector3 Gravity = new Vector3(0, -.981f);
    public float MaxCoyoteTime = 0.1f;

    private Vector3 _fallingVelocity;
    private float _coyoteTime = 0;

    void Start()
    {
        _controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        var move = new Vector3(Input.GetAxisRaw("Horizontal"), 0, 0) * Time.deltaTime * Speed;

        if (_controller.isGrounded)
        {
            //Debug.Log("CharacterMovement coyote 0 ");
            _coyoteTime = 0;
        }
        else
        {
            _coyoteTime += Time.deltaTime;
            //Debug.Log("CharacterMovement setting coyote time " + _coyoteTime);
        }

        var shouldFall = _coyoteTime > MaxCoyoteTime;

        if (shouldFall)
        {
            _fallingVelocity += Gravity * Time.deltaTime;
            //Debug.Log("CharacterMovement falling " + _fallingVelocity);
        }
        else
        {
            _fallingVelocity.y = -_controller.minMoveDistance;
            //Debug.Log("CharacterMovement grounded " + _fallingVelocity);
        }
        //Debug.Log("CharacterMovement move " + move + " + " + _fallingVelocity);

        transform.localScale = FlipX(transform.localScale, move.x < 0);

        _controller.Move(move + _fallingVelocity);
    }

    private Vector3 FlipX(Vector3 v, bool facingLeft)
    {
        var newX = facingLeft ? -Mathf.Abs(v.x) : Mathf.Abs(v.x);
        return new Vector3(newX, v.y, v.z);
    }
}
