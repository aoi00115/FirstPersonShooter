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
    }

    public void Fire()
    {
        // if(gun.isADS)
        // {
        //     gun.armsAnimator.Play(gameObject.name + "_Arms_ADS_Fire_Animation");
        //     gun.gunAnimator.Play(gameObject.name + "_Gun_ADS_Fire_Animation");
        // }
        // else
        // {
        //     gun.armsAnimator.Play(gameObject.name + "_Arms_Fire_Animation");
        //     gun.gunAnimator.Play(gameObject.name + "_Gun_Fire_Animation");
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
            gun.armsAnimator.Play(gameObject.name + "_ADS_In_Animation", -1, 0);
        }
        else
        {
            gun.armsAnimator.Play(gameObject.name + "_ADS_In_Animation");
        }
        
    }

    public void ADSOut()
    {
        gun.isADSOut = true;
        gun.isADSIn = false;
        gun.armsAnimator.SetFloat("ADSSpeed", -gun.adsSpeed);
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
        }

        // Setting the float used in the blend tree in animator, it calculates the normalized ADS time from 0 to 1 where 0 being hip fire, and 1 being ADS
        gun.armsAnimator.SetFloat("ADSProgress", gun.adsTimer / (gun.adsDuration / gun.adsSpeed));
    }

    public bool CalculateADS()
    {
        return gun.isADS;
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
        
    }

    public void PutAway()
    {
        
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
