using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IFireable
{
    void Fire();
    void ADSIn();
    void ADSOut();
    void CalculateADSTime();
    bool CalculateADS();
    void Reload();
    void SwitchFireMode();
    void Draw();
    void PutAway();
    void Walk(float walkingAnimationSpeed, bool isIdle);
    void SetUp();
    void Reset();
}
