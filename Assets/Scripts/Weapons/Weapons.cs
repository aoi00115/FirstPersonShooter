using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Weapons
{
    [Serializable]
    public class Gun
    {
        [Header("References")]
        public WeaponController weaponController;
        public CharacterControllerScr characterController;
        public Transform armsRig;
        public Transform cameraRecoil;
        public Transform weaponHolder;
        public Transform weaponSway;
        public Transform weaponRecoil;
        public Transform weaponADSRecoil;
        public Animator gunAnimator;
        public Animator armsAnimator;
        public Animator cameraAnimator;
        public RuntimeAnimatorController gunAnimatorController;
        public RuntimeAnimatorController armsAnimatorController;
        public RuntimeAnimatorController cameraAnimatorController;
        public Transform socket;
        public Transform swayPoint;
        public Vector3 weaponRotation;

        [Header("Gun Stats Settings")]
        public int ammoCount;
        public int magCapacity;
        public int ammoReserveCount;
        public Vector3 adsPosition;
        public float reloadDuration;
        public float reloadTimer;
        public bool isReloading;
        public float drawDuration;
        public float drawTimer;
        public bool isDraw;
        public float putAwayDuration;
        public float putAwayTimer;
        public bool isPutAway;
        public bool isPuttingAway;
        public float fireRate;
        public int fireMode;
        public Vector2 bulletSpread;
        public int damage;

        [Header("ADS Settings")]
        public float adsDuration;
        public float adsSpeed;
        public float adsZoom;
        public float adsTimer;
        public bool isADS;
        public bool isADSIn;
        public bool isADSOut;

        public bool isFireModeSwitchable;
        public bool isAimable;
        public bool isPrimary;
        public bool isSecondary;
        public bool inUse;

        [Header("Recoil Settings")]
        public Vector3 recoil;
        public Vector3 ADSRecoil;
        public float snappiness;
        public float returnSpeed;
    }

    [Serializable]
    public class Melee
    {
        public float meleeDuration;
        public float meleeLength;
        public int damage;
    }

    [Serializable]
    public class Equipment
    {
        public float prepDuration;
        public float throwDistance;
        public int hitDamage;
        public bool isFused;
        public float fuseTime;
    }
}
