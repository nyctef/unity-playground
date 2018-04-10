using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CharacterMovement : MonoBehaviour
{
    public float Speed = 1f;
    public Vector3 Gravity = new Vector3(0, -.981f);
    public float MaxCoyoteTime = 0.1f;

    public Vector3 VertJumpForce = new Vector3(0, 2f);
    public Vector3 HorizontalJumpForce = new Vector3(1.5f, 0.5f);

    public Transform GrenadePrefab;
    public float GrenadeThrowSpeed;

    // TODO: can/should we separate up/down aiming from left/right movement?
    public float AimAngle = -90f;
    public float AimSpeed = 2f;

    private CharacterController _controller;
    private SimpleSpriteAnimator _animator;
    private Transform _sprite;

    private Vector3 _fallingVelocity;
    private float _coyoteTime = 0;

    private bool _facingLeft;

    private string _currentAnimation = null;

    void Start()
    {
        _controller = GetComponent<CharacterController>();
        _animator = GetComponent<SimpleSpriteAnimator>();
        _sprite = transform.Find("Sprite");
    }

    void Update()
    {
        var horizontalInput = Input.GetAxisRaw("Horizontal");
        var verticalInput = Input.GetAxisRaw("Vertical");
        var jumpInput = Input.GetButtonDown("Jump");
        var fire1Input = Input.GetKeyDown(KeyCode.F1); // temp input
        var fire2Input = Input.GetKeyDown(KeyCode.F2); // temp input

        // TODO pull out weapons manager

        if (fire1Input)
        {
            var ray = new Ray(transform.position, Quaternion.Euler(0, 0, AimAngle) * Vector3.up);
            Debug.DrawRay(ray.origin, ray.direction * 200, Color.red, 0.5f, false);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                EventManager.Instance.TriggerEvent(new Events.Explosion(hit.point, 50));
            }
        }

        if (fire2Input)
        {
            var grenade = Instantiate(GrenadePrefab, transform.position, Quaternion.identity);
            Physics.IgnoreCollision(GetComponent<Collider>(), grenade.GetComponent<Collider>());
            var direction = Quaternion.Euler(0, 0, AimAngle) * Vector3.up *GrenadeThrowSpeed;
            grenade.GetComponent<Rigidbody>().velocity = direction;
        }

        var move = new Vector3(horizontalInput, 0, 0) * Time.deltaTime * Speed;
        
        // flip relevant values when facing left
        if (Math.Abs(move.x) > 0.01)
        {
            var facingLeft = move.x < 0;
            if (facingLeft != _facingLeft)
            {
                AimAngle = -AimAngle; // since 0 angle is straight up
            }
            _facingLeft = facingLeft;
        }

        AimAngle += verticalInput * AimSpeed * (_facingLeft ? -1 : 1);
        AimAngle %= 360;

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
                _fallingVelocity = FlipX(HorizontalJumpForce, _facingLeft);
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

        //Debug.LogFormat(
        //    "CharacterMovement {0:R} {1} move {2} fallingVelocity {3} coyoteTime {4} shouldFall {5} zCorrection {6} anim {7}",
        //    transform.position.y, _controller.isGrounded ? "G" : "F", move.ToString("R"),
        //    _fallingVelocity.ToString("R"), _coyoteTime, shouldFall, zCorrection, newAnimation);

        _sprite.transform.localScale = FlipX(transform.localScale, _facingLeft);

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
