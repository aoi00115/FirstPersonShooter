using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Models;

public class CharacterControllerScr : MonoBehaviour
{
    [HideInInspector]
    public CharacterController characterController;
    private DefaultInput defaultInput;
    [HideInInspector]
    public Vector2 input_Movement;
    [HideInInspector]
    public Vector2 input_View;

    private Vector3 newCameraRotation;
    private Vector3 newCharacterRotation;

    private float timer = 0;

    [Header("References")]
    public Transform cameraHolder;
    public Transform feetTransform;

    [Header("Settings")]
    public PlayerSettingsModel playerSettings;
    public float viewClampYMin = -89;
    public float viewClampYMax = 89;
    public LayerMask playerMask;
    public LayerMask groundMask;

    [Header("Gravity")]
    public float gravityAmount;
    public float gravityMin;
    private float playerGravity;

    public Vector3 jumpingForce;
    private Vector3 jumpingForceVelocity;

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

    [HideInInspector]
    public bool isSprinting;
    private bool isLimitedSprint;
    [HideInInspector]
    public bool isWalking;
    [HideInInspector]
    public bool isIdle;
    [HideInInspector]
    public bool isStand;
    [HideInInspector]
    public bool isCrouch;
    [HideInInspector]
    public bool isProne;

    private Vector3 newMovementSpeed;
    private Vector3 newMovementSpeedVelocity;

    [Header("Weapon")]
    public WeaponController currentWeapon;

    public float walkingAnimationSpeed;

    [HideInInspector]
    public bool isFalling;
    private Vector3 jumpingMomentum;

    #region - Awake -

    // Awake method is called when the script instance is being loaded
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

        newCameraRotation = cameraHolder.localRotation.eulerAngles;
        newCharacterRotation = transform.localRotation.eulerAngles;

        characterController = GetComponent<CharacterController>();

        cameraHeight = cameraHolder.localPosition.y;

        if(currentWeapon)
        {
            currentWeapon.Initialise(this);
        }
    }

    #endregion

    #region - Update -

    private void Update()
    {
        CalculateView();
        CalculateMovement();
        CalculateJump();
        CalculateStance();
        CalculateSprint();
    }

    #endregion

    #region - View / Movement -

    private void CalculateView()
    {
        newCharacterRotation.y += playerSettings.ViewXSensitivity * (playerSettings.ViewXInverted ? -input_View.x : input_View.x) * Time.deltaTime;
        transform.localRotation = Quaternion.Euler(newCharacterRotation);

        // If the boolean playerSettings.ViewYInverted is true the left side of the options followed by ? is selected and vice versa
        newCameraRotation.x += playerSettings.ViewYSensitivity * (playerSettings.ViewYInverted ? input_View.y : -input_View.y) * Time.deltaTime;
        newCameraRotation.x = Mathf.Clamp(newCameraRotation.x, viewClampYMin, viewClampYMax);

        cameraHolder.localRotation = Quaternion.Euler(newCameraRotation);
    }

    private void CalculateMovement()
    {
        if(input_Movement.y <= 0.2f)
        {
            isSprinting = false;
        }

        var verticalSpeed = playerSettings.WalkingForwardSpeed;
        var horizontalSpeed = playerSettings.WalkingStrafeSpeed;

        // Multiplying player movement speed by sprinting speed when sprinting
        if(isSprinting)
        {
            verticalSpeed = playerSettings.RunningForwardSpeed;
            horizontalSpeed = playerSettings.RunningStrafeSpeed;
        }

        // Speed effectors for when the player is in a different motion
        if(!characterController.isGrounded)
        {
            // Remove comment out to slow down the player when jumping
            // playerSettings.SpeedEffector = playerSettings.FallingSpeedEffector;
        }
        else if (playerStance == PlayerStance.Crouch)
        {
            playerSettings.SpeedEffector = playerSettings.CrouchSpeedEffector;
        }
        else if (playerStance == PlayerStance.Prone)
        {
            playerSettings.SpeedEffector = playerSettings.ProneSpeedEffector;
        }
        else
        {
            playerSettings.SpeedEffector = 1;
        }

        verticalSpeed *= playerSettings.SpeedEffector;
        horizontalSpeed *= playerSettings.SpeedEffector;

        newMovementSpeed = Vector3.SmoothDamp(newMovementSpeed, new Vector3(horizontalSpeed * input_Movement.x * Time.deltaTime, 0, verticalSpeed * input_Movement.y * Time.deltaTime), ref newMovementSpeedVelocity, characterController.isGrounded ? playerSettings.MovementSmoothing : playerSettings.FallingSmoothing);
        var movementSpeed = transform.TransformDirection(newMovementSpeed);

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

        // These two lines are for enabling jump while moving
        movementSpeed.y += playerGravity;
        movementSpeed += jumpingForce * Time.deltaTime;

        if(characterController.isGrounded)  // Move the player position with characterController.Move
        {
            characterController.Move(movementSpeed);
        }
        else    // Carries the momentum when jumping and move the player position with characterController.Move
        {
            characterController.Move(movementSpeed + (jumpingMomentum * Time.deltaTime));
        }
        
        
        // Setting walking animation speed depending on how fast player is moving
        walkingAnimationSpeed = characterController.velocity.magnitude / playerSettings.WalkingForwardSpeed; // By multiplying it by "playerSettings.SpeedEffector" It'll play the animation at the speed of one no matter the stance  
        
        if(walkingAnimationSpeed > 1)
        {
            walkingAnimationSpeed = 1;
        }

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

    private void CalculateJump()
    {
        jumpingForce = Vector3.SmoothDamp(jumpingForce, Vector3.zero, ref jumpingForceVelocity, playerSettings.JumpingFalloff);
    }

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
        jumpingForce = Vector3.up * playerSettings.JumpingHeight;
        playerGravity = 0;
        currentWeapon.TriggerJump();
        // Store the player's moving direction as vector3 and translate it into world coordinate
        jumpingMomentum = transform.TransformDirection(newMovementSpeed) * 50;
        if(isSprinting)
        {
            isSprinting = false;
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

        cameraHeight = Mathf.SmoothDamp(cameraHolder.localPosition.y, currentStance.CameraHeight, ref cameraHeightVelocity, playerStanceSmoothing);
        cameraHolder.localPosition = new Vector3(cameraHolder.localPosition.x, cameraHeight, cameraHolder.localPosition.z);

        characterController.height = Mathf.SmoothDamp(characterController.height, currentStance.StanceCollider.height, ref stanceCapsuleHeightVelocity, playerStanceSmoothing);
        characterController.center = Vector3.SmoothDamp(characterController.center, currentStance.StanceCollider.center, ref stanceCapsuleCenterVelocity, playerStanceSmoothing);
    }

    private void Crouch()
    {
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
        
        // Timer has to have less than 2 seconds to sprint when the limit is on. Sprint immediately if the limit is off.
        if(isLimitedSprint)
        {            
            if(timer < 1)
            {            
                isSprinting = true; 
                isLimitedSprint = false;
            }
            else
            {
                return;
            }
        }
        else
        {
            isSprinting = true; 
        }
    }

    private void CalculateSprint()
    {
        if(isSprinting)
        {
            // Increase the timer, and if the stamina ran out turn on the limit and disable sprinting
            if(playerSettings.StaminaDuration < timer)
            {
                isLimitedSprint = true;
                isSprinting = false;
            }
            else
            {
                timer += Time.deltaTime;
            }
        }
        else
        {
            // When not sprinting, reduce the time until 0
            if(timer > 0)
            {
                timer -= Time.deltaTime;
            }
            else
            {
                timer = 0;
            }
        }
    }

    private void StopSprint()
    {
        if(playerSettings.sprintingHold)
        {
            isSprinting = false;
        }
    }

    #endregion
}
