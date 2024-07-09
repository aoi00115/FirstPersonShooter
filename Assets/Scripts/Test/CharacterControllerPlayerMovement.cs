using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterControllerPlayerMovement : MonoBehaviour
{
    [Header("References")]
    public Transform headPosition;
    public Transform feetTransform;
    private CharacterController characterController;
    private DefaultInput defaultInput;
    [HideInInspector] public Vector2 input_Movement;
    [HideInInspector] public Vector2 input_View;

    [Header("Mask Settings")]
    public LayerMask playerMask;
    public LayerMask groundMask;

    [Header("Look Settings")]
    public float viewXSensitivity;
    public float viewYSensitivity;
    public float viewClampYMin = -89;
    public float viewClampYMax = 89;
    private Vector3 newCameraRotation;
    private Vector3 newCharacterRotation;

    [Header("Movement Settings")]
    public float movementSmoothing;
    public float airMultiplier;
    public float fallingSmoothing;
    private Vector3 inputMovementSpeed;
    private Vector3 newMovementSpeed;
    private Vector3 newMovementSpeedVelocity;
    private Vector3 movementSpeed;
    private Vector3 momentum;
    [HideInInspector] public bool isIdle;

    [Header("Sprinting Movement Settings")]
    public float sprintingForwardSpeed;
    public float sprintingStrafeSpeed;
    public bool enableStamina;
    [Tooltip("Enable this to sprint when holding sprint button")]
    public bool isSprintingHold;
    [HideInInspector] public bool isSprinting;

    [Header("Walking Movement Settings")]
    public float walkingForwardSpeed;
    public float walkingBackwardSpeed;
    public float walkingStrafeSpeed;
    [HideInInspector] public bool isWalking;

    [Header("Jumping Movement Settings")]
    public float jumpingHeight;
    public float jumpingFalloff;
    private float fallingDelay;
    [Space]
    public Vector3 jumpingForce;
    private Vector3 jumpingForceVelocity;
    private bool isSprintJump;
    [HideInInspector] public bool isJumping;

    [Header("Speed Effectors")]
    public float speedEffector = 1;
    public float crouchSpeedEffector;
    public float proneSpeedEffector;
    public float fallingSpeedEffector;

    [Header("Gravity Settings")]
    public float gravityAmount;
    public float gravityMin;
    private float playerGravity;

    [Header("Stance")]
    public PlayerStance playerStance;
    public float playerStanceSmoothing;
    public CharacterStance playerStandStance;
    public CharacterStance playerCrouchStance;
    public CharacterStance playerProneStance;
    private float stanceCheckErrorMargine = 0.05f;
    private float cameraHeight;
    private float cameraHeightVelocity;
    private Vector3 stanceCapsuleCenterVelocity;
    private float stanceCapsuleHeightVelocity;
    [HideInInspector] public bool isStand;
    [HideInInspector] public bool isCrouch;
    [HideInInspector] public bool isProne;

    public enum PlayerStance
    {
        Stand,
        Crouch,
        Prone
    }

    [Serializable]
    public class CharacterStance
    {
        public float CameraHeight;
        public CapsuleCollider StanceCollider;
    }

    private void Awake()
    {
        // Initialize defaultInput object by creating a new instance of DefaultInput class
        defaultInput = new DefaultInput();

        // Subscribe to the performed event of the Movement action in the Character action map
        // Assign a lambda function as the event handler to update input_Movement when the action is performed
        defaultInput.Character.Movement.performed += e => input_Movement = e.ReadValue<Vector2>();
        defaultInput.Character.View.performed += e => input_View = e.ReadValue<Vector2>();
        defaultInput.Character.Jump.performed += e => Jump();
        defaultInput.Character.Crouch.performed += e => Crouch();
        defaultInput.Character.Prone.performed += e => Prone();
        defaultInput.Character.Sprint.performed += e => ToggleSprint();
        defaultInput.Character.SprintReleased.performed += e => StopSprint();

        defaultInput.Enable();
    }

    private void Start()
    {
        characterController = GetComponent<CharacterController>();

        newCameraRotation = headPosition.localRotation.eulerAngles;
        newCharacterRotation = transform.localRotation.eulerAngles;

        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        CalculateLook();
        CalculateMovement();
        CalculateJump();
        CalculateStance();
        CalculateSprint();
    }

    #region - Look -

    private void CalculateLook()
    {
        newCharacterRotation.y += viewXSensitivity * input_View.x * Time.deltaTime;
        transform.localRotation = Quaternion.Euler(newCharacterRotation);

        newCameraRotation.x += viewYSensitivity * -input_View.y * Time.deltaTime;
        newCameraRotation.x = Mathf.Clamp(newCameraRotation.x, viewClampYMin, viewClampYMax);

        headPosition.localRotation = Quaternion.Euler(newCameraRotation);
    }

    #endregion

    #region - Movement -

    private void CalculateMovement()
    {
        if(input_Movement.y <= 0.2f)
        {
            isSprinting = false;
        }

        var verticalSpeed = walkingForwardSpeed;
        var horizontalSpeed = walkingStrafeSpeed;

        // Multiplying player movement speed by sprinting speed when sprinting
        if(isSprinting)
        {
            verticalSpeed = sprintingForwardSpeed;
            horizontalSpeed = sprintingStrafeSpeed;
        }

        // Speed effectors for when the player is in a different motion
        if(!characterController.isGrounded)
        {
            // Remove comment out to slow down the player when jumping
            // SpeedEffector = fallingSpeedEffector;
        }
        else if (playerStance == PlayerStance.Crouch)
        {
            speedEffector = crouchSpeedEffector;
        }
        else if (playerStance == PlayerStance.Prone)
        {
            speedEffector = proneSpeedEffector;
        }
        else
        {
            speedEffector = 1;
        }

        verticalSpeed *= speedEffector;
        horizontalSpeed *= speedEffector;

        // Translating input to the direction where player is looking as inputMovementSpeed
        inputMovementSpeed = transform.TransformDirection(new Vector3(horizontalSpeed * input_Movement.x * Time.deltaTime, 0, verticalSpeed * input_Movement.y * Time.deltaTime));

        // Smoothing the movement only if the player is on the ground so that it starts smoothing after player is on the ground, aka landed
        if(characterController.isGrounded)
        {
            newMovementSpeed = Vector3.SmoothDamp(newMovementSpeed, inputMovementSpeed, ref newMovementSpeedVelocity, characterController.isGrounded ? movementSmoothing : fallingSmoothing);
        }
        else
        {
            newMovementSpeed += transform.TransformDirection(new Vector3(airMultiplier * input_Movement.x * Time.deltaTime, 0, airMultiplier * input_Movement.y * Time.deltaTime));
        }

        // This line is needed to prevent, all the added movement such as jumping below, to get smoothed as well
        movementSpeed = newMovementSpeed;

        // if(characterController.isGrounded)
        // {
        //     momentum = movementSpeed;
        // }
        // else
        // {
        //     movementSpeed = momentum;
        // }

        // Deducting from the playerGravity so that the gravity accelerates as the player falls. For the player not to go crazy fast when falling, set the "Terminal velocity(gravityMin)"
        if(playerGravity > gravityMin)
        {
            playerGravity -= gravityAmount * Time.deltaTime;
        }

        // Setting the gravity to -1 when player is grounded 
        if(playerGravity < -0.01f && characterController.isGrounded)
        {
            playerGravity = -0.01f;
        }

        // Adding gravity and jumpingForce along y axis
        movementSpeed.y += playerGravity;
        movementSpeed += jumpingForce * Time.deltaTime;

        // Move the player position with characterController.Move
        characterController.Move(movementSpeed);

        // Calculating whether play is idle or not
        if(input_Movement == Vector2.zero)
        {
            isWalking = false;
            isIdle = true;
        }
        else
        {
            isWalking = true;
            isIdle = false;
        }
    }

    #endregion

    #region - Jumping -

    private void Jump()
    {
        if(!characterController.isGrounded)
        {
            return;
        }

        if(playerStance == PlayerStance.Crouch || playerStance == PlayerStance.Prone)
        {
            if(StanceCheck(playerCrouchStance.StanceCollider.height))
            {
                return;
            }
            if(StanceCheck(playerStandStance.StanceCollider.height))
            {
                playerStance = PlayerStance.Crouch;
                return;
            }
            
            playerStance = PlayerStance.Stand;
            return;
        }

        // Jump
        isJumping = true;
        jumpingForce = Vector3.up * jumpingHeight;
        playerGravity = 0;

        if(isSprinting)
        {
            isSprinting = false;
            isSprintJump = true;
        }
    }

    private void CalculateJump()
    {
        jumpingForce = Vector3.SmoothDamp(jumpingForce, Vector3.zero, ref jumpingForceVelocity, jumpingFalloff);

        // Setting delay so that the isGroundedTrigger does not immediately turn true after jumping
        if(!isJumping)
        {
            fallingDelay = 0;
        }
        else
        {
            fallingDelay += Time.deltaTime;
        }

        // This isGrounded comes from CharacterControllerScr, not the CharacterController component!!!
        if(characterController.isGrounded && isJumping && fallingDelay > 0.01f)        // Landing
        {            
            // Debug.Log("Trigger Land");
            isSprintJump = false;
            isJumping = false;
        }
        else if(characterController.isGrounded && !isJumping)                          // Falling
        {
            // Debug.Log("Trigger Falling");
            isJumping = true;
        }
    }

    #endregion

    #region - Stance -

    private void CalculateStance()
    {
        var currentStance = playerStandStance;
        isStand = true;
        isCrouch = false;
        isProne = false;

        if(playerStance == PlayerStance.Crouch)
        {
            currentStance = playerCrouchStance;
            isStand = false;
            isCrouch = true;
            isProne = false;
        }
        else if(playerStance == PlayerStance.Prone)
        {
            currentStance = playerProneStance;
            isStand = false;
            isCrouch = false;
            isProne = true;
        }

        cameraHeight = Mathf.SmoothDamp(headPosition.localPosition.y, currentStance.CameraHeight, ref cameraHeightVelocity, playerStanceSmoothing);
        headPosition.localPosition = new Vector3(headPosition.localPosition.x, cameraHeight, headPosition.localPosition.z);

        characterController.height = Mathf.SmoothDamp(characterController.height, currentStance.StanceCollider.height, ref stanceCapsuleHeightVelocity, playerStanceSmoothing);
        characterController.center = Vector3.SmoothDamp(characterController.center, currentStance.StanceCollider.center, ref stanceCapsuleCenterVelocity, playerStanceSmoothing);
    }

    private void Crouch()
    {
        if(isSprinting)
        {
            isSprinting = false;
        }

        if(playerStance == PlayerStance.Crouch)
        {
            if(StanceCheck(playerStandStance.StanceCollider.height))
            {
                return;
            }

            playerStance = PlayerStance.Stand;
            return;
        }

        if(StanceCheck(playerCrouchStance.StanceCollider.height))
        {
            return;
        }
        
        playerStance = PlayerStance.Crouch;
    }

    private void Prone()
    {
        if(isSprinting)
        {
            isSprinting = false;
        }
        
        if(playerStance == PlayerStance.Prone)
        {
            if(StanceCheck(playerStandStance.StanceCollider.height) || StanceCheck(playerCrouchStance.StanceCollider.height))
            {
                return;
            }

            playerStance = PlayerStance.Stand;
            return;
        }

        playerStance = PlayerStance.Prone;
    }

    private bool StanceCheck(float stanceCheckHeight)
    {
        var start = new Vector3(feetTransform.position.x, feetTransform.position.y + characterController.radius + stanceCheckErrorMargine, feetTransform.position.z);
        var end = new Vector3(feetTransform.position.x, feetTransform.position.y - characterController.radius - stanceCheckErrorMargine + stanceCheckHeight, feetTransform.position.z);

        return Physics.CheckCapsule(start, end, characterController.radius, playerMask);
    }

    #endregion

    #region - Sprinting -

    private void ToggleSprint()
    {        
        if(playerStance == PlayerStance.Crouch || playerStance == PlayerStance.Prone)
        {
            if(StanceCheck(playerCrouchStance.StanceCollider.height))
            {
                return;
            }
            if(StanceCheck(playerStandStance.StanceCollider.height))
            {
                playerStance = PlayerStance.Crouch;
                return;
            }
            
            playerStance = PlayerStance.Stand;
        }
        
        if(input_Movement.y <= 0.2f)
        {
            isSprinting = false;
            return;
        }
        
        isSprinting = true;
    }

    private void CalculateSprint()
    {
        if(isSprinting)
        {
            // This will ensure to stand while sprinting. Without this it player might sprint while changing the stance to crouch or prone, resulting in sprinting while in crouch or prone position
            if(playerStance == PlayerStance.Crouch || playerStance == PlayerStance.Prone)
            {
                playerStance = PlayerStance.Stand;
            }
        }
    }

    private void StopSprint()
    {
        if(isSprintingHold)
        {
            isSprinting = false;
        }
    }

    #endregion
}
