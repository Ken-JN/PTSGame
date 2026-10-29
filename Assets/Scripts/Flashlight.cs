using UnityEngine;
using UnityEngine.Serialization;

public class Flashlight : MonoBehaviour
{
    [Tooltip("Transform of the 2D Light component")]
    public Transform playerLight;

    [Tooltip("Flashlight rotation smoothness speed")]
    [FormerlySerializedAs("kecepatanRotasiLight")]
    public float lightRotationSpeed = 15f;

    private Vector2 lastLightDirection = Vector2.down;

    void Start()
    {
    }

    void Update()
    {
        RotateLight();
    }

    void RotateLight()
    {
        if (playerLight == null) return;

        // Calculate angle from last light direction
        float angle = Mathf.Atan2(lastLightDirection.y, lastLightDirection.x) * Mathf.Rad2Deg;

        // Adjust rotation offset (-90f if default light sprite points upward at 0 degrees)
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, angle - 90f);

        playerLight.rotation = Quaternion.Lerp(
            playerLight.rotation,
            targetRotation,
            Time.deltaTime * lightRotationSpeed
        );
    }
}
