using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IFireable
{
    void Fire();
    void FireUp();
    void ADSIn();
    void ADSOut();
    bool CalculateADS();
    bool CalculateADSIn();
    void Reload();
    void SwitchFireMode();
    void Draw();
    void PutAway();
    void Walk(float walkingAnimationSpeed, bool isIdle);
    void Sprint(bool isSprint);
    void SetUp();
    void Reset();
}
