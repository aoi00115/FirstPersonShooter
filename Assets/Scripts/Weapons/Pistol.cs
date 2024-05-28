using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using static Weapons;

public class Pistol : MonoBehaviour, IFireable, IDisplayable
{
    [Header("Pistol Settings")]
    public Gun gun;
    
    // Start is called before the first frame update
    void Start()
    {
        gun.adsDuration += 0.01f;
    }

    // Update is called once per frame
    void Update()
    {
        CalculateADSTime();
        CalculateReloadTime();
        CalculateDrawTime();
        CalculatePutAwayTime();
        CalculateWeaponStance();
    }

    #region "Fire"

    public void Fire()
    {
        // Fire only after drawing, reloading or only not putting away
        if(gun.isDrawing || gun.isPuttingAway || gun.isReloading)
        {            
            return;
        }

        // When mag is empty, return
        if(gun.ammoCount <= 0)
        {
            gun.ammoCount = 0;
            return;
        }

        // If fired when running, stop running
        if(gun.characterController.isSprinting == true)
        {
            gun.characterController.isSprinting = false;
        }

        gun.fireCrossHairTimer = gun.crossHairResetDuration;

        gun.armsAnimator.Play("Arms Fire Blend Tree");
        gun.gunAnimator.Play(gameObject.name + "_Gun_Fire_Animation");

        gun.audioSource.PlayOneShot(gun.fireClip, 1);

        // Ejecting bullet casing
        GameObject instantiatedBulletCasing;
        instantiatedBulletCasing = Instantiate(gun.bulletCasing, gun.ejectionPoint.position, gun.ejectionPoint.rotation);
        Rigidbody bulletCasingRb = instantiatedBulletCasing.GetComponent<Rigidbody>();
        bulletCasingRb.AddRelativeForce(Random.Range(5.0f, 6.0f), Random.Range(2.0f, 4.0f), 0, ForceMode.Impulse);
        bulletCasingRb.AddRelativeTorque(0, Random.Range(0f, 20f), 0, ForceMode.Impulse);
        Destroy(instantiatedBulletCasing, 5);

        gun.ammoCount--;
    }

    #endregion

    #region "ADS"

    public void ADSIn()
    {
        // If ADS when reloading, set isADSIn true and do nothing
        if(gun.isReloading || gun.isDrawing)
        {
            gun.isTryingToADSWhileDoingSomethingElse = true;
            return;
        }
        else if(gun.isPuttingAway)
        {
            gun.isTryingToADSWhileDoingSomethingElse = true;
        }

        // If ADS when running, stop running
        if(gun.characterController.isSprinting == true)
        {
            gun.characterController.isSprinting = false;
        }

        gun.isADS = false;
        gun.isADSIn = true;
        gun.isADSOut = false;
        gun.armsAnimator.SetFloat("ADSSpeed", gun.adsSpeed);

        // When the animation is not reset when the timer is 0, play it back from 0 time frame, and when the timer is not 0, play the animation from where the animation is continued
        if(gun.adsTimer == 0)
        {
            gun.armsAnimator.Play(gameObject.name + "_ADS_Animation", -1, 0);
        }
        else
        {
            gun.armsAnimator.Play(gameObject.name + "_ADS_Animation");
        }
    }

    public void ADSOut()
    {
        // If ADSOut when reloading, set isADSIn false and do nothing
        if(gun.isReloading || gun.isDrawing)
        {
            gun.isTryingToADSWhileDoingSomethingElse = false;
            return;
        }
        gun.isTryingToADSWhileDoingSomethingElse = false;

        gun.isADS = false;
        gun.isADSOut = true;
        gun.isADSIn = false;
        gun.armsAnimator.SetFloat("ADSSpeed", -gun.adsSpeed);
    }

    public bool CalculateADS()
    {
        return gun.isADS;
    }

    public bool CalculateADSIn()
    {
        return gun.isADSIn;
    }

    public void CalculateADSTime()
    {
        if(gun.isADSIn)
        {
            // Increase the timer when aiming in, until it reaches up to ADSduration, and if it reaches, freeze the timer by setting adsTimer to ADSduration
            if(gun.adsDuration / gun.adsSpeed >= gun.adsTimer)
            {
                gun.adsTimer += Time.deltaTime;

                // Start counting up the ADS zoom timer when ads timer go past the ads zoom start time
                if(gun.adsZoomStartTime / gun.adsSpeed < gun.adsTimer)
                {
                    gun.adsZoomTimer += Time.deltaTime;
                }
            }
            else
            {
                gun.adsTimer = gun.adsDuration / gun.adsSpeed;
                gun.adsZoomTimer = (gun.adsDuration / gun.adsSpeed) - (gun.adsZoomStartTime / gun.adsSpeed);

                gun.isADS = true;
                gun.isADSIn = false;
                gun.armsAnimator.SetFloat("ADSSpeed", 0f);
            }
        }
        if(gun.isADSOut)
        {
            // Decrease the timer when aiming out, until it reaches to 0, if it reaches, set the timer to 0
            if(gun.adsTimer > 0)
            {
                gun.adsTimer -= Time.deltaTime;
            }
            else
            {
                gun.adsTimer = 0;
            }

            // Do the above but for ads zoom timer
            if(gun.adsZoomTimer > 0)
            {
                gun.adsZoomTimer -= Time.deltaTime;
            }
            else
            {
                gun.adsZoomTimer = 0;
            }
        }

        // When the ADS animation reaches zero time frame, stop the animation from playing by setting the speed to 0
        if(gun.adsTimer == 0)
        {
            gun.isADSOut = false;
            gun.armsAnimator.SetFloat("ADSSpeed", 0f);            
            // When not ADS (Hip fire) play the empty state
            gun.armsAnimator.Play("ADS Empty");
        }

        // Setting the float used in the blend tree in animator, it calculates the normalized ADS time from 0 to 1 where 0 being hip fire, and 1 being ADS
        gun.armsAnimator.SetFloat("ADSProgress", gun.adsTimer / (gun.adsDuration / gun.adsSpeed));

        // Change the fov according to the ads zoom progress
        gun.camera.fieldOfView = Mathf.Lerp(gun.characterController.playerSettings.FieldOfView, gun.characterController.playerSettings.FieldOfView - gun.adsZoom, (gun.adsZoomTimer / ((gun.adsDuration / gun.adsSpeed) - (gun.adsZoomStartTime / gun.adsSpeed))));

        // Change the ads position if there is any change in ads position
        if(gun.adsPosition != Vector3.zero)
        {
            gun.armsRig.localPosition = Vector3.Lerp(new Vector3(0, 0, -0.12f), new Vector3(gun.adsPosition.x, gun.adsPosition.y, gun.adsPosition.z - 0.12f), gun.adsTimer / (gun.adsDuration / gun.adsSpeed));
        }
    }

    #endregion

    #region "Reload"

    public void Reload()
    {
        // Return when ammo is full, or reesrved ammo is 0
        if(gun.ammoCount == gun.magCapacity || gun.ammoReserveCount == 0 || gun.isPuttingAway)
        {
            return;
        }

        // If reload when sprinting and isReloadWhileSprint is fault, stop running
        if(gun.characterController.isSprinting == true && !gun.isReloadWhileSprint)
        {
            gun.characterController.isSprinting = false;
        }

        // If reload when ADS, ADSout
        if(gun.isADS || gun.isADSIn)
        {
            ADSOut();
            gun.isTryingToADSWhileDoingSomethingElse = true;
        }

        if(!gun.isReloading)
        {
            gun.isReloading = true;
        }

        gun.armsAnimator.Play(gameObject.name + "_Arms_Reload_Animation");
        gun.gunAnimator.Play(gameObject.name + "_Gun_Reload_Animation");
        gun.cameraAnimator.Play(gameObject.name + "_Camera_Reload_Animation");
    }

    public void CalculateReloadTime()
    {
        if(gun.isReloading)
        {
            // Increase the timer when aiming in, until it reaches up to ADSduration, and if it reaches, freeze the timer by setting adsTimer to ADSduration
            if(gun.reloadDuration > gun.reloadTimer)
            {
                gun.reloadTimer += Time.deltaTime;                
            }
            else
            {
                // If the timer reaches the reload duration
                gun.reloadTimer = gun.reloadDuration;
                gun.isReloading = false;

                // If ADS when reloading the gun, reset everything and reADS
                if(gun.isTryingToADSWhileDoingSomethingElse)
                {
                    ADSIn();
                }
            }
        }
        else
        {
            // When reload is cancelled
            gun.reloadTimer = 0;
            gun.armsAnimator.Play("Reload Empty");
            gun.gunAnimator.Play("Reload Empty");
            gun.cameraAnimator.Play(gameObject.name + "_Camera_Idle_Animation");
        }

        if(gun.magInDuration <= gun.reloadTimer)
        {
            // Action after reloading goes below
            int loadedAmmo = gun.magCapacity - gun.ammoCount;
            if(gun.ammoReserveCount < loadedAmmo)
            {
                loadedAmmo = gun.ammoReserveCount;
            }
            gun.ammoReserveCount -= loadedAmmo;
            gun.ammoCount += loadedAmmo;
        }

        // Cancel reload if sprint and isReloadWhileSprint is fault
        if(gun.characterController.isSprinting && !gun.isReloadWhileSprint)
        {
            gun.isReloading = false;
        }
    }

    #endregion

    public void SwitchFireMode()
    {
        
    }

    #region "Draw"

    public void Draw()
    {        
        gun.armsAnimator.Play(gameObject.name + "_Draw_Animation", -1, 0);
        gun.isPuttingAway = false;
        gun.putAwayTimer = 0;
        gun.isDrawing = true;
    }

    public void CalculateDrawTime()
    {
        if(gun.isDrawing)
        {
            // Increase the timer when aiming in, until it reaches up to ADSduration, and if it reaches, freeze the timer by setting adsTimer to ADSduration
            if(gun.drawDuration > gun.drawTimer)
            {
                gun.drawTimer += Time.deltaTime;
            }
            else
            {
                gun.drawTimer = 0;
                gun.isDrawing = false;
                gun.armsAnimator.Play("Draw Empty");
                
                // If ADS when drawing the gun, it'll reset the timer to 0 and ADS from the start
                if(gun.isTryingToADSWhileDoingSomethingElse)
                {
                    ADSOut();
                    gun.adsTimer = 0;
                    ADSIn();
                }
            }
        }
    }

    #endregion

    #region "PutAway"

    public void PutAway()
    {        
        gun.armsAnimator.Play(gameObject.name + "_PutAway_Animation");
        if(!gun.isPuttingAway)
        {
            gun.isPuttingAway = !gun.isPuttingAway;            
            gun.armsAnimator.SetFloat("PutAwaySpeed", 1);
        }
        else
        {
            gun.isPuttingAway = !gun.isPuttingAway;
            gun.armsAnimator.SetFloat("PutAwaySpeed", -1);
        }

        gun.isReloading = false;
    }

    public void CalculatePutAwayTime()
    {
        if(gun.isPuttingAway)
        {
            // Increase the timer when aiming in, until it reaches up to ADSduration, and if it reaches, freeze the timer by setting adsTimer to ADSduration
            if(gun.putAwayDuration > gun.putAwayTimer)
            {
                gun.putAwayTimer += Time.deltaTime;                
            }
            else
            {
                // If the timer reaches the putaway duration
                gun.putAwayTimer = gun.putAwayDuration;

                gun.armsAnimator.SetFloat("PutAwaySpeed", 0f);

                // Action after putting the weapon away goes in below
                gun.weaponController.SwitchWeapons();

                // If PutAway when ADS,
                if(gun.isADS || gun.isADSIn)
                {
                    gun.isTryingToADSWhileDoingSomethingElse = true;
                }
            }
        }
        else
        {
            // Decrease the timer when aiming out, until it reaches to 0, if it reaches, set the timer to 0
            if(gun.putAwayTimer > 0)
            {
                gun.putAwayTimer -= Time.deltaTime;
            }
            else
            {
                gun.putAwayTimer = 0;

                gun.armsAnimator.SetFloat("PutAwaySpeed", 0f);            
                // When not ADS (Hip fire) play the empty state
                gun.armsAnimator.Play("PutAway Empty");
            }
        }
    }

    #endregion

    #region "Walk & Sprint"

    public void Walk(float walkingAnimationSpeed, bool isIdle)
    {
        gun.armsAnimator.SetBool("isIdle", isIdle);
        gun.armsAnimator.SetFloat("WalkingAnimationSpeed", walkingAnimationSpeed);
    }

    public void Sprint(bool isSprint)
    {
        gun.armsAnimator.SetBool("isSprint", isSprint);
    }

    #endregion
    
    #region "SetUp & Reset"

    public void SetUp()
    {
        // if(gun.inUse)
        // {
        //     gun.weaponController.currentWeapon = this.gameObject;
        // }

        // Setting up all the scripts and reference
        gun.characterController = transform.Find("../../../../../../").GetComponent<CharacterControllerScr>();
        gun.weaponController = transform.Find("../../").GetComponent<WeaponController>();

        gun.armsRig = transform.Find("../../WeaponSway/WeaponRecoil/WeaponStance/ArmsRig");
        gun.cameraRecoil = transform.Find("../../../");
        gun.camera = transform.Find("../../../CameraAnimator/Camera").GetComponent<Camera>();
        gun.weaponHolder = transform.Find("../../");
        gun.weaponSway = transform.Find("../../WeaponSway");
        gun.weaponRecoil = transform.Find("../../WeaponSway/WeaponRecoil");
        gun.weaponStance = transform.Find("../../WeaponSway/WeaponRecoil/WeaponStance");
        gun.socket = transform.Find("../../WeaponSway/WeaponRecoil/WeaponStance/ArmsRig/arms_rig/root/upper_arm_R/lower_arm_R/hand_R/" + gameObject.name + "Socket");
        gun.swayPoint = transform.Find("../../SwayPoints/" + gameObject.name + "SwayPoint");

        gun.gunAnimator = GetComponent<Animator>();
        gun.armsAnimator = transform.Find("../../WeaponSway/WeaponRecoil/WeaponStance/ArmsRig").GetComponent<Animator>();
        gun.cameraAnimator = transform.Find("../../../CameraAnimator").GetComponent<Animator>();

        gun.ejectionPoint = transform.Find("EjectionPoint");

        gun.gunAnimator.runtimeAnimatorController = gun.gunAnimatorController;
        gun.armsAnimator.runtimeAnimatorController = gun.armsAnimatorController;
        gun.cameraAnimator.runtimeAnimatorController = gun.cameraAnimatorController;

        gun.audioSource = GetComponent<AudioSource>();

        // Setting the parent of primary/secondary weapon to it's appropriate weapon socket
        gun.weaponController.currentWeapon.transform.SetParent(gun.socket);
        gun.weaponController.currentWeapon.transform.localPosition = Vector3.zero;
        gun.weaponController.currentWeapon.transform.localRotation = Quaternion.Euler(gun.weaponRotation);

        // Unparent the armsRig from weaponRecoil and set the swayPoint to the appropriate swayPoint to the gun and parent it back to weaponRecoil
        // gun.armsRig.SetParent(gun.weaponHolder);
        // gun.weaponSway.localPosition = gun.swayPoint.localPosition;
        // gun.armsRig.SetParent(gun.weaponRecoil);

        Draw();
    }

    public void Reset()
    {
        // Setting every position back to normal
        gun.armsRig.localPosition = new Vector3(0, 0, -0.12f);
        gun.weaponSway.localPosition = Vector3.zero;

        // Resetting the parent of primary/secondary weapon to weaponHolder
        gun.weaponController.currentWeapon.transform.SetParent(gun.weaponHolder);
        gun.weaponController.currentWeapon.transform.localPosition = Vector3.zero;
        gun.weaponController.currentWeapon.transform.localRotation = Quaternion.Euler(Vector3.zero);
    }

    #endregion

    #region "Display"

    public void DisplayWeaponStats(TMP_Text weaponNameTMP, TMP_Text ammoCountTMP, TMP_Text ammoReserveCountTMP)
    {
        weaponNameTMP.text = gameObject.name;
        ammoCountTMP.text = gun.ammoCount.ToString();
        ammoReserveCountTMP.text = gun.ammoReserveCount.ToString();
    }

    public void DisplayCrossHair(RectTransform crossHair)
    {
        CanvasGroup crossHairAlpha = crossHair.gameObject.GetComponent<CanvasGroup>();

        // Reducing the timer after a shot is fired
        if(gun.fireCrossHairTimer > 0)
        {
            gun.fireCrossHairTimer -= Time.deltaTime;
        }
        else
        {
            gun.fireCrossHairTimer = 0;
        }

        var stanceCrossHairSize = 0f;

        if(gun.characterController.isStand)
        {
            stanceCrossHairSize = gun.standCrossHairSize;
        }
        else if(gun.characterController.isCrouch)
        {
            stanceCrossHairSize = gun.crouchCrossHairSize;
        }
        else if(gun.characterController.isProne)
        {
            stanceCrossHairSize = gun.proneCrossHairSize;
        }

        gun.crossHairSize = Mathf.SmoothDamp(gun.crossHairSize, stanceCrossHairSize, ref gun.crossHairStanceSizeVelocity, gun.characterController.playerStanceSmoothing);        
        gun.walkCrossHairLerp = Mathf.Lerp(0, gun.walkCrossHairSize, gun.characterController.smoothedWalkingAnimationSpeed);
        gun.fireCrossHairLerp = Mathf.Lerp(0, gun.fireCrossHairSize, gun.fireCrossHairTimer / gun.crossHairResetDuration);

        gun.addedCrossHairSize = gun.crossHairSize + gun.fireCrossHairLerp + gun.walkCrossHairLerp;

        if(gun.isADSIn || gun.isADSOut || gun.isADS)
        {
            gun.currentCrossHairSize = Mathf.Lerp(gun.addedCrossHairSize, 0, gun.adsTimer / (gun.adsDuration / gun.adsSpeed));
            gun.currentAlpha = Mathf.Lerp(1, 0, (gun.adsTimer / (gun.adsDuration / gun.adsSpeed)) / 0.8f);
        }
        else
        {
            gun.currentCrossHairSize = gun.addedCrossHairSize;            
            gun.currentAlpha = 1;
        }

        gun.currentCrossHairSize = gun.currentCrossHairSize * (90f / gun.characterController.playerSettings.FieldOfView);

        crossHair.sizeDelta = new Vector2(gun.currentCrossHairSize, gun.currentCrossHairSize);
        crossHairAlpha.alpha = gun.currentAlpha;
        crossHair.gameObject.SetActive(!gun.isADS);
    }

    #endregion

    #region "Weapon Stance"

    private void CalculateWeaponStance()
    {
        Vector3 stanceWeaponPosition = Vector3.zero;
        Vector3 stanceWeaponRotation = Vector3.zero;

        if(gun.characterController.isStand)
        {
            stanceWeaponPosition = Vector3.zero;
            stanceWeaponRotation = Vector3.zero;
        }
        else if(gun.characterController.isCrouch)
        {
            stanceWeaponPosition = gun.crouchWeaponPosition;
            stanceWeaponRotation = gun.crouchWeaponRotation;
        }
        else if(gun.characterController.isProne)
        {
            stanceWeaponPosition = gun.proneWeaponPosition;
            stanceWeaponRotation = gun.proneWeaponRotation;
        }

        // Saving the position and rotation of WeaponStance solely based on values without taking the change with ADS into account
        gun.weaponStanceReferencePosition = Vector3.SmoothDamp(gun.weaponStanceReferencePosition, stanceWeaponPosition, ref gun.weaponStanceReferencePositionVelocity, gun.characterController.playerStanceSmoothing); 
        gun.weaponStanceReferenceRotation = QuaternionSmoothDamp(gun.weaponStanceReferenceRotation, Quaternion.Euler(stanceWeaponRotation), ref gun.weaponStanceReferenceRotationVelocity, gun.characterController.playerStanceSmoothing);

        if(!gun.isADS)
        {
            gun.weaponStance.localPosition = Vector3.SmoothDamp(gun.weaponStance.localPosition, stanceWeaponPosition, ref gun.weaponStancePositionVelocity, gun.characterController.playerStanceSmoothing); 
            // Smoothly interpolate the rotation towards the target rotation
            gun.weaponStance.localRotation = QuaternionSmoothDamp(gun.weaponStance.localRotation, Quaternion.Euler(stanceWeaponRotation), ref gun.weaponStanceRotationVelocity, gun.characterController.playerStanceSmoothing);
        }
        else
        {
            if(gun.weaponStance.localPosition != stanceWeaponPosition || gun.weaponStance.localRotation != Quaternion.Euler(stanceWeaponRotation))
            {
                gun.weaponStance.localPosition = Vector3.SmoothDamp(gun.weaponStance.localPosition, stanceWeaponPosition, ref gun.weaponStancePositionVelocity, gun.characterController.playerStanceSmoothing); 
                // Smoothly interpolate the rotation towards the target rotation
                gun.weaponStance.localRotation = QuaternionSmoothDamp(gun.weaponStance.localRotation, Quaternion.Euler(stanceWeaponRotation), ref gun.weaponStanceRotationVelocity, gun.characterController.playerStanceSmoothing);
            }
        }
        
        // When ADS In and Out, reset the positon and rotation to wherever the WeaponStance should be
        gun.weaponStance.localPosition = Vector3.Lerp(gun.weaponStanceReferencePosition, Vector3.zero, gun.adsTimer / (gun.adsDuration / gun.adsSpeed));
        gun.weaponStance.localRotation = Quaternion.Lerp(gun.weaponStanceReferenceRotation, Quaternion.Euler(Vector3.zero), gun.adsTimer / (gun.adsDuration / gun.adsSpeed));
    }

    // Custom SmoothDamp function for Quaternions
    Quaternion QuaternionSmoothDamp(Quaternion current, Quaternion target, ref Quaternion velocity, float smoothTime)
    {
        // Smooth damp for each component
        Vector4 currentVec = new Vector4(current.x, current.y, current.z, current.w);
        Vector4 targetVec = new Vector4(target.x, target.y, target.z, target.w);
        Vector4 result = new Vector4(
            Mathf.SmoothDamp(currentVec.x, targetVec.x, ref velocity.x, smoothTime),
            Mathf.SmoothDamp(currentVec.y, targetVec.y, ref velocity.y, smoothTime),
            Mathf.SmoothDamp(currentVec.z, targetVec.z, ref velocity.z, smoothTime),
            Mathf.SmoothDamp(currentVec.w, targetVec.w, ref velocity.w, smoothTime)
        );

        // Normalize the result to get a valid quaternion
        return new Quaternion(result.x, result.y, result.z, result.w).normalized;
    }

    #endregion
}
