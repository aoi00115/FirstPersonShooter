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
    public Transform headPosition;
    public Transform feetTransform;
    public Camera camera;
    public Transform cameraHolder;

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
    public WeaponController weaponController;

    public float walkingAnimationSpeed;
    public float smoothedWalkingAnimationSpeed;
    private float smoothedWalkingAnimationSpeedVelocity;

    [HideInInspector]
    public bool isFalling;
    private Vector3 jumpingMomentum;

    private Vector3 headBobbingStartPos;


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

        // Weapon Action
        defaultInput.Character.Fire.performed += e => weaponController.fireable.Fire();
        defaultInput.Character.Reload.performed += e => weaponController.fireable.Reload();
        defaultInput.Character.ADSIn.performed += e => weaponController.fireable.ADSIn();
        defaultInput.Character.ADSOut.performed += e => weaponController.fireable.ADSOut();
        defaultInput.Character.SwitchWeapons.performed += e => weaponController.fireable.PutAway();

        defaultInput.Enable();

        newCameraRotation = headPosition.localRotation.eulerAngles;
        newCharacterRotation = transform.localRotation.eulerAngles;

        characterController = GetComponent<CharacterController>();

        cameraHeight = headPosition.localPosition.y;

        headBobbingStartPos = cameraHolder.localPosition;

        if(weaponController)
        {
            weaponController.Initialise(this);
        }

        Cursor.lockState = CursorLockMode.Locked;
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

        CalculateHeadBobbing();

        camera.fieldOfView = playerSettings.FieldOfView;

        Debug.Log(Mathf.Round(smoothedWalkingAnimationSpeed * 10f) / 10f);
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

        headPosition.localRotation = Quaternion.Euler(newCameraRotation);
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

        // Move the player position with characterController.Move
        characterController.Move(movementSpeed);

        // if(characterController.isGrounded)  
        // {
        //     characterController.Move(movementSpeed);
        // }
        // else    // Carries the momentum when jumping and move the player position with characterController.Move
        // {
        //     characterController.Move(movementSpeed + (jumpingMomentum * Time.deltaTime));
        // }
        
        
        // Setting walking animation speed depending on how fast player is moving
        walkingAnimationSpeed = characterController.velocity.magnitude / playerSettings.WalkingForwardSpeed; // By multiplying it by "playerSettings.SpeedEffector" It'll play the animation at the speed of one no matter the stance  
        smoothedWalkingAnimationSpeed = Mathf.SmoothDamp(smoothedWalkingAnimationSpeed, walkingAnimationSpeed, ref smoothedWalkingAnimationSpeedVelocity, 0.1f); // Smoothed walkingAnimationSpeed for head bobbing and cross hair spreading
        
        if(walkingAnimationSpeed > 1)
        {
            walkingAnimationSpeed = 1;
        }

        if(smoothedWalkingAnimationSpeed > 1)
        {
            smoothedWalkingAnimationSpeed = 1;
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
        weaponController.TriggerJump();
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

        cameraHeight = Mathf.SmoothDamp(headPosition.localPosition.y, currentStance.CameraHeight, ref cameraHeightVelocity, playerStanceSmoothing);
        headPosition.localPosition = new Vector3(headPosition.localPosition.x, cameraHeight, headPosition.localPosition.z);

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
        // Return when ADS
        if(weaponController.fireable.CalculateADS() || weaponController.fireable.CalculateADSIn())
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
        }
        
        if(input_Movement.y <= 0.2f)
        {
            isSprinting = false;
            return;
        }
        
        // Timer has to have less than 2 seconds to sprint when the limit is on. Sprint immediately if the limit is off.
        if(isLimitedSprint)
        {            
            if(timer < playerSettings.SprintableTiming)
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

            // This will ensure to stand while sprinting. Without this it player might sprint while changing the stance to crouch or prone, resulting in sprinting while in crouch or prone position
            if(playerStance == PlayerStance.Crouch || playerStance == PlayerStance.Prone)
            {
                playerStance = PlayerStance.Stand;
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

    #region - Head Bobbing -

    private void CalculateHeadBobbing()
    {
        if(playerSettings.enableHeadBobbing)
        {
            CheckMotion();
            ResetPosition();
            if(newCameraRotation.x < 60 && newCameraRotation.x > -60)
            { 
                cameraHolder.transform.LookAt(FocusTarget());
            }
        }
    }

    private void PlayMotion(Vector3 motion)
    {
        cameraHolder.localPosition += motion; 
    }

    private void CheckMotion()
    {
        float speed = new Vector3(characterController.velocity.x, 0, characterController.velocity.z).magnitude;

        if(speed < playerSettings.HeadBobbingToggleSpeed) 
        {
            return;
        }
        if(!characterController.isGrounded) 
        {
            return;
        }

        PlayMotion(FootStepMotion());
    }

    private Vector3 FootStepMotion()
    {
        Vector3 pos = Vector3.zero;
        float frequency = 0;
        float amplitude = 0;

        if(isSprinting)
        {
            amplitude = (playerSettings.HeadBobbingAmplitude * (Mathf.Round(smoothedWalkingAnimationSpeed * 10f) / 10f)) * 2.5f;
            frequency = (playerSettings.HeadBobbingFrequency * (Mathf.Round(smoothedWalkingAnimationSpeed * 10f) / 10f)) * 1.5f;
        }
        else
        {
            amplitude = playerSettings.HeadBobbingAmplitude * (Mathf.Round(smoothedWalkingAnimationSpeed * 10f) / 10f);
            frequency = playerSettings.HeadBobbingFrequency * (Mathf.Round(smoothedWalkingAnimationSpeed * 10f) / 10f);
        }

        // Fix this part!!! So that it has smoother transition of head bobbing when fully looking up or down. Or fix the weird head bobbing problem as a whole
        // if(newCameraRotation.x < 83 && newCameraRotation.x > -83)
        // {            
        pos.y += Mathf.Sin(Time.time * frequency) * (amplitude / 500);
        pos.x += Mathf.Cos(Time.time * frequency / 2) * ((amplitude * 2) / 500);
        // }

        return pos;
    }

    private void ResetPosition()
    {
        if(cameraHolder.localPosition == headBobbingStartPos) 
        { 
            return;
        }
        cameraHolder.localPosition = Vector3.Lerp(cameraHolder.localPosition, headBobbingStartPos, 10 * Time.deltaTime);
    }

    private Vector3 FocusTarget()
    {
        Vector3 pos = new Vector3(transform.position.x, transform.position.y + headPosition.localPosition.y, transform.position.z);
        pos += headPosition.forward * 15.0f;
        return pos;
    }

    #endregion
}
