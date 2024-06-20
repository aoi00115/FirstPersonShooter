using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using static Weapons;

public class Pistol : MonoBehaviour, IFireable, IDisplayable
{
    [Header("Pistol Settings")]
    public Gun gun;

    float zRotationRef;
    public float lerp;

    // Start is called before the first frame update
    void Start()
    {
        gun.adsDuration += 0.01f;
    }

    // Update is called once per frame
    void Update()
    {
        CalculateADSTime();
        CalculateReloadTime();
        CalculateDrawTime();
        CalculatePutAwayTime();
        CalculateWeaponStance();

        CalculateTotalCameraRecoil();
        CalculateCameraRecoil();
        CalculateCameraRecoilImpact();

        CalculateFireCap();
        FullFire();
        BurstFire();
    }

    #region "Fire"

    public void Fire()
    {
        
        // This will ensure not to execute the following
        if(gun.isDrawing || gun.isPuttingAway || gun.isReloading)
        {            
            return;
        }

        // When mag is empty, return
        if(gun.ammoCount <= 0)
        {
            gun.ammoCount = 0;
            return;
        }

        // If fired when running, stop running
        if(gun.characterController.isSprinting == true && !gun.isFireableWhileSprint)
        {
            gun.characterController.isSprinting = false;
        }

        switch (gun.fireMode)
        {
            case FireMode.Semi:
                SemiFire();
                // Debug.Log("Semi Firing");
                break;

            case FireMode.Burst:
                // When fired while burst firing it'll subscribe burst fire for the next one
                if(gun.isBurstFiring && gun.isBurstSubscribable)
                {
                    gun.isSubscribingBurstFire = true;
                }
                gun.isBurstFiring = true;
                // Debug.Log("Burst Firing");
                break;

            case FireMode.Full:

                gun.isFullFiring = true;
                // Debug.Log("Full Firing");
                break;
        }
    }

    public void FireUp()
    {
        if(gun.fireMode == FireMode.Full)
        {
            gun.isFullFiring = false;
        }

        if(gun.isHoldBurst)
        {
            gun.isBurstFiring = false;
            gun.burstCounter = 0;
        }
    }

    public void SemiFire()
    {
        BaseFire();
    }

    public void BurstFire()
    {
        if(gun.isBurstFiring)
        {
            if(gun.burstCounter < gun.numberOfBurst)
            {
                BaseFire();
            }
            else
            {
                gun.isBurstFiring = false;
                gun.burstCounter = 0;
            }
        }

        // Set isBurstFiring to false and burstCounter to 0 after running out of ammo
        if(gun.ammoCount <= 0)
        {
            gun.isBurstFiring = false;
            gun.burstCounter = 0;
        }
    }

    public void FullFire()
    {
        if(gun.isFullFiring)
        {
            BaseFire();
        }

        // Set isFullFiring to false after running out of ammo
        if(gun.ammoCount <= 0)
        {
            gun.isFullFiring = false;
        }
    }

    public void BaseFire()
    {
        // Maybe clean up the Fire() function by utilizing this function and semi full fire functions
        // Fire only after drawing, reloading or only not putting away
        if(!gun.isReadyToFire)
        {            
            return;
        }

        // Expanding the cross-hair
        gun.fireCrossHairTimer = gun.crossHairResetDuration;

        // Play firing animation depending on the ammo left in the magazine
        if(gun.ammoCount == 1)
        {
            gun.armsAnimator.Play("Arms Last Fire Blend Tree");
            gun.gunAnimator.Play(gameObject.name + "_Gun_Last_Fire_Animation");
        }
        else
        {
            gun.armsAnimator.Play("Arms Fire Blend Tree");
            gun.gunAnimator.Play(gameObject.name + "_Gun_Fire_Animation");
        }

        // Play firing sound
        gun.audioSource.PlayOneShot(gun.fireAudioClip, 1);

        // Using raycast to actually shooting a bullet
        // Calculate random spread within the crosshair
        float spreadX = Random.Range(-gun.currentCrossHairSize, gun.currentCrossHairSize);
        float spreadY = Random.Range(-gun.currentCrossHairSize, gun.currentCrossHairSize);
        // Get the center position of the screen
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        // Apply the random spread to the screen center position
        Vector2 spreadPosition = new Vector2(screenCenter.x + spreadX, screenCenter.y + spreadY);
        // Convert the spread position from screen space to a ray
        Ray ray = gun.camera.ScreenPointToRay(spreadPosition);
        // Perform the raycast
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            Instantiate(gun.bulletDecal, hit.point, Quaternion.LookRotation(hit.normal));
        }

        // Camera Recoil
        if(gun.enableRecoil) CameraRecoil();
        if(gun.enableRecoilImpact) CameraRecoilImpact();

        // Ejecting bullet casing
        GameObject instantiatedBulletCasing;
        instantiatedBulletCasing = Instantiate(gun.bulletCasing, gun.ejectionPoint.position, gun.ejectionPoint.rotation);
        Rigidbody bulletCasingRb = instantiatedBulletCasing.GetComponent<Rigidbody>();
        bulletCasingRb.AddRelativeForce(Random.Range(5.0f, 6.0f), Random.Range(2.0f, 4.0f), 0, ForceMode.Impulse);
        bulletCasingRb.AddRelativeTorque(0, Random.Range(0f, 20f), 0, ForceMode.Impulse);
        Destroy(instantiatedBulletCasing, 5);

        // Subtracting ammoCount by one
        gun.ammoCount--;

        // Set is Empty to true when the ammo count is zero after firing
        if(gun.ammoCount == 0)
        {
            gun.isEmpty = true;
            gun.gunAnimator.SetBool("isEmpty", gun.isEmpty);
        }
        
        gun.isReadyToFire = false;
        gun.lastFiredTime = Time.time;

        // If canBurstFire, add up one at the end
        if(gun.fireMode == FireMode.Burst)
        {
            gun.burstCounter++;

            // If the burst fire is subscribed before the final round is shot, keeps burst firing by resetting burst counter to 0 then un-subscribe burst fire
            if(gun.burstCounter == gun.numberOfBurst && gun.isSubscribingBurstFire && gun.isBurstSubscribable)
            {
                if(gun.ammoCount <= 0)
                {
                    gun.isSubscribingBurstFire = false;
                }
                else
                {
                    gun.burstCounter = 0;
                    gun.isSubscribingBurstFire = false;
                }
            }
        }
    }

    public void CalculateFireCap()
    {
        if(Time.time > gun.lastFiredTime + 1 / gun.fireRate)
        {
            gun.isReadyToFire = true;
        }

        // if (fireTime > 0)
        // {
        //     fireTime -= Time.deltaTime;
        // }
        // else
        // {
        //     gun.isReadyToFire = true;
        // }
    }

    #endregion

    #region "Recoil"

    public void CameraRecoil()
    {
        gun.targetCameraRecoilRotation += new Vector3(gun.isADS ? -gun.adsRecoil.x : -gun.recoil.x, Random.Range((gun.isADS ? -gun.adsRecoil.y : -gun.recoil.y), (gun.isADS ? gun.adsRecoil.y : gun.recoil.y)), Random.Range((gun.isADS ? -gun.adsRecoil.z : -gun.recoil.z), (gun.isADS ? gun.adsRecoil.z : gun.recoil.z)));
    }

    public void CameraRecoilImpact()
    {
        // Reset animation curve's time to 0
        gun.recoilImpactTime = 0;
        gun.recoilImpactReference = Vector3.zero;

        // Generate a random number 0 or 1, so that the rotation z only uses either negative or postive of set recoil z, no in between
        int randomNumber = Random.Range(0, 2);

        // No.1(The best). The recoil impact along x and y are either positive or negative, and z is random ranged
        gun.recoilImpactReference.x = gun.isADS ? ((randomNumber == 0) ? -gun.adsRecoilImpact.x : gun.adsRecoilImpact.x) : ((randomNumber == 0) ? -gun.recoilImpact.x : gun.recoilImpact.x);
        gun.recoilImpactReference.y = gun.isADS ? ((randomNumber == 0) ? -gun.adsRecoilImpact.y : gun.adsRecoilImpact.y) : ((randomNumber == 0) ? -gun.recoilImpact.y : gun.recoilImpact.y);
        gun.recoilImpactReference.z = Random.Range((gun.isADS ? -gun.adsRecoilImpact.z : -gun.recoilImpact.z), (gun.isADS ? gun.adsRecoilImpact.z : gun.recoilImpact.z));

        // No.2(Good). The recoil impact along x and y are random ranged, and z is either positive or negative
        // gun.recoilImpactReference.x = Random.Range((gun.isADS ? -gun.adsRecoilImpact.x : -gun.recoilImpact.x), (gun.isADS ? gun.adsRecoilImpact.x : gun.recoilImpact.x));
        // gun.recoilImpactReference.y = Random.Range((gun.isADS ? -gun.adsRecoilImpact.y : -gun.recoilImpact.y), (gun.isADS ? gun.adsRecoilImpact.y : gun.recoilImpact.y));
        // gun.recoilImpactReference.z = gun.isADS ? ((randomNumber == 0) ? -gun.adsRecoilImpact.z : gun.adsRecoilImpact.z) : ((randomNumber == 0) ? -gun.recoilImpact.z : gun.recoilImpact.z);

        // No.3(Inconsistent). The recoil impact along x, y and z are all random ranged
        // gun.recoilImpactReference.x = Random.Range((gun.isADS ? -gun.adsRecoilImpact.x : -gun.recoilImpact.x), (gun.isADS ? gun.adsRecoilImpact.x : gun.recoilImpact.x));
        // gun.recoilImpactReference.y = Random.Range((gun.isADS ? -gun.adsRecoilImpact.y : -gun.recoilImpact.y), (gun.isADS ? gun.adsRecoilImpact.y : gun.recoilImpact.y));
        // gun.recoilImpactReference.z = Random.Range((gun.isADS ? -gun.adsRecoilImpact.z : -gun.recoilImpact.z), (gun.isADS ? gun.adsRecoilImpact.z : gun.recoilImpact.z));

        // // No.3(Chaotic and too repetitive). The recoil impact along x, y and z are all either postive or negative
        // gun.recoilImpactReference.x = gun.isADS ? ((randomNumber == 0) ? -gun.adsRecoilImpact.x : gun.adsRecoilImpact.x) : ((randomNumber == 0) ? -gun.recoilImpact.x : gun.recoilImpact.x);
        // gun.recoilImpactReference.y = gun.isADS ? ((randomNumber == 0) ? -gun.adsRecoilImpact.y : gun.adsRecoilImpact.y) : ((randomNumber == 0) ? -gun.recoilImpact.y : gun.recoilImpact.y);
        // gun.recoilImpactReference.z = gun.isADS ? ((randomNumber == 0) ? -gun.adsRecoilImpact.z : gun.adsRecoilImpact.z) : ((randomNumber == 0) ? -gun.recoilImpact.z : gun.recoilImpact.z);
    }

    public void CalculateTotalCameraRecoil()
    {
        // Setting cameraRecoils rotation to the total of recoil and recoil impact
        gun.cameraRecoil.transform.localRotation = Quaternion.Euler(gun.currentCameraRecoilRotation + gun.currentCameraRecoilImpactRotation);
    }

    public void CalculateCameraRecoil()
    {
        // Lerping the cameraRecoils rotation to zero 
        gun.targetCameraRecoilRotation = Vector3.Lerp(gun.targetCameraRecoilRotation, Vector3.zero, gun.recoilReturnSpeed * Time.deltaTime);

        gun.currentCameraRecoilRotation = Vector3.Lerp(gun.currentCameraRecoilRotation, gun.targetCameraRecoilRotation, gun.recoilSnappiness * Time.fixedDeltaTime);
    }

    public void CalculateCameraRecoilImpact()
    {
        gun.targetCameraRecoilImpactRotation.x = gun.recoilImpactReference.x * gun.recoilImpactSpringDampingCurve.Evaluate(gun.recoilImpactTime);
        gun.targetCameraRecoilImpactRotation.y = gun.recoilImpactReference.y * gun.recoilImpactSpringDampingCurve.Evaluate(gun.recoilImpactTime);
        gun.targetCameraRecoilImpactRotation.z = gun.recoilImpactReference.z * gun.recoilImpactSpringDampingCurve.Evaluate(gun.recoilImpactTime);
        gun.recoilImpactTime += gun.recoilImpactSpringDampingSpeed * Time.deltaTime;

        gun.currentCameraRecoilImpactRotation = Vector3.Lerp(gun.currentCameraRecoilImpactRotation, gun.targetCameraRecoilImpactRotation, gun.recoilSnappiness * Time.fixedDeltaTime);
    }

    #endregion

    #region "ADS"

    public void ADSIn()
    {
        // If ADS when reloading, set isADSIn true and do nothing
        if(gun.isDrawing)
        {
            gun.isTryingToADSWhileDoingSomethingElse = true;
            return;
        }

        if(!gun.isReloadableWhileADS && gun.isReloading)
        {
            gun.isTryingToADSWhileDoingSomethingElse = true;
            return;
        }

        // If ADS when running, stop running
        if(gun.characterController.isSprinting == true)
        {
            gun.characterController.isSprinting = false;
        }

        gun.isADS = false;
        gun.isADSIn = true;
        gun.isADSOut = false;
        gun.adsAnimator.SetFloat("ADSSpeed", gun.adsSpeed);

        // When the animation is not reset when the timer is 0, play it back from 0 time frame, and when the timer is not 0, play the animation from where the animation is continued
        if(gun.adsTimer == 0)
        {
            gun.adsAnimator.Play(gameObject.name + "_ADS_Animation", -1, 0);
        }
        else
        {
            gun.adsAnimator.Play(gameObject.name + "_ADS_Animation");
        }
    }

    public void ADSOut()
    {
        // If ADSOut when reloading, set isADSIn false and do nothing
        if(gun.isDrawing)
        {
            gun.isTryingToADSWhileDoingSomethingElse = false;
            return;
        }

        if(!gun.isReloadableWhileADS && gun.isReloading)
        {
            gun.isTryingToADSWhileDoingSomethingElse = false;
            return;
        }

        gun.isTryingToADSWhileDoingSomethingElse = false;

        gun.isADS = false;
        gun.isADSOut = true;
        gun.isADSIn = false;
        gun.adsAnimator.SetFloat("ADSSpeed", -gun.adsSpeed);

        if(gun.adsTimer == gun.adsDuration)
        {
            gun.adsAnimator.Play(gameObject.name + "_ADS_Animation", -1, 1);
        }
    }

    public bool CalculateADS()
    {
        return gun.isADS;
    }

    public bool CalculateADSIn()
    {
        return gun.isADSIn;
    }

    public float CalculateADSMovementSlownessSpeedEffector()
    {
        return Mathf.Lerp(1, gun.ADSMovementSlownessSpeedEffector / 10, gun.adsTimer / (gun.adsDuration / gun.adsSpeed));
    }

    public void CalculateADSTime()
    {
        if(gun.isADSIn)
        {
            // Increase the timer when aiming in, until it reaches up to ADSduration, and if it reaches, freeze the timer by setting adsTimer to ADSduration
            if(gun.adsDuration / gun.adsSpeed >= gun.adsTimer)
            {
                gun.adsTimer += Time.deltaTime;

                // Start counting up the ADS zoom timer when ads timer go past the ads zoom start time
                if(gun.adsZoomStartTime / gun.adsSpeed < gun.adsTimer)
                {
                    gun.adsZoomTimer += Time.deltaTime;
                }
            }
            else
            {
                gun.adsTimer = gun.adsDuration / gun.adsSpeed;
                gun.adsZoomTimer = (gun.adsDuration / gun.adsSpeed) - (gun.adsZoomStartTime / gun.adsSpeed);
                
                gun.isTryingToADSWhileDoingSomethingElse = false;

                gun.isADS = true;
                gun.isADSIn = false;
                gun.adsAnimator.SetFloat("ADSSpeed", 0f);
            }
        }
        if(gun.isADSOut)
        {
            // Decrease the timer when aiming out, until it reaches to 0, if it reaches, set the timer to 0
            if(gun.adsTimer > 0)
            {
                gun.adsTimer -= Time.deltaTime;
            }
            else
            {
                gun.adsTimer = 0;
            }

            // Do the above but for ads zoom timer
            if(gun.adsZoomTimer > 0)
            {
                gun.adsZoomTimer -= Time.deltaTime;
            }
            else
            {
                gun.adsZoomTimer = 0;
            }
        }

        // When the ADS animation reaches zero time frame, stop the animation from playing by setting the speed to 0
        if(gun.adsTimer == 0)
        {
            gun.isADSOut = false;
            gun.adsAnimator.SetFloat("ADSSpeed", 0f);            
            // When not ADS (Hip fire) play the empty state
            gun.adsAnimator.Play("ADS Empty");
        }

        // Setting the float used in the blend tree in animator, it calculates the normalized ADS time from 0 to 1 where 0 being hip fire, and 1 being ADS
        gun.armsAnimator.SetFloat("ADSProgress", gun.adsTimer / (gun.adsDuration / gun.adsSpeed));

        // Change the fov according to the ads zoom progress
        gun.camera.fieldOfView = Mathf.Lerp(gun.characterController.playerSettings.FieldOfView, gun.characterController.playerSettings.FieldOfView - gun.adsZoom, (gun.adsZoomTimer / ((gun.adsDuration / gun.adsSpeed) - (gun.adsZoomStartTime / gun.adsSpeed))));

        // Change the ads position if there is any change in ads position
        if(gun.adsPosition != Vector3.zero)
        {
            gun.armsRig.localPosition = Vector3.Lerp(new Vector3(0, 0, -0.12f), new Vector3(gun.adsPosition.x, gun.adsPosition.y, gun.adsPosition.z - 0.12f), gun.adsTimer / (gun.adsDuration / gun.adsSpeed));
        }
    }

    #endregion

    #region "Reload"

    public void Reload()
    {
        // Return when ammo is full, or reesrved ammo is 0
        if(gun.ammoCount == gun.magCapacity || gun.ammoReserveCount == 0 || gun.isPuttingAway)
        {
            return;
        }

        // If reload when sprinting and isReloadWhileSprint is fault, stop running
        if(gun.characterController.isSprinting == true && !gun.isReloadableWhileSprint)
        {
            gun.characterController.isSprinting = false;
        }

        // When "reload while ADS" is off, ADS out when reload
        if(!gun.isReloadableWhileADS)
        {
            // If reload when ADS, ADSout
            if(gun.isADS || gun.isADSIn)
            {
                ADSOut();
                gun.isTryingToADSWhileDoingSomethingElse = true;
            }
        }

        // Cancel burst firing when reloading
        gun.isSubscribingBurstFire = false;
        gun.isBurstFiring = false;
        gun.burstCounter = 0;

        if(!gun.isReloading)
        {
            gun.isReloading = true;
        }
        
        gun.armsAnimator.SetFloat("ReloadSpeed", gun.reloadSpeed);
        gun.gunAnimator.SetFloat("ReloadSpeed", gun.reloadSpeed);
        gun.cameraAnimator.SetFloat("ReloadSpeed", gun.reloadSpeed);

        // When the mag is empty
        if(gun.isEmpty)
        {
            gun.armsAnimator.Play(gameObject.name + "_Arms_Empty_Reload_Animation");
            gun.gunAnimator.Play(gameObject.name + "_Gun_Empty_Reload_Animation");
            gun.cameraAnimator.Play(gameObject.name + "_Camera_Reload_Animation");
        }
        else
        {
            gun.armsAnimator.Play(gameObject.name + "_Arms_Reload_Animation");
            gun.gunAnimator.Play(gameObject.name + "_Gun_Reload_Animation");
            gun.cameraAnimator.Play(gameObject.name + "_Camera_Reload_Animation");
        }
    }

    public void CalculateReloadTime()
    {
        if(gun.isReloading)
        {
            // Increase the timer when aiming in, until it reaches up to ADSduration, and if it reaches, freeze the timer by setting adsTimer to ADSduration
            // When the mag is empty, use the emptyReloadDuration
            if((gun.isEmpty ? gun.emptyReloadDuration : gun.reloadDuration) / gun.reloadSpeed > gun.reloadTimer)
            {
                gun.reloadTimer += Time.deltaTime;

                // Play the reload sound at the certain time depending on the bullet left
                if(gun.isEmpty)
                {
                    foreach(AudioSequence emptyReloadAS in gun.emptyReloadAudioSequences)
                    {
                        if(emptyReloadAS.time / gun.reloadSpeed <= gun.reloadTimer)
                        {
                            if(!emptyReloadAS.hasPlayed)
                            {
                                gun.reloadAudioSource.PlayOneShot(emptyReloadAS.audioClip, 1);
                                emptyReloadAS.hasPlayed = true;
                            }
                        }
                    }
                }
                else
                {
                    foreach(AudioSequence reloadAS in gun.reloadAudioSequences)
                    {
                        if(reloadAS.time / gun.reloadSpeed <= gun.reloadTimer)
                        {
                            if(!reloadAS.hasPlayed)
                            {
                                gun.reloadAudioSource.PlayOneShot(reloadAS.audioClip, 1);
                                reloadAS.hasPlayed = true;
                            }
                        }
                    }
                }
            }
            else
            {
                // If the timer reaches the reload duration
                // When the mag is empty, set it to emptyReloadDuration
                gun.reloadTimer = (gun.isEmpty ? gun.emptyReloadDuration : gun.reloadDuration) / gun.reloadSpeed;
                gun.isReloading = false;

                // If ADS when reloading the gun, reset everything and reADS
                if(gun.isTryingToADSWhileDoingSomethingElse)
                {
                    ADSIn();
                }
            }
        }
        else
        {
            // When reload is completed/cancelled
            gun.reloadTimer = 0;
            gun.armsAnimator.Play("Reload Empty");
            gun.gunAnimator.Play("Reload Empty");
            gun.cameraAnimator.Play(gameObject.name + "_Camera_Idle_Animation");

            // Set isEmpty to false after the reload is done or cancelled
            // The if below ensures that when reload cancelling after rechambering, it sets gun.isEmpty to false
            if(gun.ammoCount > 0)
            {
                gun.isEmpty = false;
                gun.gunAnimator.SetBool("isEmpty", gun.isEmpty);
            }

            // Setting the hasPlayed bool in the reloadAudioSequence to false after reloading
            foreach(AudioSequence emptyReloadAS in gun.emptyReloadAudioSequences)
            {
                emptyReloadAS.hasPlayed = false;
            }
            foreach(AudioSequence reloadAS in gun.reloadAudioSequences)
            {
                reloadAS.hasPlayed = false;
            }

            gun.reloadAudioSource.Stop();
        }

        // When the mag is empty, use the gun.rechamberDuration to determine the timing of which the player can do reload-cancelling
        if((gun.isEmpty ? gun.rechamberDuration : gun.magInDuration) / gun.reloadSpeed <= gun.reloadTimer)
        {
            // Action after reloading goes below
            int loadedAmmo = gun.magCapacity - gun.ammoCount;
            if(gun.ammoReserveCount < loadedAmmo)
            {
                loadedAmmo = gun.ammoReserveCount;
            }
            gun.ammoReserveCount -= loadedAmmo;
            gun.ammoCount += loadedAmmo;
        }

        // Cancel reload if sprint and isReloadWhileSprint is fault
        if(gun.characterController.isSprinting && !gun.isReloadableWhileSprint)
        {
            gun.isReloading = false;
        }
    }

    #endregion

    public void SwitchFireMode()
    {
        // If the fire mode is not switchable on the gun, return
        if(!gun.isFireModeSwitchable)
        {
            return;
        }

        // If the gun shoots burst fire, switch between semi-burst, if not switch between semi-full
        if(gun.fireMode == FireMode.Semi)
        {
            if(gun.canBurstFire)
            {
                gun.fireMode = FireMode.Burst;
            }
            else
            {
                gun.fireMode = FireMode.Full;
            }
        }
        else if(gun.fireMode == FireMode.Burst)
        {
            if(gun.canBurstFire)
            {
                gun.fireMode = FireMode.Semi;
            }
            else
            {
                gun.fireMode = FireMode.Full;
            }
        }
        else if(gun.fireMode == FireMode.Full)
        {
            gun.fireMode = FireMode.Semi;
        }
    }

    #region "Draw"

    public void Draw()
    {
        gun.armsAnimator.SetFloat("DrawSpeed", gun.drawSpeed);
        gun.armsAnimator.Play(gameObject.name + "_Draw_Animation", -1, 0);
        gun.isPuttingAway = false;
        gun.putAwayTimer = 0;
        gun.isDrawing = true;
    }

    public void CalculateDrawTime()
    {
        if(gun.isDrawing)
        {
            // Increase the timer when aiming in, until it reaches up to ADSduration, and if it reaches, freeze the timer by setting adsTimer to ADSduration
            if(gun.drawDuration / gun.drawSpeed > gun.drawTimer)
            {
                gun.drawTimer += Time.deltaTime;
            }
            else
            {
                gun.drawTimer = 0;
                gun.isDrawing = false;
                gun.armsAnimator.Play("Draw Empty");
                
                // If ADS when drawing the gun, it'll reset the timer to 0 and ADS from the start
                if(gun.isTryingToADSWhileDoingSomethingElse)
                {
                    ADSOut();
                    gun.adsTimer = 0;
                    ADSIn();
                }
            }
        }
    }

    #endregion

    #region "PutAway"

    public void PutAway()
    {        
        gun.armsAnimator.Play(gameObject.name + "_PutAway_Animation");

        if(!gun.isPuttingAway)
        {
            gun.isPuttingAway = !gun.isPuttingAway;            
            gun.armsAnimator.SetFloat("PutAwaySpeed", gun.putAwaySpeed);
        }
        else
        {
            gun.isPuttingAway = !gun.isPuttingAway;
            gun.armsAnimator.SetFloat("PutAwaySpeed", -gun.putAwaySpeed);
        }

        // Cancel reloading, burst firing when swapping weapon
        gun.isReloading = false;
        gun.isSubscribingBurstFire = false;
        gun.isBurstFiring = false;
        gun.burstCounter = 0;

        // This will ensure to stop drawing animation when weapon swapping
        gun.drawTimer = 0;
        gun.isDrawing = false;
        gun.armsAnimator.Play("Draw Empty");
    }

    public void CalculatePutAwayTime()
    {
        if(gun.isPuttingAway)
        {
            // Increase the timer when aiming in, until it reaches up to ADSduration, and if it reaches, freeze the timer by setting adsTimer to ADSduration
            if(gun.putAwayDuration / gun.putAwaySpeed > gun.putAwayTimer)
            {
                gun.putAwayTimer += Time.deltaTime;         
            }
            else
            {
                // If the timer reaches the putaway duration
                gun.putAwayTimer = gun.putAwayDuration / gun.putAwaySpeed;

                gun.armsAnimator.SetFloat("PutAwaySpeed", 0f);

                // Action after putting the weapon away goes in below
                gun.weaponController.SwitchWeapons();
            }

            // If PutAway when ADS,
            if(gun.isADS || gun.isADSIn)
            {
                ADSOut();
                gun.isTryingToADSWhileDoingSomethingElse = true;
            }
        }
        else
        {
            // Decrease the timer when cancelling putAway, until it reaches to 0, if it reaches, set the timer to 0
            if(gun.putAwayTimer > 0)
            {
                gun.putAwayTimer -= Time.deltaTime;
            }
            else
            {
                gun.putAwayTimer = 0;

                gun.armsAnimator.SetFloat("PutAwaySpeed", 0f);            
                // When not ADS (Hip fire) play the empty state
                gun.armsAnimator.Play("PutAway Empty");
            }

            // If weapon switch is cancelled when ADS it'll ADS
            if(gun.isTryingToADSWhileDoingSomethingElse)
            {            
                ADSIn();
            }
        }
    }

    #endregion

    #region "Walk & Sprint"

    public void Walk(float walkingAnimationSpeed, bool isIdle)
    {
        gun.armsAnimator.SetBool("isIdle", isIdle);
        gun.armsAnimator.SetFloat("WalkingAnimationSpeed", walkingAnimationSpeed);
    }

    public void Sprint(bool isSprint)
    {
        // When isReloadableWhileSprint is set to true, play fast walk animation when reloading
        if((gun.isReloading && gun.isReloadableWhileSprint) || (isSprint && gun.isFireableWhileSprint && Time.time < gun.lastFiredTime + 0.5f))
        {
            gun.armsAnimator.SetBool("isSprint", false);
            gun.armsAnimator.SetFloat("WalkingAnimationSpeed", 1.5f);
        }
        else
        {
            gun.armsAnimator.SetBool("isSprint", isSprint);
        }
    }

    #endregion
    
    #region "SetUp & Reset"

    public void SetUp()
    {
        // if(gun.inUse)
        // {
        //     gun.weaponController.currentWeapon = this.gameObject;
        // }

        // Setting up all the scripts and reference
        gun.characterController = transform.Find("../../../../../../").GetComponent<CharacterControllerScr>();
        gun.weaponController = transform.Find("../../").GetComponent<WeaponController>();

        gun.armsRig = transform.Find("../../WeaponADSAdjustmentLayer/WeaponADS/WeaponSway/WeaponRecoil/WeaponStanceAdjustmentLayer/WeaponStance/ArmsRig");
        gun.cameraRecoil = transform.Find("../../../");
        gun.camera = transform.Find("../../../CameraAnimator/Camera").GetComponent<Camera>();
        gun.weaponHolder = transform.Find("../../");
        gun.weaponADSAdjustmentLayer = transform.Find("../../WeaponADSAdjustmentLayer");
        gun.weaponADS = transform.Find("../../WeaponADSAdjustmentLayer/WeaponADS");
        gun.weaponSway = transform.Find("../../WeaponADSAdjustmentLayer/WeaponADS/WeaponSway");
        gun.weaponRecoil = transform.Find("../../WeaponADSAdjustmentLayer/WeaponADS/WeaponSway/WeaponRecoil");
        gun.weaponStanceAdjustmentLayer = transform.Find("../../WeaponADSAdjustmentLayer/WeaponADS/WeaponSway/WeaponRecoil/WeaponStanceAdjustmentLayer");
        gun.weaponStance = transform.Find("../../WeaponADSAdjustmentLayer/WeaponADS/WeaponSway/WeaponRecoil/WeaponStanceAdjustmentLayer/WeaponStance");
        gun.socket = transform.Find("../../WeaponADSAdjustmentLayer/WeaponADS/WeaponSway/WeaponRecoil/WeaponStanceAdjustmentLayer/WeaponStance/ArmsRig/arms_rig/root/upper_arm_R/lower_arm_R/hand_R/" + gameObject.name + "Socket");
        gun.swayPoint = transform.Find("../../SwayPoints/" + gameObject.name + "SwayPoint");

        gun.gunAnimator = GetComponent<Animator>();
        gun.armsAnimator = transform.Find("../../WeaponADSAdjustmentLayer/WeaponADS/WeaponSway/WeaponRecoil/WeaponStanceAdjustmentLayer/WeaponStance/ArmsRig").GetComponent<Animator>();
        gun.cameraAnimator = transform.Find("../../../CameraAnimator").GetComponent<Animator>();
        gun.adsAnimator = transform.Find("../../WeaponADSAdjustmentLayer/WeaponADS").GetComponent<Animator>();

        gun.ejectionPoint = transform.Find("EjectionPoint");

        gun.gunAnimator.runtimeAnimatorController = gun.gunAnimatorController;
        gun.armsAnimator.runtimeAnimatorController = gun.armsAnimatorController;
        gun.cameraAnimator.runtimeAnimatorController = gun.cameraAnimatorController;
        gun.adsAnimator.runtimeAnimatorController = gun.adsAnimatorController;

        gun.audioSource = GetComponent<AudioSource>();
        gun.reloadAudioSource = transform.Find("ReloadAudioSource").GetComponent<AudioSource>();

        // Setting the parent of primary/secondary weapon to it's appropriate weapon socket
        gun.weaponController.currentWeapon.transform.SetParent(gun.socket);
        gun.weaponController.currentWeapon.transform.localPosition = Vector3.zero;
        gun.weaponController.currentWeapon.transform.localRotation = Quaternion.Euler(gun.weaponRotation);

        // Unparent the armsRig from weaponRecoil and set the swayPoint to the appropriate swayPoint to the gun and parent it back to weaponRecoil
        gun.weaponStanceAdjustmentLayer.SetParent(gun.weaponHolder);
        gun.weaponADSAdjustmentLayer.localPosition = gun.swayPoint.localPosition;
        gun.weaponStanceAdjustmentLayer.SetParent(gun.weaponRecoil);

        Draw();
    }

    public void Reset()
    {
        // Setting every position back to normal
        gun.armsRig.localPosition = new Vector3(0, 0, -0.12f);
        gun.weaponSway.localPosition = Vector3.zero;

        // Resetting the parent of primary/secondary weapon to weaponHolder
        gun.weaponController.currentWeapon.transform.SetParent(gun.weaponHolder);
        gun.weaponController.currentWeapon.transform.localPosition = Vector3.zero;
        gun.weaponController.currentWeapon.transform.localRotation = Quaternion.Euler(Vector3.zero);
    }

    #endregion

    #region "Display"

    public void DisplayWeaponStats(TMP_Text weaponNameTMP, TMP_Text ammoCountTMP, TMP_Text ammoReserveCountTMP)
    {
        weaponNameTMP.text = gameObject.name + " : " + gun.fireMode.ToString();
        ammoCountTMP.text = gun.ammoCount.ToString();
        ammoReserveCountTMP.text = gun.ammoReserveCount.ToString();
    }

    public void DisplayCrossHair(RectTransform crossHair, RectTransform dot)
    {
        RectTransform currentCrossHair = null;

        switch (gun.crossHairType)
        {
            case CrossHairType.CrossHair:
                crossHair.gameObject.SetActive(true);
                dot.gameObject.SetActive(false);
                currentCrossHair = crossHair;
                break;

            case CrossHairType.Dot:
                crossHair.gameObject.SetActive(false);
                dot.gameObject.SetActive(true);
                currentCrossHair = dot;
                break;
        }

        CanvasGroup crossHairAlpha = currentCrossHair.gameObject.GetComponent<CanvasGroup>();

        // Calculate cross-hair size only when the currentCrossHair is other than dot, when currentCrossHairis dot keep the size at 0 to keep the bullet from spreading
        if(currentCrossHair == dot)
        {
            gun.currentCrossHairSize = 0;

            if(gun.isADSIn || gun.isADSOut || gun.isADS)
            {
                gun.currentAlpha = Mathf.Lerp(1, 0, (gun.adsTimer / (gun.adsDuration / gun.adsSpeed)) / 0.8f);
            }
            else
            {       
                gun.currentAlpha = 1;
            }
        }
        else
        {
            // Reducing the timer after a shot is fired
            if(gun.fireCrossHairTimer > 0)
            {
                gun.fireCrossHairTimer -= Time.deltaTime;
            }
            else
            {
                gun.fireCrossHairTimer = 0;
            }

            var stanceCrossHairSize = 0f;

            if(gun.characterController.isStand)
            {
                stanceCrossHairSize = gun.standCrossHairSize;
            }
            else if(gun.characterController.isCrouch)
            {
                stanceCrossHairSize = gun.crouchCrossHairSize;
            }
            else if(gun.characterController.isProne)
            {
                stanceCrossHairSize = gun.proneCrossHairSize;
            }

            gun.crossHairSize = Mathf.SmoothDamp(gun.crossHairSize, stanceCrossHairSize, ref gun.crossHairStanceSizeVelocity, gun.characterController.playerStanceSmoothing);        
            gun.walkCrossHairLerp = Mathf.Lerp(0, gun.walkAdditiveCrossHairSize, gun.characterController.smoothedWalkingAnimationSpeed);
            gun.fireCrossHairLerp = Mathf.Lerp(0, gun.fireAdditiveCrossHairSize, gun.fireCrossHairTimer / gun.crossHairResetDuration);

            gun.addedCrossHairSize = gun.crossHairSize + gun.fireCrossHairLerp + gun.walkCrossHairLerp;

            if(gun.isADSIn || gun.isADSOut || gun.isADS)
            {
                gun.currentCrossHairSize = Mathf.Lerp(gun.addedCrossHairSize, 0, gun.adsTimer / (gun.adsDuration / gun.adsSpeed));
                gun.currentAlpha = Mathf.Lerp(1, 0, (gun.adsTimer / (gun.adsDuration / gun.adsSpeed)) / 0.8f);
            }
            else
            {
                gun.currentCrossHairSize = gun.addedCrossHairSize;            
                gun.currentAlpha = 1;
            }

            gun.currentCrossHairSize = gun.currentCrossHairSize * (90f / gun.characterController.playerSettings.FieldOfView);
        }

        currentCrossHair.sizeDelta = new Vector2(gun.currentCrossHairSize, gun.currentCrossHairSize);
        crossHairAlpha.alpha = gun.currentAlpha;
        currentCrossHair.gameObject.SetActive(!gun.isADS);
    }

    #endregion

    #region "Weapon Stance"

    private void CalculateWeaponStance()
    {
        Vector3 stanceWeaponPosition = Vector3.zero;
        Vector3 stanceWeaponRotation = Vector3.zero;

        if(gun.characterController.isStand)
        {
            stanceWeaponPosition = Vector3.zero;
            stanceWeaponRotation = Vector3.zero;
        }
        else if(gun.characterController.isCrouch)
        {
            stanceWeaponPosition = gun.crouchWeaponPosition;
            stanceWeaponRotation = gun.crouchWeaponRotation;
        }
        else if(gun.characterController.isProne)
        {
            stanceWeaponPosition = gun.proneWeaponPosition;
            stanceWeaponRotation = gun.proneWeaponRotation;
        }

        // Saving the position and rotation of WeaponStance solely based on values without taking the change with ADS into account
        gun.weaponStanceReferencePosition = Vector3.SmoothDamp(gun.weaponStanceReferencePosition, stanceWeaponPosition, ref gun.weaponStanceReferencePositionVelocity, gun.characterController.playerStanceSmoothing); 
        gun.weaponStanceReferenceRotation = QuaternionSmoothDamp(gun.weaponStanceReferenceRotation, Quaternion.Euler(stanceWeaponRotation), ref gun.weaponStanceReferenceRotationVelocity, gun.characterController.playerStanceSmoothing);

        if(!gun.isADS)
        {
            gun.weaponStance.localPosition = Vector3.SmoothDamp(gun.weaponStance.localPosition, stanceWeaponPosition, ref gun.weaponStancePositionVelocity, gun.characterController.playerStanceSmoothing); 
            // Smoothly interpolate the rotation towards the target rotation
            gun.weaponStance.localRotation = QuaternionSmoothDamp(gun.weaponStance.localRotation, Quaternion.Euler(stanceWeaponRotation), ref gun.weaponStanceRotationVelocity, gun.characterController.playerStanceSmoothing);
        }
        else
        {
            if(gun.weaponStance.localPosition != stanceWeaponPosition || gun.weaponStance.localRotation != Quaternion.Euler(stanceWeaponRotation))
            {
                gun.weaponStance.localPosition = Vector3.SmoothDamp(gun.weaponStance.localPosition, stanceWeaponPosition, ref gun.weaponStancePositionVelocity, gun.characterController.playerStanceSmoothing); 
                // Smoothly interpolate the rotation towards the target rotation
                gun.weaponStance.localRotation = QuaternionSmoothDamp(gun.weaponStance.localRotation, Quaternion.Euler(stanceWeaponRotation), ref gun.weaponStanceRotationVelocity, gun.characterController.playerStanceSmoothing);
            }
        }
        
        // When ADS In and Out, reset the positon and rotation to wherever the WeaponStance should be
        gun.weaponStance.localPosition = Vector3.Lerp(gun.weaponStanceReferencePosition, Vector3.zero, gun.adsTimer / (gun.adsDuration / gun.adsSpeed));
        gun.weaponStance.localRotation = Quaternion.Lerp(gun.weaponStanceReferenceRotation, Quaternion.Euler(Vector3.zero), gun.adsTimer / (gun.adsDuration / gun.adsSpeed));
    }

    // Custom SmoothDamp function for Quaternions
    Quaternion QuaternionSmoothDamp(Quaternion current, Quaternion target, ref Quaternion velocity, float smoothTime)
    {
        // Smooth damp for each component
        Vector4 currentVec = new Vector4(current.x, current.y, current.z, current.w);
        Vector4 targetVec = new Vector4(target.x, target.y, target.z, target.w);
        Vector4 result = new Vector4(
            Mathf.SmoothDamp(currentVec.x, targetVec.x, ref velocity.x, smoothTime),
            Mathf.SmoothDamp(currentVec.y, targetVec.y, ref velocity.y, smoothTime),
            Mathf.SmoothDamp(currentVec.z, targetVec.z, ref velocity.z, smoothTime),
            Mathf.SmoothDamp(currentVec.w, targetVec.w, ref velocity.w, smoothTime)
        );

        // Normalize the result to get a valid quaternion
        return new Quaternion(result.x, result.y, result.z, result.w).normalized;
    }

    #endregion

    public void ReloadAudioSequencer()
    {
        if (!gun.isReloading)
        {
            return;
        }

        AnimatorStateInfo stateInfo = gun.armsAnimator.GetCurrentAnimatorStateInfo(3);
        if(stateInfo.IsName(gameObject.name + "_Arms_Empty_Reload_Animation"))
        {
            float elapsedTime = stateInfo.normalizedTime * stateInfo.length * stateInfo.speed;
            // if (frame >= targetFrame && !hasPlayedAudio)
            // {
            //     // gun.audioSource.PlayOneShot(gun.fireClip, 1);
            //     
            // }
            // Debug.Log(elapsedTime);
            
            foreach(AudioSequence AS in gun.reloadAudioSequences)
            {
                Debug.Log(AS.action);
            }
        }
    }
}

