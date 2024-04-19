using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Models;

public class CharacterControllerScr : MonoBehaviour
{
    private CharacterController characterController;
    private DefaultInput defaultInput;
    public Vector2 input_Movement;
    public Vector2 input_View;

    private Vector3 newCameraRotation;
    private Vector3 newCharacterRotation;

    [Header("References")]
    public Transform cameraHolder;

    [Header("Settings")]
    public PlayerSettingsModel playerSettings;
    public float viewClampYMin = -89;
    public float viewClampYMax = 89;

    // Awake method is called when the script instance is being loaded
    private void Awake()
    {
        // Initialize defaultInput object by creating a new instance of DefaultInput class
        defaultInput = new DefaultInput();

        // Subscribe to the performed event of the Movement action in the Character action map
        // Assign a lambda function as the event handler to update input_Movement when the action is performed
        defaultInput.Character.Movement.performed += e => input_Movement = e.ReadValue<Vector2>();
        defaultInput.Character.View.performed += e => input_View = e.ReadValue<Vector2>();

        defaultInput.Enable();

        newCameraRotation = cameraHolder.localRotation.eulerAngles;
        newCharacterRotation = transform.localRotation.eulerAngles;

        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        CalculateView();
        CalculateMovement();
    }

    private void CalculateView()
    {
        newCharacterRotation.y += playerSettings.ViewXSensitivity * (playerSettings.ViewXInverted ? -input_View.x : input_View.x)* Time.deltaTime;
        transform.localRotation = Quaternion.Euler(newCharacterRotation);

        // If the boolean playerSettings.ViewYInverted is true the left side of the options followed by ? is selected and vice versa
        newCameraRotation.x += playerSettings.ViewYSensitivity * (playerSettings.ViewYInverted ? input_View.y : -input_View.y) * Time.deltaTime;
        newCameraRotation.x = Mathf.Clamp(newCameraRotation.x, viewClampYMin, viewClampYMax);

        cameraHolder.localRotation = Quaternion.Euler(newCameraRotation);
    }

    private void CalculateMovement()
    {
        var verticalSpeed = playerSettings.WalkingForwardSpeed * input_Movement.y * Time.deltaTime;
        var horizontalSpeed = playerSettings.WalkingStrafeSpeed * input_Movement.x * Time.deltaTime;

        var newMovementSpeed = new Vector3(horizontalSpeed, 0, verticalSpeed);

        newMovementSpeed = transform.TransformDirection(newMovementSpeed);

        characterController.Move(newMovementSpeed);

    }
}
