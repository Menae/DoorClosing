using UnityEngine;

public sealed class RotatingCube : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private Vector3 rotationAxis = Vector3.up;

    private void Update()
    {
        if (rotationAxis.sqrMagnitude > 0f)
        {
            transform.Rotate(rotationAxis.normalized, rotationSpeed * Time.deltaTime, Space.Self);
        }
    }
}
