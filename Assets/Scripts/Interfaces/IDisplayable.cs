using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public interface IDisplayable
{
    void DisplayWeaponStats(TMP_Text weaponNameTMP, TMP_Text ammoCountTMP, TMP_Text ammoReserveCountTMP);
    void DisplayCrossHair(RectTransform crossHair);
}
