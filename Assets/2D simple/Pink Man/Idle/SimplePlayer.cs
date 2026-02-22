using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem;

public class SimplePlayer : MonoBehaviour //library สำหรับช่วง gameplay
{
    private Rigidbody2D rigid; // component rigidbody2D
    private Animator anim; // component animator

    [Header("Ground And Wall Check")]
    [SerializeField] private float groundDistCheck = 1f; // ระยะ sensor ตรวจหาพื้น
    [SerializeField] private float wallDistCheck = 1f; // ระยะ sensor ตรวจหาผนัง
    [SerializeField] private LayerMask groundLayer; // sensor เจอเฉพาะ ground layer
    public bool isGrounded = false; // เจอพื้น ?
    public bool isWalled = false; // เจอผนัง ?
    [Header("Move")]
    [SerializeField] private float moveSpeed = 5f; // ความเร็ว Player แนวราบ
    public float X_input; // ปุ่ม a,d
    public float Y_input; // ปุ่ม s ใช้ตอนเร่งความเร็ว WallSliding
    public int facing = 1; //หันหัว

    [Header("Jump")]
    [SerializeField] private float jumpForce = 20f;
    [SerializeField] private Vector2 wallJumpFore = new Vector2(10f, 15f);
    public bool isJumping = false;
    public bool isWallJumping = false;
    public bool isWallSilding = false;
    public bool canDoubleJump = false;

    [SerializeField] private float coyoteTimeLimit = .5f;
    [SerializeField] private float bufferTimeLimit = .5f;
    public float coyoteTime;
    public float bufferTime;

    private void Awake() // Awake() ทำก่อน Start() 
    {
        rigid = GetComponent<Rigidbody2D>(); // ดึงจากตัวมันเอง
        anim = GetComponentInChildren<Animator>(); // InChildren เพราะดึงจากลูมัน
    }
    private void Update() // method ที่รันทุก frame
    {
        JumpState(); // ดูสถานะการกระโดด >> takeoff, landing, wallSlide, etc
        Jump(); // สั่งให้กระโดด
        WallSlide(); //  สั่งให้ wallSlide
        InputVal(); // ค่า input จาก player
        Move(); // สั้งให้เคลื่อนที่แนวนอน ทั้งตอนอยู่บนพื้นและกลางอากาศ
        Flip(); // หันซ้าย/ขวา เวลา wallJump ให้โดดทิศตรงข้ามกำแพง
        GroundAndWallCheck(); // ตรวจพื้นกับผนัง
        Animation(); // สั่งให้ play Animation
    }
    private void JumpState()
    {
        if (!isGrounded && !isJumping)
        {
            isJumping = true;
            if (rigid.linearVelocityY <= 0f)
            {
                coyoteTime = Time.time;
            }

        }
        if (isGrounded && isJumping)
        {
            isJumping = false ;
            isWallJumping = false;
            isWallSilding=false;    
            canDoubleJump = false;
           
        }
        if (isWalled)
        {
            isJumping = false;
            isWallJumping = false;
            canDoubleJump = false;
            if (isGrounded)
            {
                isWallSilding = false;
            }
            else
            {
                isWallSilding = true;
            }
            
        }
        else
        {
            isWallSilding = false ;
        }
    }
    private void Jump()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!isWalled) 
            {
                if (isGrounded)
                {
                    canDoubleJump = true;
                    rigid.linearVelocity = new Vector2(rigid.linearVelocityX, jumpForce);
                }
                else
                {
                    if (rigid.linearVelocityY > 0f && canDoubleJump) ;
                    {
                        canDoubleJump=false;
                        rigid.linearVelocity = new Vector2(rigid.linearVelocityX, jumpForce);

                    }
                    if (rigid.linearVelocityY <= 0f) // ถ้ากำลังตกลง coyoteJump , bufferjumps
                    {
                        if (Time.time < coyoteTime + coyoteTimeLimit) // ***coyotejump
                        {
                            coyoteTime = 0f; // จับเวลาใหม่ ทำให้โดดซ้ำไม่ได้
                            rigid.linearVelocity = new Vector2(rigid.linearVelocityX, jumpForce);
                        }
                        else
                        {
                            bufferTime = Time.time;
                        }
                    }
                }
            }
            else
            {
                isWallJumping = true ;
                rigid.linearVelocity = new Vector2(wallJumpFore.x * facing, wallJumpFore.y);
            }
        }
        else
        {
            if (isGrounded && Time.time < bufferTime + bufferTimeLimit)
           
            bufferTime= 0f;
            rigid.linearVelocity = new Vector2(rigid.linearVelocityX, jumpForce);

        }
    }
    private void WallSlide()
    {
        if (isWalled || isGrounded || isWallJumping || rigid.linearVelocityY > 0f)
            return;
        float Y_slide = Y_input < 0f ? 1f : 0.5f;
        rigid.linearVelocity = new Vector2(X_input * moveSpeed, rigid.linearVelocityY * .5f);

    }
    private void InputVal()
    {
        X_input = Input.GetAxisRaw("Horizontal"); // ปุ่ม a,d
        Y_input = Input.GetAxisRaw("Vertical"); // ปุ่ม w,s 
    }
    private void Move()
    {
        if (isWallJumping) 
            return;
        if (isGrounded)
        {
            rigid.linearVelocity = new Vector2(X_input * moveSpeed, rigid.linearVelocityY);
        }
        else 
        {
            float X_airMove = X_input !=0f ? X_input * moveSpeed : rigid.linearVelocityX;

            rigid.linearVelocity = new Vector2(X_airMove, rigid.linearVelocityY);
        }
    }

    private void Flip()
    {
        if(rigid.linearVelocityX > 0.01f)
        {
            facing = -1;
            transform.rotation = Quaternion.Euler(0f, 0f , 0f);
        }
        if (rigid.linearVelocityX < -0.01f)
        {
            facing = 1;

            transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        }
    }
    private void GroundAndWallCheck()
    {
        // Raycast(เริ่มจุดไหน, ทิศไหน, ระยะทาง, layerMask ที่จะตรวจ)
        isGrounded = Physics2D.Raycast(transform.position, Vector2.down, groundDistCheck, groundLayer); // sensor พื้น
        isWalled = Physics2D.Raycast(transform.position, transform.right, wallDistCheck, groundLayer); // sensor ผนัง
    }
    private void OnDrawGizmos() // method ที่ unity รู้จัก ไว้แสดงผล sensor พื้นและผนัง
    {
        Gizmos.color = Color.blue;  // สีของ sensor
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundDistCheck); // UI แสดง sensor พื้น
        Gizmos.color = Color.red;  // สีของ sensor
        Gizmos.DrawLine(transform.position, transform.position + transform.right * wallDistCheck); // UI แสดง sensor ผนัง
    }
    private void Animation()
    {

    }
}
