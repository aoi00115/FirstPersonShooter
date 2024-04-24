using UnityEngine;
using static Models;


public class WeaponController : MonoBehaviour
{
    private CharacterControllerScr characterController;

    [Header("References")]
    public Animator universalAnimationController;

    [Header("Settings")]
    public WeaponSettingsModel settings;

    bool isInitialised;

    Vector3 targetWeaponRotation;
    Vector3 targetWeaponRotationVelocity;

    Vector3 newWeaponRotation;
    Vector3 newWeaponRotationVelocity;

    Vector3 targetWeaponMovementRotation;
    Vector3 targetWeaponMovementRotationVelocity;

    Vector3 newWeaponMovementRotation;
    Vector3 newWeaponMovementRotationVelocity;

    private bool isJumping;
    private float fallingDelay;
    
    private void Start()
    {
        newWeaponRotation = transform.localRotation.eulerAngles;
    }

    public void Initialise(CharacterControllerScr CharacterController)
    {
        characterController = CharacterController;
        isInitialised = true;
    }

    private void Update()
    {
        if(!isInitialised)
        {
            return;
        }

        CalculateWeaponRotation();
        SetWeaponAnimations();
    }

    public void TriggerJump()
    {
        // Debug.Log("Trigger Jumping");
        universalAnimationController.SetTrigger("JumpingTrigger");
        isJumping = true;     // Jumping
    }

    private void CalculateWeaponRotation()
    {
        // Camera rotation when looking around
        targetWeaponRotation.y += settings.SwayAmount * (settings.SwayXInverted ? -characterController.input_View.x : characterController.input_View.x) * Time.deltaTime;
        targetWeaponRotation.x += settings.SwayAmount * (settings.SwayYInverted ? characterController.input_View.y : -characterController.input_View.y) * Time.deltaTime;

        targetWeaponRotation.x = Mathf.Clamp(targetWeaponRotation.x, -settings.SwayClampX, settings.SwayClampX);
        targetWeaponRotation.y = Mathf.Clamp(targetWeaponRotation.y, -settings.SwayClampY, settings.SwayClampY);
        targetWeaponRotation.z = targetWeaponRotation.y;

        // Smooth damping for resetting the weapon sway(↑) and for setting weapon sway(↓)
        targetWeaponRotation = Vector3.SmoothDamp(targetWeaponRotation, Vector3.zero, ref targetWeaponRotationVelocity, settings.SwayResetSmoothing);
        newWeaponRotation = Vector3.SmoothDamp(newWeaponRotation, targetWeaponRotation, ref newWeaponRotationVelocity, settings.SwaySmoothing);

        // Camera rotation along z axis when moving side to side 
        targetWeaponMovementRotation.z = settings.MovementSwayX * -characterController.input_Movement.x;
        targetWeaponMovementRotation.x = settings.MovementSwayY * -characterController.input_Movement.y;

        targetWeaponMovementRotation = Vector3.SmoothDamp(targetWeaponMovementRotation, Vector3.zero, ref targetWeaponMovementRotationVelocity, settings.SwayResetSmoothing);
        newWeaponMovementRotation = Vector3.SmoothDamp(newWeaponMovementRotation, targetWeaponMovementRotation, ref newWeaponMovementRotationVelocity, settings.MovementSwaySmoothing);

        // Combining both weapon sway and movement sway
        transform.localRotation = Quaternion.Euler(newWeaponRotation + newWeaponMovementRotation);
    }

    private void SetWeaponAnimations()
    {
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
        if(characterController.characterController.isGrounded && isJumping && fallingDelay > 0.01f)        // Landing
        {            
            // Debug.Log("Trigger Land");
            universalAnimationController.Play("Landing");
            isJumping = false;
        }
        else if(!characterController.characterController.isGrounded && !isJumping)                          // Falling
        {
            // Debug.Log("Trigger Falling");
            universalAnimationController.SetTrigger("FallingTrigger");
            isJumping = true;
        }
        
        universalAnimationController.SetBool("isIdle", characterController.isIdle);
        universalAnimationController.SetFloat("WalkingAnimationSpeed", characterController.walkingAnimationSpeed);
    }
}
