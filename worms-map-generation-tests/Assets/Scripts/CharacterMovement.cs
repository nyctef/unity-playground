using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CharacterMovement : MonoBehaviour
{
    private CharacterController _controller;
    private SimpleSpriteAnimator _animator;

    public float Speed = 1f;
    public Vector3 Gravity = new Vector3(0, -.981f);
    public float MaxCoyoteTime = 0.1f;

    public Vector3 VertJumpForce = new Vector3(0, 2f);
    public Vector3 HorizontalJumpForce = new Vector3(1.5f, 0.5f);

    private Vector3 _fallingVelocity;
    private float _coyoteTime = 0;

    private string _currentAnimation = null;

    void Start()
    {
        _controller = GetComponent<CharacterController>();
        _animator = GetComponent<SimpleSpriteAnimator>();
    }

    void Update()
    {
        var horizontalInput = Input.GetAxisRaw("Horizontal");
        var jumpInput = Input.GetButtonDown("Jump");
        var explodeInput = Input.GetKeyDown(KeyCode.F1); // temp input

        if (explodeInput)
        {
            EventManager.Instance.TriggerEvent(new Events.Explosion(transform.position, 50));
        }

        var move = new Vector3(horizontalInput, 0, 0) * Time.deltaTime * Speed;
        var facingLeft = move.x < 0;

        // calculate "coyote time" - a small grace period to jump off ledges with
        if (_controller.isGrounded)
        {
            _coyoteTime = 0;
        }
        else
        {
            _coyoteTime += Time.deltaTime;
        }
        var shouldFall = _coyoteTime > MaxCoyoteTime;

        // stop horizontal jumps if we touch the floor
        if (_controller.collisionFlags != 0)
        {
            _fallingVelocity.x = 0;
        }

        // bump off ceilings
        if ((_controller.collisionFlags & CollisionFlags.Above) != 0)
        {
            _fallingVelocity.y = Math.Min(_fallingVelocity.y, 0);
        }

        if (shouldFall)
        {
            // wheeeeee
            _fallingVelocity += Gravity * Time.deltaTime;
        }
        else if (jumpInput)
        {
            // do a vertical or horizontal jump depending on if we're moving horizontally
            if (Math.Abs(move.x) < 0.01f)
            {
                _fallingVelocity = VertJumpForce;
            }
            else
            {
                _fallingVelocity = FlipX(HorizontalJumpForce, facingLeft);
            }
        }
        else if (Math.Abs(_fallingVelocity.y) < 0.1f)
        {
            // we have to push the character down a bit
            // isGrounded is only set if the character actually hit something when we last tried to move them
            _fallingVelocity.y = -0.1f;
        }

        // no 3D allowed
        var zCorrection = new Vector3(0,0, -transform.position.z);

        var newAnimation = Math.Abs(move.x) > 0.001 ? "Walk" : "Idle";
        SetAnimation(newAnimation);

        Debug.LogFormat(
            "CharacterMovement {0:R} {1} move {2} fallingVelocity {3} coyoteTime {4} shouldFall {5} zCorrection {6} anim {7}",
            transform.position.y, _controller.isGrounded ? "G" : "F", move.ToString("R"),
            _fallingVelocity.ToString("R"), _coyoteTime, shouldFall, zCorrection, newAnimation);

        transform.localScale = FlipX(transform.localScale, facingLeft);

        _controller.Move(move + _fallingVelocity + zCorrection);
    }

    private void SetAnimation(string newAnimation)
    {
        if (newAnimation == _currentAnimation)
        {
            return;
        }
        _currentAnimation = newAnimation;
        _animator.Play(newAnimation);
    }

    private Vector3 FlipX(Vector3 v, bool facingLeft)
    {
        var newX = facingLeft ? -Mathf.Abs(v.x) : Mathf.Abs(v.x);
        return new Vector3(newX, v.y, v.z);
    }
}
