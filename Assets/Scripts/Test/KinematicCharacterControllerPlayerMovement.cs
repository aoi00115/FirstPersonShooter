using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;

namespace KinematicCharacterController.Test
{
    public enum BonusOrientationMethod
    {
        None,
        TowardsGravity,
        TowardsGroundSlopeAndGravity,
    }

    public enum PlayerStance
    {
        Stand,
        Crouch,
        Prone
    }

    [Serializable]
    public class CharacterStance
    {
        public float capsuleHeight;
        public float cameraHeight;
    }

    public class KinematicCharacterControllerPlayerMovement : MonoBehaviour, ICharacterController
    {
        public KinematicCharacterMotor Motor;
        private DefaultInput defaultInput;
        public Transform headPosition;
        public Transform cameraHolder;
        public Transform headBob;

        [Header("View")]
        public float viewXSensitivity = 8f;
        public float viewYSensitivity = 8f;
        public float viewClampYMax = 89f;
        public float viewClampYMin = -89f;
        public bool viewXInverted;
        public bool viewYInverted;
        private Vector3 newCameraRotation;

        [Header("Stable Movement")]
        public float StableMovementSmoothing = 15f;

        [Header("Stable Movement - Walking")]
        public float MaxStableWalkingForwardSpeed = 5f;
        public float MaxStableWalkingStrafeSpeed = 5f;

        [Header("Stable Movement - Sprinting")]
        public float MaxStableSprintingForwardSpeed = 10f;
        public float MaxStableSprintingStrafeSpeed = 10f;
        public float staminaDuration = 5f;
        public float sprintCooldownDuration = 2f;
        public bool enableStamina;
        [Tooltip("Enable this to sprint when holding sprint button")]
        public bool isSprintingHold;
        [HideInInspector] public bool isSprinting;
        private bool isSprintCooldownRequired;
        private float staminaTimer;

        [Header("Speed Effectors")]
        public float speedEffector = 1f;
        public float crouchSpeedEffector = 0.6f;
        public float proneSpeedEffector = 0.2f;

        [Header("Air Movement")]
        public float MaxAirMoveSpeed = 5f;
        public float AirAccelerationSpeed = 15f;
        public float Drag = 0f;

        [Header("Jumping")]
        public bool AllowJumpingWhenSliding = false;
        public float JumpUpSpeed = 10f;
        public float JumpScalableForwardSpeed = 0f;
        public float JumpPreGroundingGraceTime = 0.1f;
        public float JumpPostGroundingGraceTime = 0.1f;

        [Header("Stance")]
        public PlayerStance playerStance;
        public float playerStanceSmoothing = 0.1f;
        public float capsuleRadius;
        public CharacterStance playerStandStance;
        public CharacterStance playerCrouchStance;
        public CharacterStance playerProneStance;
        
        private float stanceCheckErrorMargine = 0.05f;
        private float currentCapsuleHeight;
        private float currentCameraHeight;
        private float currentCapsuleHeightVelocity;
        private float currentCameraHeightVelocity;
        [HideInInspector] public bool isStand;
        [HideInInspector] public bool isCrouch;
        [HideInInspector] public bool isProne;

        [Header("Head Bobbing")]
        public bool enableHeadBobbing;
        [Range(0, 1f)]
        public float headBobbingAmplitude = 0.5f;
        [Range(0, 30)]
        public float headBobbingFrequency = 15f;
        private float headBobbingToggleSpeed = 0.1f;
        private Vector3 headBobbingStartPos = Vector3.zero;

        [Header("Mask Settings")]
        public LayerMask playerMask;
        public LayerMask groundMask;

        [Header("Misc")]
        public List<Collider> IgnoredColliders = new List<Collider>();
        public BonusOrientationMethod BonusOrientationMethod = BonusOrientationMethod.None;
        public float BonusOrientationSharpness = 20f;
        public Vector3 Gravity = new Vector3(0, -30f, 0);
        public float walkingAnimationSpeed;

        [HideInInspector]
        public Vector2 input_Movement;
        [HideInInspector]
        public Vector2 input_View;
        private Vector3 PlanarDirection;
        private float _targetVerticalAngle;
        private Collider[] _probedColliders = new Collider[8];
        private Vector3 _moveInputVector;
        private Vector3 _lookInputVector;
        private bool _jumpRequested = false;
        private bool _jumpConsumed = false;
        private bool _jumpedThisFrame = false;
        private float _timeSinceJumpRequested = Mathf.Infinity;
        private float _timeSinceLastAbleToJump = 0f;
        private Vector3 _internalVelocityAdd = Vector3.zero;
        private bool _shouldBeCrouching = false;
        private bool _isCrouching = false;

        #region - Awake, Start, Update -
        void Awake()
        {
            // Assign the characterController to the motor
            Motor.CharacterController = this;

            ActivateInputAction();
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            // Put CalculateStance and whatnot in update
            SetInputs();
            CalculateSprint();
            CalculateStance();
            CalculateHeadBobbing();
        }

        void LateUpdate()
        {
            CalculateCameraPositionandRotation();
        }

        #endregion

        void ActivateInputAction()
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

            // View Variable Setup
            newCameraRotation = cameraHolder.localRotation.eulerAngles;
            // Find the planar direction by checking headPosition.forward to find out which direction is upside
            PlanarDirection = headPosition.forward;

            // Set cursor mode to locked
            Cursor.lockState = CursorLockMode.Locked;
        }

        #region - Sprinting -

        private void ToggleSprint()
        {        
            if(playerStance == PlayerStance.Crouch || playerStance == PlayerStance.Prone)
            {
                if(StanceCheck(playerCrouchStance.capsuleHeight))
                {
                    return;
                }
                if(StanceCheck(playerCrouchStance.capsuleHeight))
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
            
            // If enableStamina is true 
            if(enableStamina)
            {
                // Timer has to have less than 2 seconds to sprint when the limit is on. Sprint immediately if the limit is off.
                if(isSprintCooldownRequired)
                {
                    return;
                }
                else
                {
                    isSprinting = true; 
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
                // This will ensure to stand while sprinting. Without this it player might sprint while changing the stance to crouch or prone, resulting in sprinting while in crouch or prone position
                if(playerStance == PlayerStance.Crouch || playerStance == PlayerStance.Prone)
                {
                    playerStance = PlayerStance.Stand;
                }
            }

            if(enableStamina)
            {
                CalculateStamina();
            }
        }

        private void CalculateStamina()
        {
            if(isSprinting)
            {
                // Increase the timer, and if the stamina ran out set isSprintCooldownRequired to true and disable sprinting
                if(staminaDuration < staminaTimer)
                {
                    isSprintCooldownRequired = true;
                    isSprinting = false;
                }
                else
                {
                    staminaTimer += Time.deltaTime;
                }
            }
            else
            {
                // When not sprinting, reduce the time until 0
                if(staminaTimer > 0)
                {
                    staminaTimer -= Time.deltaTime;
                }
                else
                {
                    staminaTimer = 0;
                }

                // Set isSprintCooldownRequired to false after the cool down if isSprintCooldownRequired
                if(isSprintCooldownRequired)
                {            
                    if(staminaTimer < sprintCooldownDuration)
                    {
                        isSprintCooldownRequired = false;
                    }
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

        #region - Jump -

        void Jump()
        {
            // Change stance and return when in crouch or prone
            if(playerStance == PlayerStance.Crouch || playerStance == PlayerStance.Prone)
            {
                if(StanceCheck(playerCrouchStance.capsuleHeight))
                {
                    return;
                }
                if(StanceCheck(playerStandStance.capsuleHeight))
                {
                    playerStance = PlayerStance.Crouch;
                    return;
                }
                
                playerStance = PlayerStance.Stand;
                return;
            }

            _timeSinceJumpRequested = 0f;
            _jumpRequested = true;

            if(isSprinting)
            {
                isSprinting = false;
            }
        }

        #endregion

        #region - Stance -

        private void Crouch()
        {
            if(isSprinting)
            {
                isSprinting = false;
            }

            if(playerStance == PlayerStance.Crouch)
            {
                if(StanceCheck(playerStandStance.capsuleHeight))
                {
                    return;
                }

                playerStance = PlayerStance.Stand;
                return;
            }

            if(StanceCheck(playerCrouchStance.capsuleHeight))
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
                if(StanceCheck(playerStandStance.capsuleHeight) || StanceCheck(playerCrouchStance.capsuleHeight))
                {
                    return;
                }

                playerStance = PlayerStance.Stand;
                return;
            }

            playerStance = PlayerStance.Prone;
        }

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

            // Set headPosition according to the current camera hight
            currentCameraHeight = Mathf.SmoothDamp(headPosition.localPosition.y, currentStance.cameraHeight, ref currentCameraHeightVelocity, playerStanceSmoothing);
            headPosition.localPosition = new Vector3(headPosition.localPosition.x, currentCameraHeight, headPosition.localPosition.z);

            // Set capsule height according to the current camera hight
            currentCapsuleHeight = Mathf.SmoothDamp(currentCapsuleHeight, currentStance.capsuleHeight, ref currentCapsuleHeightVelocity, playerStanceSmoothing);
            Motor.SetCapsuleDimensions(capsuleRadius, currentCapsuleHeight, currentCapsuleHeight * 0.5f);
        }

        private bool StanceCheck(float stanceCheckHeight)
        {
            var start = new Vector3(transform.position.x, transform.position.y + capsuleRadius + stanceCheckErrorMargine, transform.position.z);
            var end = new Vector3(transform.position.x, transform.position.y - capsuleRadius - stanceCheckErrorMargine + stanceCheckHeight, transform.position.z);

            return Physics.CheckCapsule(start, end, capsuleRadius, playerMask);
        }

        #endregion

        #region - Inputs and Camera -

        // Read the value of wasd input and rotation of cameraHolder
        void SetInputs()
        {
            // Stop sprinting when walking backward and stopping
            if(input_Movement.y <= 0.2f)
            {
                isSprinting = false;
            }

            // Set speed effector 
            float verticalSpeed = MaxStableWalkingForwardSpeed;
            float horizontalSpeed = MaxStableWalkingStrafeSpeed;

            // Multiplying player movement speed by sprinting speed when sprinting
            if(isSprinting)
            {
                verticalSpeed = MaxStableSprintingForwardSpeed;
                horizontalSpeed = MaxStableSprintingStrafeSpeed;
            }

            // Speed effectors for when the player is in a different stance
            if(playerStance == PlayerStance.Crouch)
            {
                speedEffector = crouchSpeedEffector;
            }
            else if(playerStance == PlayerStance.Prone)
            {
                speedEffector = proneSpeedEffector;
            }
            else
            {
                speedEffector = 1;
            }

            verticalSpeed *= speedEffector;
            horizontalSpeed *= speedEffector;

            // Clamp input
            Vector3 moveInputVector = Vector3.ClampMagnitude(new Vector3(input_Movement.x, 0f, input_Movement.y), 1f);

            // Calculate camera direction and rotation on the character plane
            Vector3 cameraPlanarDirection = Vector3.ProjectOnPlane(cameraHolder.rotation * Vector3.forward, Motor.CharacterUp).normalized;
            if (cameraPlanarDirection.sqrMagnitude == 0f)
            {
                cameraPlanarDirection = Vector3.ProjectOnPlane(cameraHolder.rotation * Vector3.up, Motor.CharacterUp).normalized;
            }
            Quaternion cameraPlanarRotation = Quaternion.LookRotation(cameraPlanarDirection, Motor.CharacterUp);

            // Un-comment the "Separate Speed" to set different speed for forward and strafe
            // Original : Move and look inputs
            // _moveInputVector = cameraPlanarRotation * moveInputVector;
            // _lookInputVector = cameraPlanarDirection;

            // Separate Speed : Move and look inputs
            _moveInputVector = cameraPlanarRotation * new Vector3(moveInputVector.x * horizontalSpeed, 0, moveInputVector.z * verticalSpeed);
            _lookInputVector = cameraPlanarDirection;
        }

        // Change the camera's rotation and postion based on player's mouse input and player's position
        void CalculateCameraPositionandRotation()
        {
            // Move cameraHolder to the headPosition
            cameraHolder.position = headPosition.position;

            // Prevent moving the camera while the cursor isn't locked
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                input_View = Vector3.zero;
            }

            // Un-comment out here for the gravity adoptable mouse rotation
            // Process rotation input
            Quaternion rotationFromInput = Quaternion.Euler(headPosition.up * ((viewXInverted ? -input_View.x : input_View.x) * viewXSensitivity / 150));
            PlanarDirection = rotationFromInput * PlanarDirection;
            PlanarDirection = Vector3.Cross(headPosition.up, Vector3.Cross(PlanarDirection, headPosition.up));
            Quaternion planarRot = Quaternion.LookRotation(PlanarDirection, headPosition.up);                           // Planar rotatio = horizontal rotation
            _targetVerticalAngle += ((viewYInverted ? input_View.y : -input_View.y) * viewYSensitivity / 150);
            _targetVerticalAngle = Mathf.Clamp(_targetVerticalAngle, viewClampYMin, viewClampYMax);
            Quaternion verticalRot = Quaternion.Euler(_targetVerticalAngle, 0, 0);                                      // Vertical rotation = vertical rotation
            cameraHolder.localRotation = planarRot * verticalRot;

            // Un-comment out here for the traditional mouse rotation
            // newCameraRotation.y += viewXSensitivity * (viewXInverted ? -input_View.x : input_View.x) * Time.deltaTime;
            // // If the boolean viewYInverted is true the left side of the options followed by ? is selected and vice versa
            // newCameraRotation.x += viewYSensitivity * (viewYInverted ? input_View.y : -input_View.y) * Time.deltaTime;
            // newCameraRotation.x = Mathf.Clamp(newCameraRotation.x, viewClampYMin, viewClampYMax);
            // cameraHolder.localRotation = Quaternion.Euler(newCameraRotation);
        }

        #endregion

        #region - Head Bobbing -

        private void CalculateHeadBobbing()
        {
            if(enableHeadBobbing)
            {
                CheckMotion();
                float cameraHolderXRotationEulerAngle = cameraHolder.localRotation.eulerAngles.x > 180 ? cameraHolder.localRotation.eulerAngles.x - 360 : cameraHolder.localRotation.eulerAngles.x;
                if(cameraHolderXRotationEulerAngle < 60 && cameraHolderXRotationEulerAngle > -60)
                { 
                    headBob.transform.LookAt(FocusTarget());
                }
            }
        }

        private void PlayMotion(Vector3 motion)
        {
            headBob.localPosition += motion; 
        }

        private void CheckMotion()
        {
            float speed = walkingAnimationSpeed;

            ResetPosition();

            if(speed < headBobbingToggleSpeed || !Motor.GroundingStatus.IsStableOnGround) 
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
                amplitude = headBobbingAmplitude * walkingAnimationSpeed * 2.5f;
                frequency = headBobbingFrequency * walkingAnimationSpeed * 1.5f;
            }
            else
            {
                amplitude = headBobbingAmplitude * walkingAnimationSpeed;
                frequency = headBobbingFrequency * walkingAnimationSpeed;
            }

            // Fix this part!!! So that it has smoother transition of head bobbing when fully looking up or down. Or fix the weird head bobbing problem as a whole
            // if(newCameraRotation.x < 83 && newCameraRotation.x > -83)
            // {            
            pos.y += Mathf.Sin(Time.time * frequency) * (amplitude / 500);
            pos.x += Mathf.Cos(Time.time * frequency / 2) * (((amplitude / 500) * 2));
            // }

            return pos;
        }

        private void ResetPosition()
        {
            if(headBob.localPosition == headBobbingStartPos) 
            {
                return;
            }
            headBob.localPosition = Vector3.Lerp(headBob.localPosition, headBobbingStartPos, 10 * Time.deltaTime);
        }

        private Vector3 FocusTarget()
        {
            Vector3 pos = new Vector3(transform.position.x, transform.position.y + headPosition.localPosition.y, transform.position.z);
            pos += cameraHolder.forward * 15.0f;
            return pos;
        }

        #endregion

        #region - KCC -

        /// <summary>
        /// (Called by KinematicCharacterMotor during its update cycle)
        /// This is where you tell your character what its rotation should be right now. 
        /// This is the ONLY place where you should set the character's rotation
        /// </summary>
        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            // Set the player's current rotation to wherever the camera is facing taking the gravity direction into account
            currentRotation = Quaternion.LookRotation(_lookInputVector, Motor.CharacterUp);

            Vector3 currentUp = (currentRotation * Vector3.up);
            if (BonusOrientationMethod == BonusOrientationMethod.TowardsGravity)
            {
                // Rotate from current up to invert gravity
                Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, -Gravity.normalized, 1 - Mathf.Exp(-BonusOrientationSharpness * deltaTime));
                currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
            }
            else if (BonusOrientationMethod == BonusOrientationMethod.TowardsGroundSlopeAndGravity)
            {
                if (Motor.GroundingStatus.IsStableOnGround)
                {
                    Vector3 initialCharacterBottomHemiCenter = Motor.TransientPosition + (currentUp * Motor.Capsule.radius);

                    Vector3 smoothedGroundNormal = Vector3.Slerp(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal, 1 - Mathf.Exp(-BonusOrientationSharpness * deltaTime));
                    currentRotation = Quaternion.FromToRotation(currentUp, smoothedGroundNormal) * currentRotation;

                    // Move the position to create a rotation around the bottom hemi center instead of around the pivot
                    Motor.SetTransientPosition(initialCharacterBottomHemiCenter + (currentRotation * Vector3.down * Motor.Capsule.radius));
                }
                else
                {
                    Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, -Gravity.normalized, 1 - Mathf.Exp(-BonusOrientationSharpness * deltaTime));
                    currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
                }
            }
            else
            {
                Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, Vector3.up, 1 - Mathf.Exp(-BonusOrientationSharpness * deltaTime));
                currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
            }
        }

        /// <summary>
        /// (Called by KinematicCharacterMotor during its update cycle)
        /// This is where you tell your character what its velocity should be right now. 
        /// This is the ONLY place where you can set the character's velocity
        /// </summary>
        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            // Ground movement
            if (Motor.GroundingStatus.IsStableOnGround)
            {
                float currentVelocityMagnitude = currentVelocity.magnitude;

                Vector3 effectiveGroundNormal = Motor.GroundingStatus.GroundNormal;

                // Reorient velocity on slope
                currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, effectiveGroundNormal) * currentVelocityMagnitude;

                // Un-comment the "Separate Speed" to set different speed for forward and strafe
                // Original : Calculate target velocity
                // Vector3 inputRight = Vector3.Cross(_moveInputVector, Motor.CharacterUp);
                // Vector3 reorientedInput = Vector3.Cross(effectiveGroundNormal, inputRight).normalized * _moveInputVector.magnitude;
                // Vector3 targetMovementVelocity = reorientedInput * MaxStableWalkingForwardSpeed;

                // Separate Speed : Calculate target velocity
                Vector3 inputRight = Vector3.Cross(_moveInputVector, Motor.CharacterUp);
                Vector3 reorientedInput = Vector3.Cross(effectiveGroundNormal, inputRight).normalized * _moveInputVector.magnitude;
                Vector3 targetMovementVelocity = reorientedInput;

                // Smooth movement Velocity
                currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1f - Mathf.Exp(-StableMovementSmoothing * deltaTime));

                // Set walkingAnimationSpeed
                walkingAnimationSpeed = currentVelocity.magnitude / MaxStableWalkingForwardSpeed;
                if(walkingAnimationSpeed > 1)
                {
                    walkingAnimationSpeed = 1;
                }
                Debug.Log(currentVelocity.magnitude);
            }
            // Air movement
            else
            {
                // Add move input
                if (_moveInputVector.sqrMagnitude > 0f)
                {
                    Vector3 addedVelocity = _moveInputVector.normalized * AirAccelerationSpeed * deltaTime;

                    Vector3 currentVelocityOnInputsPlane = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp);

                    // Limit air velocity from inputs
                    if (currentVelocityOnInputsPlane.magnitude < MaxAirMoveSpeed)
                    {
                        // clamp addedVel to make total vel not exceed max vel on inputs plane
                        Vector3 newTotal = Vector3.ClampMagnitude(currentVelocityOnInputsPlane + addedVelocity, MaxAirMoveSpeed);
                        addedVelocity = newTotal - currentVelocityOnInputsPlane;
                    }
                    else
                    {
                        // Make sure added vel doesn't go in the direction of the already-exceeding velocity
                        if (Vector3.Dot(currentVelocityOnInputsPlane, addedVelocity) > 0f)
                        {
                            addedVelocity = Vector3.ProjectOnPlane(addedVelocity, currentVelocityOnInputsPlane.normalized);
                        }
                    }

                    // Prevent air-climbing sloped walls
                    if (Motor.GroundingStatus.FoundAnyGround)
                    {
                        if (Vector3.Dot(currentVelocity + addedVelocity, addedVelocity) > 0f)
                        {
                            Vector3 perpenticularObstructionNormal = Vector3.Cross(Vector3.Cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal), Motor.CharacterUp).normalized;
                            addedVelocity = Vector3.ProjectOnPlane(addedVelocity, perpenticularObstructionNormal);
                        }
                    }

                    // Apply added velocity
                    currentVelocity += addedVelocity;
                }

                // Gravity
                currentVelocity += Gravity * deltaTime;

                // Drag
                currentVelocity *= (1f / (1f + (Drag * deltaTime)));
            }

            // Handle jumping
            _jumpedThisFrame = false;
            _timeSinceJumpRequested += deltaTime;
            if (_jumpRequested)
            {
                // See if we actually are allowed to jump
                if (!_jumpConsumed && ((AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround) || _timeSinceLastAbleToJump <= JumpPostGroundingGraceTime))
                {
                    // Calculate jump direction before ungrounding
                    Vector3 jumpDirection = Motor.CharacterUp;
                    if (Motor.GroundingStatus.FoundAnyGround && !Motor.GroundingStatus.IsStableOnGround)
                    {
                        jumpDirection = Motor.GroundingStatus.GroundNormal;
                    }

                    // Makes the character skip ground probing/snapping on its next update. 
                    // If this line weren't here, the character would remain snapped to the ground when trying to jump. Try commenting this line out and see.
                    Motor.ForceUnground();

                    // Add to the return velocity and reset jump state
                    currentVelocity += (jumpDirection * JumpUpSpeed) - Vector3.Project(currentVelocity, Motor.CharacterUp);
                    currentVelocity += (_moveInputVector * JumpScalableForwardSpeed);
                    _jumpRequested = false;
                    _jumpConsumed = true;
                    _jumpedThisFrame = true;
                }
            }

            // Take into account additive velocity
            if (_internalVelocityAdd.sqrMagnitude > 0f)
            {
                currentVelocity += _internalVelocityAdd;
                _internalVelocityAdd = Vector3.zero;
            }
        }

        /// <summary>
        /// This is called before the motor does anything
        /// </summary>
        public void BeforeCharacterUpdate(float deltaTime)
        {

        }

        /// <summary>
        /// This is called after the motor has finished its ground probing, but before PhysicsMover/Velocity/etc.... handling
        /// </summary>
        public void PostGroundingUpdate(float deltaTime)
        {
            // Handle landing and leaving ground
            if (Motor.GroundingStatus.IsStableOnGround && !Motor.LastGroundingStatus.IsStableOnGround)
            {
                OnLanded();
            }
            else if (!Motor.GroundingStatus.IsStableOnGround && Motor.LastGroundingStatus.IsStableOnGround)
            {
                OnLeaveStableGround();
            }
        }

        /// <summary>
        /// (Called by KinematicCharacterMotor during its update cycle)
        /// This is called after the character has finished its movement update
        /// </summary>
        public void AfterCharacterUpdate(float deltaTime)
        {
            // Handle jump-related values
            {
                // Handle jumping pre-ground grace period
                if (_jumpRequested && _timeSinceJumpRequested > JumpPreGroundingGraceTime)
                {
                    _jumpRequested = false;
                }

                if (AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround)
                {
                    // If we're on a ground surface, reset jumping values
                    if (!_jumpedThisFrame)
                    {
                        _jumpConsumed = false;
                    }
                    _timeSinceLastAbleToJump = 0f;
                }
                else
                {
                    // Keep track of time since we were last able to jump (for grace period)
                    _timeSinceLastAbleToJump += deltaTime;
                }
            }
        }

        /// <summary>
        /// This is called after when the motor wants to know if the collider can be collided with (or if we just go through it)
        /// </summary>
        public bool IsColliderValidForCollisions(Collider coll)
        {
            if (IgnoredColliders.Count == 0)
            {
                return true;
            }

            if (IgnoredColliders.Contains(coll))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// This is called when the motor's ground probing detects a ground hit
        /// </summary>
        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {

        }

        /// <summary>
        /// This is called when the motor's movement logic detects a hit
        /// </summary>
        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {

        }

        /// <summary>
        /// This is called after every move hit, to give you an opportunity to modify the HitStabilityReport to your liking
        /// </summary>
        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {

        }

        /// <summary>
        /// This is called when the character detects discrete collisions (collisions that don't result from the motor's capsuleCasts when moving)
        /// </summary>
        public void OnDiscreteCollisionDetected(Collider hitCollider)
        {

        }

        protected void OnLanded()
        {

        }

        protected void OnLeaveStableGround()
        {

        }

        #endregion
    }
}