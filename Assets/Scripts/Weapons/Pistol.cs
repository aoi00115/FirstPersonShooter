using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Weapons;

public class Pistol : MonoBehaviour, IFireable
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
        CalculateDrawTime();
        CalculatePutAwayTime();
    }

    public void Fire()
    {
        // Fire only after drawing or only not putting away
        if(gun.isDraw)
        {            
            return;
        }        
        if(gun.isPuttingAway)
        {
            return;
        }
        // if(gun.isReloading)
        // {
        //     return;
        // }

        // If fired when running, stop running
        if(gun.characterController.isSprinting == true)
        {
            gun.characterController.isSprinting = false;
        }

        gun.armsAnimator.Play("Arms Fire Blend Tree");
        gun.gunAnimator.Play(gameObject.name + "_Gun_Fire_Animation");
    }

    public void ADSIn()
    {
        // If ADS when running, stop running
        if(gun.characterController.isSprinting == true)
        {
            gun.characterController.isSprinting = false;
        }
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
        gun.isADSOut = true;
        gun.isADSIn = false;
        gun.armsAnimator.SetFloat("ADSSpeed", -gun.adsSpeed);
    }

    public bool CalculateADS()
    {
        return gun.isADS;
    }

    public void CalculateADSTime()
    {
        if(gun.isADSIn)
        {
            // Increase the timer when aiming in, until it reaches up to ADSduration, and if it reaches, freeze the timer by setting adsTimer to ADSduration
            if(gun.adsDuration / gun.adsSpeed >= gun.adsTimer)
            {
                gun.adsTimer += Time.deltaTime;                
            }
            else
            {
                gun.adsTimer = gun.adsDuration / gun.adsSpeed;
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
        }

        // When the ADS animation reaches the ADSduration time frame, stop the animation from playing by setting the speed to 0
        if(gun.adsDuration / gun.adsSpeed == gun.adsTimer)
        {
            gun.isADS = true;
            gun.isADSIn = false;
            gun.armsAnimator.SetFloat("ADSSpeed", 0f);

            // Parenting the arms rig to the WeaponADSRecoil to change the sway pivot point for ADS
            // if(gun.weaponADSRecoil.childCount == 0)
            // {
            //     gun.armsRig.SetParent(gun.weaponADSRecoil);
            // }
        }
        else
        {
            gun.isADS = false;

            // Parenting the arms rig back to the WeaponRecoil to change the sway pivot point for gun
            // if(gun.weaponRecoil.childCount == 0)
            // {
            //     gun.armsRig.SetParent(gun.weaponRecoil);
            // }
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
    }

    public void Reload()
    {
        gun.armsAnimator.Play(gameObject.name + "_Arms_Reload_Animation");
        gun.gunAnimator.Play(gameObject.name + "_Gun_Reload_Animation");
        gun.cameraAnimator.Play(gameObject.name + "_Camera_Reload_Animation");
    }

    public void SwitchFireMode()
    {
        
    }

    public void Draw()
    {        
        gun.armsAnimator.Play(gameObject.name + "_Draw_Animation", -1, 0);
        gun.isPuttingAway = false;
        gun.isDraw = true;
    }

    public void CalculateDrawTime()
    {
        if(gun.isDraw)
        {
            // Increase the timer when aiming in, until it reaches up to ADSduration, and if it reaches, freeze the timer by setting adsTimer to ADSduration
            if(gun.drawDuration >= gun.drawTimer)
            {
                gun.drawTimer += Time.deltaTime;
            }
            else
            {
                // If ADS when drawing the gun, it'll reset the timer to 0 and ADS from the start
                if(gun.isADS)
                {
                    ADSOut();
                    gun.adsTimer = 0;
                    ADSIn();
                }
                gun.drawTimer = 0;
                gun.isDraw = false;
                gun.armsAnimator.Play("Draw Empty");
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

        // If reloading, stop reloading when putting the weapon away. Calculate Reload time with CalculateReloadTime() function just like other Claculate-Time function
        gun.armsAnimator.Play("Reload Empty");
        gun.gunAnimator.Play(gameObject.name + "_Gun_Idle_Animation");
        gun.cameraAnimator.Play(gameObject.name + "_Camera_Idle_Animation");
    }

    public bool CalculatePutAway()
    {
        return gun.isPutAway;
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
                gun.putAwayTimer = gun.putAwayDuration;
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
            }
        }

        // When the ADS animation reaches the ADSduration time frame, stop the animation from playing by setting the speed to 0
        if(gun.putAwayDuration == gun.putAwayTimer)
        {
            gun.isPutAway = true;
            gun.armsAnimator.SetFloat("PutAwaySpeed", 0f);

            gun.weaponController.SwitchWeapons();
        }
        else
        {
            gun.isPutAway = false;
        }

        // When the ADS animation reaches zero time frame, stop the animation from playing by setting the speed to 0
        if(gun.putAwayTimer == 0)
        {
            gun.armsAnimator.SetFloat("PutAwaySpeed", 0f);            
            // When not ADS (Hip fire) play the empty state
            gun.armsAnimator.Play("PutAway Empty");
        }
    }

    public void Walk(float walkingAnimationSpeed, bool isIdle)
    {
        gun.armsAnimator.SetBool("isIdle", isIdle);
        gun.armsAnimator.SetFloat("WalkingAnimationSpeed", walkingAnimationSpeed);
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
}
