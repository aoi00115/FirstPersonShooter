using UnityEngine;
using TMPro;
using static Models;


public class WeaponController : MonoBehaviour
{
    private CharacterControllerScr characterController;
    private DefaultInput defaultInput;

    [Header("References")]
    public Animator universalAnimationController;

    [Header("HUD Settings")]
    public TMP_Text weaponNameTMP;
    public TMP_Text ammoCountTMP;
    public TMP_Text ammoReserveCountTMP;
    public RectTransform crossHairType_CrossHair;
    public RectTransform crossHairType_Dot;

    [Header("Weapon Settings")]
    public GameObject[] weaponArray;
    private int weaponIndex;
    public GameObject melee;
    public GameObject lethal;
    public GameObject tactical;
    public GameObject currentWeapon;
    public IFireable fireable;
    public IDisplayable displayable;

    [Header("Weapon Sway Settings")]
    public WeaponSettingsModel settings;

    bool isInitialised;

    [HideInInspector]
    public Vector3 targetWeaponRotation;
    [HideInInspector]
    public Vector3 targetWeaponRotationVelocity;

    [HideInInspector]
    public Vector3 newWeaponRotation;
    [HideInInspector]
    public Vector3 newWeaponRotationVelocity;

    [HideInInspector]
    public Vector3 targetWeaponMovementRotation;
    [HideInInspector]
    public Vector3 targetWeaponMovementRotationVelocity;

    [HideInInspector]
    public Vector3 newWeaponMovementRotation;
    [HideInInspector]
    public Vector3 newWeaponMovementRotationVelocity;

    private bool isJumping;
    private float fallingDelay;

    [Header("Weapon Idle Sway")]
    public Transform weaponIdleSwayObject;
    public float idleSwayAmountA = 1;
    public float idleSwayAmountB = 2;
    public float idleSwayScale = 50;
    public float adsIdleSwayScale = 600;
    public float idleSwayLerpSpeed = 14;
    private float idleSwayTime;
    public Vector3 idleSwayPosition;

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

        CalculateWeaponSway();
        CalculateWeaponIdleSway();
        SetWeaponAnimations();

        CalculateCurrentWeapon();        

        if(currentWeapon != null && weaponNameTMP != null)
        {
            displayable.DisplayWeaponStats(weaponNameTMP, ammoCountTMP, ammoReserveCountTMP);
            displayable.DisplayCrossHair(crossHairType_CrossHair, crossHairType_Dot);
        }
        else if(currentWeapon == null && weaponNameTMP != null)
        {
            weaponNameTMP.text = "null";
            ammoCountTMP.text = "null";
            ammoReserveCountTMP.text = "null";
        }
        else if(currentWeapon != null && weaponNameTMP == null)
        {

        }
    }

    public void TriggerJump()
    {
        // Debug.Log("Trigger Jumping");
        // universalAnimationController.SetTrigger("JumpingTrigger");
        isJumping = true;     // Jumping
    }

    private void CalculateWeaponSway()
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

        // Weapon rotation along z axis when moving side to side 
        // Disable rotation along x axis and set the smoothing to 0.05f so that it snaps back to 0 quicker leaving the smoothing of the rotation along other axises un-touched when ADS
        targetWeaponMovementRotation.z = settings.MovementSwayX * -characterController.input_Movement.x;
        if(fireable.CalculateADSIn() || fireable.CalculateADS())
        {
            targetWeaponMovementRotation.x = 0;

            targetWeaponMovementRotation.x = Mathf.SmoothDamp(targetWeaponMovementRotation.x, 0, ref targetWeaponMovementRotationVelocity.x, 0.05f);
            newWeaponMovementRotation.x = Mathf.SmoothDamp(newWeaponMovementRotation.x, targetWeaponMovementRotation.x, ref newWeaponMovementRotationVelocity.x, 0.05f);
            targetWeaponMovementRotation.y = Mathf.SmoothDamp(targetWeaponMovementRotation.y, 0, ref targetWeaponMovementRotationVelocity.y, settings.SwayResetSmoothing);
            newWeaponMovementRotation.y = Mathf.SmoothDamp(newWeaponMovementRotation.y, targetWeaponMovementRotation.y, ref newWeaponMovementRotationVelocity.y, settings.MovementSwaySmoothing);
            targetWeaponMovementRotation.z = Mathf.SmoothDamp(targetWeaponMovementRotation.z, 0, ref targetWeaponMovementRotationVelocity.z, settings.SwayResetSmoothing);
            newWeaponMovementRotation.z = Mathf.SmoothDamp(newWeaponMovementRotation.z, targetWeaponMovementRotation.z, ref newWeaponMovementRotationVelocity.z, settings.MovementSwaySmoothing);
        }
        else
        {
            targetWeaponMovementRotation.x = settings.MovementSwayY * -characterController.input_Movement.y;
            
            targetWeaponMovementRotation = Vector3.SmoothDamp(targetWeaponMovementRotation, Vector3.zero, ref targetWeaponMovementRotationVelocity, settings.SwayResetSmoothing);
            newWeaponMovementRotation = Vector3.SmoothDamp(newWeaponMovementRotation, targetWeaponMovementRotation, ref newWeaponMovementRotationVelocity, settings.MovementSwaySmoothing);
        }

        // Combining both weapon sway and movement sway
        settings.SwayPoint.localRotation = Quaternion.Euler(newWeaponRotation + newWeaponMovementRotation);
    }

    private void CalculateWeaponIdleSway()
    {        
        var targetPosition = LissajousCurve(idleSwayTime, idleSwayAmountA, idleSwayAmountB) / (fireable.CalculateADS() ? adsIdleSwayScale : idleSwayScale);

        idleSwayPosition = Vector3.Lerp(idleSwayPosition, targetPosition, Time.smoothDeltaTime * idleSwayLerpSpeed);
        idleSwayTime += Time.deltaTime;
        if(idleSwayTime > 6.3f)
        {
            idleSwayTime = 0;
        }

        weaponIdleSwayObject.localPosition = idleSwayPosition;
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

        fireable.Sprint(characterController.isSprinting);
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
        else if(weaponArray[1].transform.childCount > 0)
        {
            currentWeapon = weaponArray[1];
            weaponIndex = 1;
            CalculateCurrentWeapon();
            fireable.SetUp();
        }
        else
        {
            currentWeapon = null;
        }
    }

    public void SwitchWeapons()
    {
        fireable.Draw();
        
        // weaponIndex++;
        // if(weaponIndex >= weaponArray.Length)
        // {
        //     weaponIndex = 0;
        // }
        // currentWeapon = weaponArray[weaponIndex];
        // CalculateCurrentWeapon();
        // fireable.SetUp();
    }

    // Assigning IFireable
    private void CalculateCurrentWeapon()
    {
        fireable = currentWeapon.transform.GetChild(0).GetComponent<IFireable>();
        displayable = currentWeapon.transform.GetChild(0).GetComponent<IDisplayable>();
    }
}
