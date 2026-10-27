using UnityEngine;

public class LightDirectionHandler : MonoBehaviour
{
    [Header("Referensi")]
    [SerializeField] private Transform targetLightTransform; // Drag GameObject Light 2D ke sini
    [SerializeField] private Camera mainCamera;               // Drag Camera utama (opsional, otomatis dicari jika kosong)

    [Header("Pengaturan Rotasi")]
    [SerializeField] private float kecepatanRotasi = 15f;     // Kecepatan transisi rotasi halus
    [SerializeField] private float offsetRotasi = -90f;       // Sesuaikan jika arah senter miring (biasanya -90f jika sprite/cahaya default menghadap ke atas)

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (targetLightTransform == null || mainCamera == null) return;

        // 1. Ambil posisi kursor di layar lalu ubah ke koordinat dunia (World Space)
        Vector3 mouseScreenPos = Input.mousePosition;
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);

        // 2. Hitung vektor arah dari posisi senter/karakter ke posisi mouse
        Vector2 arahKeMouse = (mouseWorldPos - targetLightTransform.position);

        // Hindari perhitungan jika kursor berada tepat di titik tengah
        if (arahKeMouse.sqrMagnitude < 0.001f) return;

        // 3. Hitung sudut rotasi pada sumbu Z
        float angle = Mathf.Atan2(arahKeMouse.y, arahKeMouse.x) * Mathf.Rad2Deg;

        // 4. Terapkan offset agar ujung cahaya pas mengarah ke kursor
        Quaternion targetRotation = Quaternion.Euler(0, 0, angle + offsetRotasi);

        // 5. Transisi rotasi secara halus menggunakan Lerp
        targetLightTransform.rotation = Quaternion.Lerp(
            targetLightTransform.rotation,
            targetRotation,
            Time.deltaTime * kecepatanRotasi
        );
    }
}