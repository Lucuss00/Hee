using UnityEngine;

public class SimplePlayerTest : MonoBehaviour //library สำหรับช่วง gameplay
{
    private Rigidbody2D rigid; // component rigidbody2D
    private Animator anim; // component animator

    [Header("Ground And Wall Check")]
    [SerializeField] private float groundDistCheck = 1f; // ระยะ sensor ตรวจหาพื้น
    [SerializeField] private float wallDistCheck = 1f; // ระยะ sensor ตรวจหาผนัง
    [SerializeField] private LayerMask groundLayer;// sensor เจอเฉพาะ ground layer
    public bool isGrounded = false; // เจอพื้น ?
    public bool isWalled = false; // เจอผนัง ?

    [Header("Move")]
    [SerializeField] private float moveSpeed = 5f; // ความเร็ว player แนวราบ
    public float X_input; // ปุ่ม a,d
    public float Y_input; // ปุ่ม s ใช้ตอนเร่งความเร็ว wallSliding
    public int facing = 1; // ใช้หันออกจากกำแพงเวลาโดด wallJumping

    [Header("Jump")]
    [SerializeField] private float jumpForce = 20f; // แรงกระโดด
    [SerializeField] private Vector2 wallJumpForce = new Vector2(10f, 15f); // แรงกระโดด wallJump
    public bool isJumping = false;
    public bool isWallJumping = false;
    public bool isWallSliding = false;
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
    private void JumpState() // ดูว่าตัวละครสถานะเป็นยังไง
    {
        if(!isGrounded && !isJumping) //  takeoff, fall
        {
            isJumping = true;
            if(rigid.linearVelocityY <= 0f) // fall
            {
                coyoteTime = Time.time; // เริ่มจับเวลา coyote
            }
        }

        if(isGrounded && isJumping) // landing
        {
            isJumping = false; // หยุดกระโดด
            isWallJumping = false;
            isWallSliding = false;
            canDoubleJump = false;
        }

        if (isWalled) // wallSliding
        {
            isJumping = false;
            isWallJumping = false;
            canDoubleJump = false;

            if (isGrounded) // ถ้าชนพื้น
            {
                isWallSliding = false;
                anim.SetBool("IsWallsild", false);
            }
            else // ถ้าไม่ชนพื้น
            {
                isWallSliding = true;
                anim.SetBool("IsWallsild", true);
            }
        }
        else // ถ้าไม่ติดผนัง
        {
            isWallSliding = false;
        }
    }
    private void Jump()
    {
        if (Input.GetKeyDown(KeyCode.Space)) // กด spacebar
        {
            if (!isWalled) // ถ้าไม่ติดผนัง
            {
                if(isGrounded) // ถ้าอยู่บนพื้น **normalJump
                {
                    canDoubleJump = true; // โดด doubleJump ได้ 1 ครั้ง
                    rigid.linearVelocity = new Vector2(rigid.linearVelocityX, jumpForce); // กระโดด
                }
                else // ถ้าไม่อยู่บนพื้น
                {
                    if(rigid.linearVelocityY > 0f && canDoubleJump) // กำลังลอยขึ้น ***doubleJump
                    {
                        canDoubleJump = false; // โดดซ้ำอีกทีไม่ได้
                        rigid.linearVelocity = new Vector2(rigid.linearVelocityX, jumpForce); // กระโดด
                    }
                     
                    if(rigid.linearVelocityY <= 0f) // ถ้ากำลังตกลง coyoteJump, bufferJump
                    {
                        if(Time.time < coyoteTime + coyoteTimeLimit) // *** coyoteJump
                        {
                            coyoteTime = 0f; // จับเวลาใหม่ ทำให้โดดซ้ำไม่ได้
                            rigid.linearVelocity = new Vector2(rigid.linearVelocityX, jumpForce); // กระโดด
                        }
                        else // ถ้าตกและเลยเวลาของ coyote จะเริ่มนับเวลา bufferJump
                        {
                            bufferTime = Time.time; // เริ่มจับเวลา
                        }
                    }
                }
            }
            else // อยู่ติดผนัง แสดงว่าเป็น wallJump
            {
                isWallJumping = true;
                rigid.linearVelocity = new Vector2(wallJumpForce.x * facing, wallJumpForce.y); // ***wallJump
            }
        }
        else // ถ้าไม่กด spacebar แสดงว่าเป็น bufferJump
        {
            if (isGrounded && Time.time < bufferTime + bufferTimeLimit) // ***bufferJump
            {
                bufferTime = 0f; // reset เวลาใหม่
                rigid.linearVelocity = new Vector2(rigid.linearVelocityX, jumpForce); // กระโดด 
            }
        }
    }
    private void WallSlide()
    {
        if (!isWalled || isGrounded || isWallJumping || rigid.linearVelocityY > 0f) 
           // ถ้าไม่ติดผนัง หรือชนพื้น หรือ wallJumping อยู่ หรือกำลังลอยขึ้น จะไม่เล่น animation wallSlide
        return; // ข้าม code ข้างล่างไป
        Debug.Log("wall");
          // สั่งให้เล่น animation wallSlide
        float Y_slide = Y_input < 0f ? 1f : 0.5f; // ถ้ากด s จะเร็ว ไม่กดจะช้า
        rigid.linearVelocity = new Vector2(X_input * moveSpeed, rigid.linearVelocityY * Y_slide);
    }
    private void InputVal()
    {
        X_input = Input.GetAxisRaw("Horizontal"); // ปุ่ม a,d
        Y_input = Input.GetAxisRaw("Vertical"); // ปุ่ม w,s
    }
    private void Move()
    {
        if (isWallJumping) // ถ้า wallJumping อยู่จะควบคุมตัวละครไม่ได้
            return; // ให้ข้ามบรรทัดล่างไปเลย
        if (isGrounded) // ถ้าอยู่บนพื้น
        {
            rigid.linearVelocity = new Vector2(X_input * moveSpeed, rigid.linearVelocityY); // แรงผลักตัวละครให้เคลื่อนที่
        }
        else // ถ้าลอยกลางอากาศ
        {
            float X_airMove = X_input != 0f ? X_input * moveSpeed : rigid.linearVelocityX; // ถ้ากดหรือไม่กด a,d

            rigid.linearVelocity = new Vector2(X_airMove, rigid.linearVelocityY);
        }
    }
    private void Flip()
    {
        if(rigid.linearVelocityX > 0.1f) // ถ้าผลักไปทางขวา
        {
            facing = -1; // หันตรงข้ามกับกำแพง wallJump
            transform.rotation = Quaternion.Euler(0f, 0f, 0f); // หันไปทางขวา
        }
        if (rigid.linearVelocityX < -0.1f) // ถ้าผลักไปทางซ้าย
        {
            facing = 1;
            transform.rotation = Quaternion.Euler(0f, 180f, 0f); // หันไปทางซ้าย
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
