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

    private bool isGroundedTrigger;
    private bool isFallingTrigger;
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

        // This isGrounded comes from CharacterControllerScr, not the CharacterController component!!!
        if(characterController.isGrounded && !isGroundedTrigger)
        {            
            isGroundedTrigger = true;
        }
        else if(!characterController.isGrounded && isGroundedTrigger)
        {            
            isGroundedTrigger = false;
        }
    }

    public void TriggerJump()
    {
        isGroundedTrigger = false;
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
        universalAnimationController.SetFloat("WalkingAnimationSpeed", characterController.walkingAnimationSpeed);
    }
}
