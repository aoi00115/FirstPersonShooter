using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Weapons;

public class Pistol : MonoBehaviour, IFireable
{
    [Header("Pistol Settings")]
    public Gun gun;
    public bool isADS;
    
    // Start is called before the first frame update
    void Start()
    {
        // SetCurrentWeapon();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Fire()
    {
        gun.armsAnimator.Play(gameObject.name + "_Arms_Fire_Animation");
        gun.gunAnimator.Play(gameObject.name + "_Gun_Fire_Animation");
    }

    public void ADSIn()
    {
        isADS = true;
        gun.armsAnimator.SetBool("isADS", isADS);
        // gun.armsAnimator.SetTrigger("ADSTrigger");
        gun.armsAnimator.SetFloat("ADSSpeed", 1.0f);
        gun.armsAnimator.Play(gameObject.name + "_ADS_In_Animation");
    }

    public void ADSOut()
    {
        isADS = false;
        gun.armsAnimator.SetBool("isADS", isADS);
        // gun.armsAnimator.SetTrigger("ADSOutTrigger");
        gun.armsAnimator.SetFloat("ADSSpeed", -1.0f);
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
