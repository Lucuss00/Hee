using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem;

public class Simple_Player : MonoBehaviour // 
{
    private Rigidbody2D rigid;
    private Animator anim;

    [Header("Ground and Wall Check")]
    [SerializeField] private float groundDistCheck = 1f;
    [SerializeField] private float wallDistCheck = 0.5f;
    [SerializeField] private LayerMask groundLayer;
    private bool isGrounded = false;
    private bool isWalled = false;

    [Header("Move")]
    [SerializeField] private float moveSpeed = 5f;
    private float X_input;
    private float Y_input;
    private int facing = 1;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 20f;
    private bool isJumping = false;
    private bool isWallJumping = false;
    private bool isWallSliding = false;
    private bool canDoubleJump = false;

    [SerializeField] private float coyoteTimeLimit = .5f;
    [SerializeField] private float bufferTimeLimit = .5f;
    private float coyoteTime = -10000f;
    private float bufferTime = -10000f;

    [SerializeField] private Vector2 wallJumpForce = new Vector2(10f, 15f);


    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();
    }
    private void Update()
    {
        JumpState();
        Jump();
        WallSlide();
        InputVal();
        Move();
        Flip();
        GroundAndWallCheck();
        Animation();
    }
    private void JumpState()
    {
        if (!isGrounded && !isJumping) // takeoff
        {
            isJumping = true;

            if (rigid.linearVelocityY <= 0f) // เริ่มนับ coyoteJump
            {
                coyoteTime = Time.time;
            }
        }

        if (isGrounded && isJumping) // landing
        {
            isJumping = false;
            isWallJumping = false;
            isWallSliding = false;
            canDoubleJump = false;
        }

        if (isWalled) // wallSlide
        {
            isJumping = false;
            isWallJumping = false;
            canDoubleJump = false;

            if (isGrounded)
            {
                isWallSliding = false;
            }
            else
            {
                isWallSliding = true;
            }
        }
        else // ยกเลิก wallSlide
        {
            isWallSliding = false;
        }
    }
    private void Jump()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!isWalled)
            {
                if (isGrounded) // *** normalJump
                {
                    canDoubleJump = true;
                    rigid.linearVelocity = new Vector2(rigid.linearVelocityX, jumpForce);
                }
                else // doubleJump, coyoteJump
                {
                    if (rigid.linearVelocityY > 0f && canDoubleJump) // *** doubleJump
                    {
                        canDoubleJump = false;
                        rigid.linearVelocity = new Vector2(rigid.linearVelocityX, jumpForce);
                    }

                    if (rigid.linearVelocityY <= 0f)
                    {
                        if (Time.time < coyoteTime + coyoteTimeLimit) // *** coyoteJump
                        {
                            coyoteTime = 0f;
                            rigid.linearVelocity = new Vector2(rigid.linearVelocityX, jumpForce);
                        }
                        else // เริ่มนับ bufferJump
                        {
                            bufferTime = Time.time;
                        }
                    }
                }
            }
            else // *** wallJump
            {
                canDoubleJump = false;
                isWallJumping = true;
                rigid.linearVelocity = new Vector2(wallJumpForce.x * facing, wallJumpForce.y);
            }
        }
        else // *** bufferJump
        {
            if (isGrounded && Time.time < bufferTime + bufferTimeLimit)
            {
                bufferTime = 0f;
                rigid.linearVelocity = new Vector2(rigid.linearVelocityX, jumpForce);
            }
        }
    }
    private void WallSlide()
    {
        if (!isWalled || isGrounded || isWallJumping || rigid.linearVelocityY > 0f)
            return;

        float Y_slide = Y_input < 0f ? 1f : .5f;
        rigid.linearVelocity = new Vector2(X_input * moveSpeed, rigid.linearVelocityY * Y_slide);
    }
    private void InputVal()
    {
        X_input = Input.GetAxisRaw("Horizontal");
        Y_input = Input.GetAxisRaw("Vertical");
    }
    private void Move()
    {
        if (isWallJumping || isWallSliding)
            return;

        if (isGrounded)
        {
            rigid.linearVelocity = new Vector2(X_input * moveSpeed, rigid.linearVelocityY);
        }
        else
        {
            float X_airMove = X_input != 0f ? X_input * moveSpeed : rigid.linearVelocityX;
            rigid.linearVelocity = new Vector2(X_airMove, rigid.linearVelocityY);
        }
    }
    private void Flip()
    {
        if (rigid.linearVelocityX > 0.1f)
        {
            facing = -1;
            transform.rotation = Quaternion.identity;
        }
        else if (rigid.linearVelocityX < -0.1f)
        {
            facing = 1;
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }
    }
    private void GroundAndWallCheck()
    {
        isGrounded = Physics2D.Raycast(transform.position, Vector2.down, groundDistCheck, groundLayer);
        isWalled = Physics2D.Raycast(transform.position, transform.right, wallDistCheck, groundLayer);
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundDistCheck);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + transform.right * wallDistCheck);
    }
    private void Animation()
    {
        anim.SetBool("isGrounded", isGrounded);
        anim.SetBool("isWallSliding", isWallSliding);

        if (!isWalled)
            anim.SetFloat("velX", rigid.linearVelocityX);
        else
            anim.SetFloat("velX", 0f);

        anim.SetFloat("velY", rigid.linearVelocityY);
    }
}
