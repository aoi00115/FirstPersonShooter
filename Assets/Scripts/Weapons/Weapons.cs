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
        public Transform weaponHolder;
        public Transform weaponADSAdjustmentLayer;
        public Transform weaponADS;
        public Transform weaponSway;
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
        public bool isReloadWhileSprint;
        public bool isReloadWhileADS;
        public float reloadSpeed;
        public float reloadDuration;
        public float emptyReloadDuration;
        public float magInDuration;
        public float rechamberDuration;
        public float reloadTimer;
        public bool isReloading;
        public bool isEmpty;
        public float drawDuration;
        public float drawTimer;
        public float drawSpeed;
        public bool isDrawing;
        public float putAwayDuration;
        public float putAwayTimer;
        public float putAwaySpeed;
        public bool isPuttingAway;
        public bool isReadyToFire;
        public float fireRate;
        public int fireMode;
        public Vector2 bulletSpread;

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

        [Header("Cross Hair Settings")]
        public float crossHairSize;
        public float standCrossHairSize;
        public float crouchCrossHairSize;
        public float proneCrossHairSize;
        public float walkCrossHairSize;
        public float fireCrossHairSize;
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
        public float crossHairStanceSizeVelocity;
        [HideInInspector]
        public float currentAlpha;

        [Header("Recoil Settings")]
        public Vector3 recoil;
        public Vector3 ADSRecoil;
        public float snappiness;
        public float returnSpeed;

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

        [Header("Audio Settings")]
        public AudioSource audioSource;
        public AudioSource reloadAudioSource;
        public AudioClip fireAudioClip;
        public AudioSequence[] reloadAudioSequences;
        public AudioSequence[] emptyReloadAudioSequences;
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
