using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class RigidbodyPlayerMovement : MonoBehaviour
{
    [Header("View")]
    public Transform cameraHolder;
    public Transform cameraPosition;
    public Transform playerObj;
    public float sensX;
    public float sensY;

    float xRotation;
    float yRotation;

    [Header("Movement")]
    public float walkSpeed;
    public float sprintSpeed;
    private float moveSpeed;
    [Space]
    public float groundDrag;
    private Vector3 playerMovementInput;

    [Header("Jumping")]
    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;

    [Header("Crouching")]
    public float crouchSpeed;
    public float crouchYScale;
    private float startYScale;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    public bool grounded;
    bool crouchGrounded;

    [Header("Slope Handling")]
    public float maxSlopeAngle;
    private RaycastHit slopeHit;
    private bool exitingSlope;
    

    float horizontalInput;
    float verticalInput;

    Vector3 moveDirection;

    Rigidbody rb;

    public MovementState state;
    public enum MovementState
    {
        walking,
        sprinting,
        crouching,
        air
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        startYScale = transform.localScale.y;

        // Player view
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {        
        // ground check
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);
        crouchGrounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.01f, whatIsGround);

        // Moving CameraHolder along with the player
        cameraHolder.position = cameraPosition.position;

        View();
        MyInput();
        SpeedControl();
        StateHandler();

        // handle drag
        if (grounded)
            rb.drag = groundDrag;
        else
            rb.drag = 0;

        // Debug.Log(new Vector3(rb.velocity.x, 0, rb.velocity.z).magnitude);
        // Debug.Log(rb.velocity.y);
        // Debug.Log(moveDirection.normalized);
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void View()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensY;

        yRotation += mouseX;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90.0f, 90.0f);

        cameraHolder.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        playerObj.rotation = Quaternion.Euler(0, yRotation, 0);
    }

    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // when to jump
        if(Input.GetKeyDown(jumpKey) && grounded)
        {
            Jump();
        }

        // start crouch
        if (Input.GetKeyDown(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        }

        // stop crouch
        if (Input.GetKeyUp(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
        }
    }

    private void StateHandler()
    {
        // Mode - Crouching
        if (Input.GetKey(crouchKey))
        {
            state = MovementState.crouching;
            if(crouchGrounded)
            {
                moveSpeed = crouchSpeed;
            }
        }

        // Mode - Sprinting
        else if(grounded && Input.GetKey(sprintKey))
        {
            state = MovementState.sprinting;
            moveSpeed = sprintSpeed;
        }

        // Mode - Walking
        else if (grounded)
        {
            state = MovementState.walking;
            moveSpeed = walkSpeed;
        }

        // Mode - Air
        else
        {
            state = MovementState.air;
        }
    }

    private void MovePlayer()
    {
        // playerMovementInput = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
        // Vector3 moveVector = playerObj.TransformDirection(playerMovementInput) * moveSpeed;
        // Vector3 dragForce = groundDrag * rb.velocity;;

        // calculate movement direction
        moveDirection = playerObj.forward * verticalInput + playerObj.right * horizontalInput;

        // on slope
        if (OnSlope() && !exitingSlope)
        {
            rb.AddForce(GetSlopeMoveDirection() * moveSpeed * 46f, ForceMode.Force);

            if (rb.velocity.y > 0)
            {
                rb.AddForce(Vector3.down * 80f, ForceMode.Force);
            }
        }
        // on ground
        else if(grounded)
        {
            // Apply the force to counteract the drag
            // rb.AddForce(dragForce, ForceMode.Force);
            // Drag 40 : requires * 200, Drag 30 : requires * 75, Drag 20 : requires * 33.333333, Drag 15 : requires * 21.4285791, Drag 12 : requires * 17, Drag 10 : requires * 12.5, Drag 5 : requires * 5.6
            rb.AddForce(moveDirection.normalized * moveSpeed * 17f, ForceMode.Force);
            // rb.velocity = new Vector3(moveVector.x, rb.velocity.y, moveVector.z);
            // rb.velocity = new Vector3(moveDirection.x * moveSpeed * 1.5f, rb.velocity.y, moveDirection.z * moveSpeed * 1.5f);
        }
        // in air
        else if(!grounded)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * airMultiplier, ForceMode.Force);
        }

        // turn gravity off while on slope
        rb.useGravity = !OnSlope();
    }

    private void SpeedControl()
    {
        // limiting speed on slope
        if (OnSlope() && !exitingSlope)
        {
            if (rb.velocity.magnitude > moveSpeed)
                rb.velocity = rb.velocity.normalized * moveSpeed;
        }
        // limiting speed on ground or in air
        else
        {
            Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            // limit velocity if needed
            if (flatVel.magnitude > moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
            }

            // Debug.Log(flatVel.magnitude);
        }
    }

    private void Jump()
    {
        exitingSlope = true;

        // reset y velocity for consistent jump
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        exitingSlope = false;
    }

    private bool OnSlope()
    {
        if(Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }

        return false;
    }

    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
    }
}