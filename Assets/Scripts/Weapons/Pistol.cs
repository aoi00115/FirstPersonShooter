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
        gun.armsnWeapons = transform.Find("../../../../");
        gun.characterController = transform.Find("../../../../../../../").GetComponent<CharacterControllerScr>();
        gun.cameraHolder = transform.Find("../../../../../../");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
