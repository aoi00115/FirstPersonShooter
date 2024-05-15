using UnityEngine;
using static Models;


public class WeaponController : MonoBehaviour
{
    private CharacterControllerScr characterController;
    private DefaultInput defaultInput;

    [Header("References")]
    public Animator universalAnimationController;

    [Header("Weapon Settings")]
    public GameObject[] weaponArray;
    private int weaponIndex;
    public GameObject currentWeapon;
    public IFireable fireable;

    [Header("Weapon Sway Settings")]
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

    [Header("Weapon Idle Sway")]
    public Transform weaponSwayObject;
    public float swayAmountA = 1;
    public float swayAmountB = 2;
    public float swayScale = 50;
    public float adsSwayScale = 600;
    public float swayLerpSpeed = 14;
    private float swayTime;
    public Vector3 swayPosition;
    public Transform swayPoint;
    public Transform adsSwayPoint;

    private void Start()
    {
        newWeaponRotation = transform.localRotation.eulerAngles;

        InitialWeaponSetUp();
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
        CalculateWeaponSway();
        SetWeaponAnimations();

        CalculateCurrentWeapon();
    }

    public void TriggerJump()
    {
        // Debug.Log("Trigger Jumping");
        // universalAnimationController.SetTrigger("JumpingTrigger");
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
        swayPoint.localRotation = Quaternion.Euler(newWeaponRotation + newWeaponMovementRotation);
        // Copying the rotation of swayPoint to adsSwayPoint too so that they syncronize in movement
        adsSwayPoint.localRotation = swayPoint.localRotation;
    }

    private void CalculateWeaponSway()
    {        
        var targetPosition = LissajousCurve(swayTime, swayAmountA, swayAmountB) / (fireable.CalculateADS() ? adsSwayScale : swayScale);

        swayPosition = Vector3.Lerp(swayPosition, targetPosition, Time.smoothDeltaTime * swayLerpSpeed);
        swayTime += Time.deltaTime;
        if(swayTime > 6.3f)
        {
            swayTime = 0;
        }

        weaponSwayObject.localPosition = swayPosition;
    }

    private Vector3 LissajousCurve(float Time, float A, float B)
    {
        return new Vector3(Mathf.Sin(Time), A * Mathf.Sin(B * Time + Mathf.PI));
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
            // universalAnimationController.Play("Landing");
            isJumping = false;
        }
        else if(!characterController.characterController.isGrounded && !isJumping)                          // Falling
        {
            // Debug.Log("Trigger Falling");
            // universalAnimationController.SetTrigger("FallingTrigger");
            isJumping = true;
        }
        
        // Based on the characterController.isIdle switch between idle and other animations
        fireable.Walk(characterController.walkingAnimationSpeed, characterController.isIdle);
    }

    private void InitialWeaponSetUp()
    {      
        if(weaponArray[0].transform.childCount > 0)
        {
            currentWeapon = weaponArray[0];
            weaponIndex = 0;
            CalculateCurrentWeapon();
            fireable.SetUp();
        }
        else
        {
            currentWeapon = weaponArray[1];
            weaponIndex = 1;
            CalculateCurrentWeapon();
            fireable.SetUp();
        }
    }

    private void SwitchWeapons()
    {
        weaponIndex++;
        if(weaponIndex >= weaponArray.Length)
        {
            weaponIndex = 0;
        }
        currentWeapon = weaponArray[weaponIndex];
        CalculateCurrentWeapon();
        fireable.SetUp();
    }

    // Assigning IFireable
    private void CalculateCurrentWeapon()
    {
        fireable = currentWeapon.transform.GetChild(0).GetComponent<IFireable>();
    }
}
