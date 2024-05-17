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

        Debug.Log(gun.adsDuration / gun.adsSpeed);
    }

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

        gun.armsAnimator.Play("Arms Fire Blend Tree");
        gun.gunAnimator.Play(gameObject.name + "_Gun_Fire_Animation");

        gun.audioSource.PlayOneShot(gun.fireClip, 1);

        gun.ammoCount--;
    }

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
    }

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

    public void SwitchFireMode()
    {
        
    }

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

    public void Walk(float walkingAnimationSpeed, bool isIdle)
    {
        gun.armsAnimator.SetBool("isIdle", isIdle);
        gun.armsAnimator.SetFloat("WalkingAnimationSpeed", walkingAnimationSpeed);
    }

    public void Sprint(bool isSprint)
    {
        gun.armsAnimator.SetBool("isSprint", isSprint);
    }
    
    public void SetUp()
    {
        // if(gun.inUse)
        // {
        //     gun.weaponController.currentWeapon = this.gameObject;
        // }

        // Setting up all the scripts and reference
        gun.characterController = transform.Find("../../../../").GetComponent<CharacterControllerScr>();
        gun.weaponController = transform.Find("../../").GetComponent<WeaponController>();

        gun.armsRig = transform.Find("../../WeaponSway/WeaponRecoil/ArmsRig");
        gun.cameraRecoil = transform.Find("../../../CameraHolder/CameraRecoil");
        gun.camera = transform.Find("../../../CameraHolder/CameraRecoil/Camera").GetComponent<Camera>();
        gun.weaponHolder = transform.Find("../../");
        gun.weaponSway = transform.Find("../../WeaponSway");
        gun.weaponRecoil = transform.Find("../../WeaponSway/WeaponRecoil");
        gun.weaponADSRecoil = transform.Find("../../WeaponADSSway/WeaponADSRecoil");
        gun.socket = transform.Find("../../WeaponSway/WeaponRecoil/ArmsRig/arms_rig/root/upper_arm_R/lower_arm_R/hand_R/" + gameObject.name + "Socket");
        gun.swayPoint = transform.Find("../../SwayPoints/" + gameObject.name + "SwayPoint");

        gun.gunAnimator = GetComponent<Animator>();
        gun.armsAnimator = transform.Find("../../WeaponSway/WeaponRecoil/ArmsRig").GetComponent<Animator>();
        gun.cameraAnimator = transform.Find("../../../CameraHolder").GetComponent<Animator>();

        gun.gunAnimator.runtimeAnimatorController = gun.gunAnimatorController;
        gun.armsAnimator.runtimeAnimatorController = gun.armsAnimatorController;
        gun.cameraAnimator.runtimeAnimatorController = gun.cameraAnimatorController;

        gun.audioSource = GetComponent<AudioSource>();

        // Setting the parent of primary/secondary weapon to it's appropriate weapon socket
        gun.weaponController.currentWeapon.transform.SetParent(gun.socket);
        gun.weaponController.currentWeapon.transform.localPosition = Vector3.zero;
        gun.weaponController.currentWeapon.transform.localRotation = Quaternion.Euler(gun.weaponRotation);

        // Unparent the armsRig from weaponRecoil and set the swayPoint to the appropriate swayPoint to the gun and parent it back to weaponRecoil
        gun.armsRig.SetParent(gun.weaponHolder);
        gun.weaponSway.localPosition = gun.swayPoint.localPosition;
        gun.armsRig.SetParent(gun.weaponRecoil);

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

    public void DisplayWeaponStats(TMP_Text weaponNameTMP, TMP_Text ammoCountTMP, TMP_Text ammoReserveCountTMP)
    {
        weaponNameTMP.text = gameObject.name;
        ammoCountTMP.text = gun.ammoCount.ToString();
        ammoReserveCountTMP.text = gun.ammoReserveCount.ToString();
    }
}
