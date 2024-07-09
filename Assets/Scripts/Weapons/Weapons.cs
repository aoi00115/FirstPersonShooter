using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Weapons
{
    [Serializable]
    public class Gun
    {
        [Header("References - Transform")]
        public WeaponController weaponController;
        public CharacterControllerScr characterController;
        public Transform armsRig;
        public Camera camera;
        public Transform cameraRecoil;
        public Transform cameraRecoilImpact;
        public Transform weaponHolder;
        public Transform weaponADSAdjustmentLayer;
        public Transform weaponADS;
        public Transform weaponSway;
        public Transform weaponRecoilAdjustmentLayer;
        public Transform weaponRecoil;
        public Transform weaponStanceAdjustmentLayer;
        public Transform weaponStance;
        public Transform socket;
        public Transform swayPoint;
        public Vector3 weaponRotation;

        [Header("References - Animator")]
        public Animator gunAnimator; 
        public Animator armsAnimator;
        public Animator cameraAnimator;
        public Animator adsAnimator;

        [Header("References - Animator Controller")]
        public RuntimeAnimatorController gunAnimatorController;
        public RuntimeAnimatorController armsAnimatorController;
        public RuntimeAnimatorController cameraAnimatorController;
        public RuntimeAnimatorController adsAnimatorController;

        [Header("Gun Stats Settings")]
        public int damage;
        public int ammoCount;
        public int magCapacity;
        public int ammoReserveCount;
        public bool isFireableWhileSprint;
        public bool isFireModeSwitchable;
        public bool canBurstFire;
        public bool isHoldBurst;
        public bool isBurstSubscribable;
        public int numberOfBurst;
        public float fireRate;
        public FireMode fireMode;
        public bool isManualRechambering;
        public bool isPrimary;
        public bool isSecondary;
        public bool inUse;

        [Header("Gun Stats Debug")]
        public bool isEmpty;
        public float lastFiredTime;
        public bool isReadyToFire;
        public int burstCounter;
        public bool isBurstFiring;
        public bool isSubscribingBurstFire;
        public bool isFullFiring;
        public bool isFiring;

        [Header("Reload Settings")]
        public bool isReloadableWhileSprint;
        public bool isReloadableWhileADS;
        public float reloadSpeed;
        public float reloadDuration;
        public float emptyReloadDuration;
        public float magInDuration;
        public float rechamberDuration;

        [Header("Reload Debug")]
        public float reloadTimer;
        public bool isReloading;

        [Header("ADS Settings")]
        public Vector3 adsPosition;
        public float adsDuration;
        public float adsSpeed;
        public float adsZoom;
        public float adsZoomStartTime;
        [Range(0f, 10f)]
        public float ADSMovementSlownessSpeedEffector;
        public bool isAimable;

        [Header("ADS Debug")]
        public float adsTimer;
        public float adsZoomTimer;
        public bool isADS;
        public bool isADSIn;
        public bool isADSOut;
        public bool isTryingToADSWhileDoingSomethingElse;

        [Header("Recoil Settings")]
        public bool enableRecoil;
        public bool enableRecoilImpact;
        public Vector3 recoil;
        public Vector3 adsRecoil;
        public Vector3 recoilImpact;
        public Vector3 adsRecoilImpact;
        public float recoilSnappiness;
        public float recoilReturnSpeed;
        public AnimationCurve recoilImpactSpringDampingCurve;
        public float recoilImpactSpringDampingSpeed;

        [Header("Recoil Debug")]
        public Vector3 lastFiredHeadPositionRotation;
        public Vector3 headPositionRotationEulerAngle;
        [HideInInspector]
        public Vector3 recoilImpactReference;
        [HideInInspector]
        public float recoilImpactTime;
        public Vector3 cameraRecoilReferenceRotation;
        public Vector3 currentCameraRecoilRotation;
        public Vector3 targetCameraRecoilRotation;
        [HideInInspector]
        public Vector3 currentCameraRecoilImpactRotation;
        [HideInInspector]
        public Vector3 targetCameraRecoilImpactRotation;
        public float onSetUpCameraRecoilXRotation;
        public float onWeaponSetUpViewClampYMax;
        public float onWeaponSetUpViewClampYMin;
        
        [Header("PutAway/Draw Settings")]
        public float drawDuration;
        public float drawSpeed;
        public float putAwayDuration;
        public float putAwaySpeed;

        [Header("PutAway/Draw Debug")]
        public float drawTimer;
        public bool isDrawing;
        public float putAwayTimer;
        public bool isPuttingAway;

        [Header("Cross Hair Settings")]
        public CrossHairType crossHairType;
        public float standCrossHairSize;
        public float crouchCrossHairSize;
        public float proneCrossHairSize;
        public float walkAdditiveCrossHairSize;
        public float fireAdditiveCrossHairSize;
        [HideInInspector]
        public float crossHairSize;
        public float crossHairResetDuration;

        [Header("Cross Hair Debug")]
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
        public float crossHairStanceSizeVelocity;
        [HideInInspector]
        public float currentAlpha;

        [Header("Weapon Stance View Settings")]
        public Vector3 crouchWeaponPosition;
        public Vector3 crouchWeaponRotation;
        public Vector3 proneWeaponPosition;
        public Vector3 proneWeaponRotation;
        [HideInInspector]
        public Vector3 weaponStancePositionVelocity;
        [HideInInspector]
        public Quaternion weaponStanceRotationVelocity = Quaternion.identity;
        [HideInInspector]
        public Vector3 weaponStanceReferencePosition;
        [HideInInspector]
        public Vector3 weaponStanceReferencePositionVelocity;
        [HideInInspector]
        public Quaternion weaponStanceReferenceRotation;
        [HideInInspector]
        public Quaternion weaponStanceReferenceRotationVelocity = Quaternion.identity;

        [Header("Effect Settings")]
        public Transform ejectionPoint;
        public GameObject bulletCasing;
        public GameObject bulletDecal;

        [Header("Audio Settings")]
        public AudioSource audioSource;
        public AudioSource reloadAudioSource;
        public AudioClip fireAudioClip;
        public AudioSequence[] reloadAudioSequences;
        public AudioSequence[] emptyReloadAudioSequences;
    }

    public enum FireMode
    {
        Semi,
        Burst,
        Full
    }

    public enum CrossHairType
    {
        CrossHair,
        Dot
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

    [Serializable]
    public class AudioSequence
    {
        public string action;
        public float time;
        public AudioClip audioClip;
        public bool hasPlayed;
    }
}
