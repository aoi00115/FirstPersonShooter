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
        public CharacterControllerScr characterController;
        public Transform armsnWeapons;
        public Transform cameraHolder;

        [Header("Gun Settings")]
        public int ammoCount;
        public int magCapacity;
        public int ammoReserveCount;
        public float adsDuration;
        public float reloadDuration;
        public float drawDuration;
        public float putAwayDuration;
        public float fireRate;
        public int fireMode;
        public Vector2 bulletSpread;
        public int damage;
        public bool isFireModeSwitchable;
        public Vector3 defaultGunPosition;
        public Vector3 gunPosition;
        public Vector3 adsPosition;
        public bool isAimable;
        public bool isPrimary;
        public bool isSecondary;

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
