using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IFireable
{
    void Fire();
    void ADSIn();
    void ADSOut();
    bool CalculateADS();
    void Reload();
    void SwitchFireMode();
    void Draw();
    void PutAway();
    bool CalculatePutAway();
    void Walk(float walkingAnimationSpeed, bool isIdle);
    void SetUp();
    void Reset();
}
