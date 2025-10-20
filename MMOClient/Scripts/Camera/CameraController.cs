using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Camera Settings")]
    public float distance = 10f;
    public float minDistance = 5f;
    public float maxDistance = 20f;
    public float zoomSpeed = 2f;

    [Header("Rotation Settings")]
    public float rotationSpeed = 5f;
    public float minVerticalAngle = 10f;
    public float maxVerticalAngle = 80f;

    [Header("Offset")]
    public Vector3 offset = new Vector3(0, 5, 0);

    private float currentRotationY = 0f;
    private float currentRotationX = 45f;

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        HandleZoom();
        HandleRotation();
        UpdateCameraPosition();
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        distance -= scroll * zoomSpeed;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
    }

    private void HandleRotation()
    {
        if (Input.GetMouseButton(1)) // Botão direito do mouse
        {
            currentRotationY += Input.GetAxis("Mouse X") * rotationSpeed;
            currentRotationX -= Input.GetAxis("Mouse Y") * rotationSpeed;
            currentRotationX = Mathf.Clamp(currentRotationX, minVerticalAngle, maxVerticalAngle);
        }
    }

    private void UpdateCameraPosition()
    {
        Quaternion rotation = Quaternion.Euler(currentRotationX, currentRotationY, 0);
        Vector3 direction = rotation * Vector3.back;

        Vector3 targetPosition = target.position + offset;
        transform.position = targetPosition + direction * distance;
        transform.LookAt(targetPosition);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}