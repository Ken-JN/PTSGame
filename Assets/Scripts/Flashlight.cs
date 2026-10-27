using UnityEngine;
using UnityEngine.UIElements;

public class Flashlight : MonoBehaviour
{

    public Transform playerLight; // Drag Transform Light 2D di Inspector
    public float kecepatanRotasiLight = 15f;
    private Vector2 arahTerakhirCahaya = Vector2.down;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        RotateLight();
    }

    void RotateLight()
    {
        if (playerLight == null) return;

        // Hitung sudut dari Vector2 arahTerakhirCahaya
        float angle = Mathf.Atan2(arahTerakhirCahaya.y, arahTerakhirCahaya.x) * Mathf.Rad2Deg;

        // Sesuaikan offset (-90f jika cahaya bawaan menghadap Atas pada Euler 0)
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, angle - 90f);

        playerLight.rotation = Quaternion.Lerp(
            playerLight.rotation,
            targetRotation,
            Time.deltaTime * kecepatanRotasiLight
        );
}

}
