using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InverseRotationTest : MonoBehaviour
{
    public Transform parentTransform; // Reference to the parent transform

    private Quaternion previousRotation;

    void Start()
    {
        if (parentTransform == null)
        {
            parentTransform = transform.parent;
        }

        previousRotation = transform.rotation;
    }

    void Update()
    {
        if (parentTransform != null)
        {
            // Calculate the rotation difference
            Quaternion deltaRotation = transform.rotation * Quaternion.Inverse(previousRotation);

            // Calculate the direction and distance from the child to the parent
            Vector3 directionToParent = parentTransform.position - transform.position;
            float distanceToParent = directionToParent.magnitude;

            // Apply the rotation to the direction vector
            Vector3 newDirection = deltaRotation * directionToParent;

            // Update the parent's position
            parentTransform.position = transform.position + newDirection.normalized * distanceToParent;

            // Apply the delta rotation to the parent
            parentTransform.rotation = deltaRotation * parentTransform.rotation;

            // Update the previous rotation to the current rotation
            previousRotation = transform.rotation;
        }
    }
}
