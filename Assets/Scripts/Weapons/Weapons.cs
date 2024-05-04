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
        public Transform cameraRecoil;
        public Animator gunAnimator;
        public Animator armsAnimator;
        public Animator cameraAnimator;
        public Transform socket;

        [Header("Gun Settings")]
        public int ammoCount;
        public int magCapacity;
        public int ammoReserveCount;
        public float adsDuration;
        public float adsZoom;
        public float reloadDuration;
        public float drawDuration;
        public float putAwayDuration;
        public float fireRate;
        public int fireMode;
        public Vector2 bulletSpread;
        public int damage;
        public bool isFireModeSwitchable;
        public Vector3 adsPosition;
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
