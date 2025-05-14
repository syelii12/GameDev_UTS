using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Player Movement")]
    public float moveSpeed = 5f;
    public float runSpeed = 8f;
    public float jumpForce = 10f;
    public float climbSpeed = 3f;

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sprite;
    private PlayerController playerController;

    private Vector2 moveInput;
    private bool isRunning = false;
    private bool isCrouching = false;
    private bool isClimbing = false;

    private enum MovementState { idle, run, jump, climb, crouch }

    [Header("Environment Checks")]
    [SerializeField] private LayerMask jumpableGround;
    [SerializeField] private LayerMask climbableLayer;
    private BoxCollider2D coll;

    [Header("Crouch Settings")]
    [SerializeField] private Vector2 crouchColliderSize = new Vector2(1f, 0.5f);
    [SerializeField] private Vector2 crouchColliderOffset = new Vector2(0f, -0.25f);
    private Vector2 originalColliderSize;
    private Vector2 originalColliderOffset;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();
        coll = GetComponent<BoxCollider2D>();

        originalColliderSize = coll.size;
        originalColliderOffset = coll.offset;

        playerController = new PlayerController(); // Input System class
    }

    private void OnEnable()
    {
        playerController.Enable();

        playerController.Movement.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        playerController.Movement.Move.canceled += ctx => moveInput = Vector2.zero;

        playerController.Movement.Jump.performed += ctx => Jump();
        playerController.Movement.Run.performed += ctx => isRunning = true;
        playerController.Movement.Run.canceled += ctx => isRunning = false;

        playerController.Movement.Crouch.performed += ctx => isCrouching = true;
        playerController.Movement.Crouch.canceled += ctx => isCrouching = false;

        playerController.Movement.Climb.performed += ctx => isClimbing = true;
        playerController.Movement.Climb.canceled += ctx => isClimbing = false;
    }

    private void OnDisable()
    {
        playerController.Disable();
    }

    private void FixedUpdate()
    {
        if (isClimbing && CanClimb())
        {
            rb.gravityScale = 0f;
            rb.velocity = new Vector2(rb.velocity.x, moveInput.y * climbSpeed);
        }
        else
        {
            rb.gravityScale = 1f;
            float speed = isRunning ? runSpeed : moveSpeed;
            float finalSpeed = isCrouching ? speed * 0.5f : speed;
            rb.velocity = new Vector2(moveInput.x * finalSpeed, rb.velocity.y);
        }

        UpdateAnimation();
    }

    private void UpdateAnimation()
    {
        MovementState state;

        if (isClimbing && Mathf.Abs(moveInput.y) > 0.1f)
        {
            state = MovementState.climb;
        }
        else if (isCrouching && IsGrounded())
        {
            state = MovementState.crouch;
        }
        else if (!IsGrounded())
        {
            state = MovementState.jump;
        }
        else if (moveInput.x != 0f)
        {
            state = MovementState.run;
            sprite.flipX = moveInput.x < 0f;
        }
        else
        {
            state = MovementState.idle;
        }

        anim.SetInteger("state", (int)state);
    }

    private void HandleCrouch()
    {
        if (isCrouching && IsGrounded())
        {
            coll.size = crouchColliderSize;
            coll.offset = crouchColliderOffset;
        }
        else
        {
            coll.size = originalColliderSize;
            coll.offset = originalColliderOffset;
        }
    }

    private bool IsGrounded()
    {
        return Physics2D.BoxCast(
            coll.bounds.center,
            coll.bounds.size,
            0f,
            Vector2.down,
            0.1f,
            jumpableGround
        );
    }

    private bool CanClimb()
    {
        return Physics2D.OverlapBox(transform.position, coll.size, 0f, climbableLayer);
    }

    private void Jump()
    {
        if (IsGrounded() && !isCrouching && !isClimbing)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }
    }
}
