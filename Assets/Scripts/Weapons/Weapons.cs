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
        public Camera camera;
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
        public int damage;
        public int ammoCount;
        public int magCapacity;
        public int ammoReserveCount;
        public bool isReloadWhileSprint;
        public float reloadDuration;
        public float magInDuration;
        public float reloadTimer;
        public bool isReloading;
        public float drawDuration;
        public float drawTimer;
        public bool isDrawing;
        public float putAwayDuration;
        public float putAwayTimer;
        public bool isPuttingAway;
        public float fireRate;
        public int fireMode;
        public Vector2 bulletSpread;
        public float crossHairSize;
        public float fireCrossHairSize;
        public float walkCrossHairSize;
        public float crossHairResetDuration;
        [HideInInspector]
        public float currentCrossHairSize;
        [HideInInspector]
        public float fireCrossHairTimer;
        [HideInInspector]
        public float fireCrossHairLerp;
        [HideInInspector]
        public float walkCrossHairLerp;
        [HideInInspector]
        public float addedCrossHairSize;
        [HideInInspector]
        public float currentAlpha;

        [Header("ADS Settings")]
        public Vector3 adsPosition;
        public float adsDuration;
        public float adsTimer;
        public float adsSpeed;
        public float adsZoom;
        public float adsZoomStartTime;
        public float adsZoomTimer;
        public bool isADS;
        public bool isADSIn;
        public bool isADSOut;
        public bool isTryingToADSWhileDoingSomethingElse;

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

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip fireClip;

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
