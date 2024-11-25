using System;
using UnityEngine;
using UnityEngine.Serialization;

public class AnimatorMover : MonoBehaviour
{
    public Joystick joystick;

    [Header("Config")]
    public float RootMotionSpeed = 1;
    public float JumpForce;
    public LayerMask WhatIsGround;
    public float GroundCheckRadius = 0.1f;
    public float GroundCheckHeightOffset = 0.1f;
    public float GroundCheckSize = 0.5f;
    public float AirInfluenceControll = 0.5f;

    public Vector3 RootMotionDeltaPosition { get; private set; }
    public float MovementSpeed { get; private set; }
    public bool IsJumping { get; private set; }
    public bool IsGrounded { get; private set; } = true;
    public bool IsMoving { get; private set; }

    private Animator _animator;
    private Rigidbody _rigidbody;

    private Vector3 _movementDirection;
    private float _bodyRotation;

    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int MovingTurn = Animator.StringToHash("MovingTurn");
    private static readonly int Grounded = Animator.StringToHash("Grounded");
    private static readonly int Jumping = Animator.StringToHash("Jumping");


    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();
        _animator.applyRootMotion = true;
        _animator.updateMode = AnimatorUpdateMode.AnimatePhysics;
    }

    private void Update()
    {
        GroundCheck();

        SetAnimatorParameters();

        var input = new Vector3(joystick.Direction.x, 0, joystick.Direction.y);
        _movementDirection = Vector3.MoveTowards(_movementDirection, input, Time.deltaTime * 8f);
        MovementSpeed = Mathf.MoveTowards(MovementSpeed, _movementDirection.magnitude * 1.4f, Time.deltaTime);
        _animator.SetFloat(Speed, MovementSpeed);
        IsMoving = true;

        if (MovementSpeed <= 0.01f)
        {
            IsMoving = false;
        }

        if (IsMoving)
        {
            var rotateDirection = Vector3.SignedAngle(transform.forward, input, Vector3.up);
            _bodyRotation = Mathf.LerpAngle(_bodyRotation, rotateDirection / 180f, 2 * Time.deltaTime);
        }
        else
        {
            _bodyRotation = Mathf.LerpAngle(_bodyRotation, 0f, 8f * Time.deltaTime);
        }

        _animator.SetFloat(MovingTurn, _bodyRotation);

        if (Input.GetKeyDown(KeyCode.Space) && IsJumping is false)
        {
            Jump();
        }
    }

    private void FixedUpdate()
    {
        // if (IsGrounded == true && IsJumping == false)
        //     _animator.applyRootMotion = true;
        // else
        //     _animator.applyRootMotion = false;

        InAirMovementControl(JumpInert: true);
    }

    private void InAirMovementControl(bool JumpInert = true)
    {
        if (!IsGrounded)
        {
            if (IsMoving)
            {
                transform.Translate(0, -1f * Time.deltaTime, 0);
                transform.Translate(transform.forward * AirInfluenceControll / 2 * Time.deltaTime,
                    Space.World);
            }
        }
    }

    private void SetAnimatorParameters()
    {
        _animator.SetBool(Grounded, IsGrounded);
        _animator.SetBool(Jumping, IsJumping);
    }

    private void GroundCheck()
    {
        var groundCheck = Physics.OverlapBox(transform.position + transform.up * GroundCheckHeightOffset
            , new Vector3(GroundCheckRadius, GroundCheckSize, GroundCheckRadius), transform.rotation, WhatIsGround);

        if (groundCheck.Length is not 0 && IsJumping is false)
            IsGrounded = true;
        else
            IsGrounded = false;
    }

    private void Jump()
    {
        if (IsJumping) return;
        if (IsGrounded is false) return;

        IsGrounded = false;
        IsJumping = true;

        _rigidbody.AddForce(transform.up * 200 * JumpForce, ForceMode.Impulse);
        Invoke(nameof(DisableJump), 0.3f);
    }

    private void DisableJump()
    {
        IsJumping = false;
    }


    private void OnAnimatorMove()
    {
        var animationDelta = _animator.deltaPosition * Time.fixedDeltaTime;
        animationDelta.y = 0;
        RootMotionDeltaPosition = animationDelta;

        _rigidbody.velocity = RootMotionDeltaPosition * 5000 * RootMotionSpeed + Vector3.up * _rigidbody.velocity.y;
        transform.Rotate(_animator.deltaRotation.eulerAngles);
    }
}