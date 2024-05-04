using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Weapons;

public class Pistol : MonoBehaviour
{
    [Header("Pistol Settings")]
    public Gun gun;
    
    // Start is called before the first frame update
    void Start()
    {
        gun.characterController = transform.Find("../../../../../../../../../../../../").GetComponent<CharacterControllerScr>();
        gun.weaponController = transform.Find("../../../../../../../../../../").GetComponent<WeaponController>();
        gun.cameraRecoil = transform.Find("../../../../../../../../../../../CameraHolder/CameraRecoil");

        gun.gunAnimator = GetComponent<Animator>();
        gun.armsAnimator = transform.Find("../../../../../../").GetComponent<Animator>();
        gun.cameraAnimator = transform.Find("../../../../../../../../../../../CameraHolder").GetComponent<Animator>();
        
        SetCurrentWeapon();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void SetCurrentWeapon()
    {
        if(gun.inUse)
        {
            gun.weaponController.currentWeapon = this.gameObject;
        }
    }
}
