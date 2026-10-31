using UnityEngine;
using UnityEngine.InputSystem;

public class LightDirectionHandler : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Transform of the 2D Light (child of player)")]
    [SerializeField] private Transform targetLightTransform;
    [Tooltip("Main camera in the scene")]
    [SerializeField] private Camera mainCamera;

    [Header("Rotation Settings")]
    [Tooltip("Rotation smoothness speed")]
    [SerializeField] private float rotationSpeed = 15f;
    [Tooltip("Rotation offset angle in degrees (e.g. -90 if default light sprite points up)")]
    [SerializeField] private float rotationOffset = -90f;

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        // Auto-detect light in children if not assigned
        if (targetLightTransform == null)
        {
            var light2D = GetComponentInChildren<UnityEngine.Rendering.Universal.Light2D>();
            if (light2D != null)
            {
                targetLightTransform = light2D.transform;
            }
        }
    }

    private void Update()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        if (targetLightTransform == null) return;

        // 1. Get mouse position from New Input System (with legacy fallback)
        Vector2 mouseScreenPos;
        if (Mouse.current != null)
        {
            mouseScreenPos = Mouse.current.position.ReadValue();
        }
        else
        {
            mouseScreenPos = Input.mousePosition;
        }

        // 2. Convert to world position
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, -mainCamera.transform.position.z));
        mouseWorldPos.z = 0f;

        // 3. Calculate direction vector from light to mouse
        Vector2 directionToMouse = (Vector2)(mouseWorldPos - targetLightTransform.position);
        if (directionToMouse.sqrMagnitude < 0.001f) return;

        // 4. Calculate angle on Z axis
        float angle = Mathf.Atan2(directionToMouse.y, directionToMouse.x) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, angle + rotationOffset);

        // 5. Smoothly interpolate rotation towards mouse
        targetLightTransform.rotation = Quaternion.Lerp(
            targetLightTransform.rotation,
            targetRotation,
            Time.deltaTime * rotationSpeed
        );
    }
}
