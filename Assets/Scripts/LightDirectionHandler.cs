using UnityEngine;

public class LightDirectionHandler : MonoBehaviour
{
    [Header("Referensi Light")]
    [SerializeField] private Transform targetLightTransform; // Drag GameObject Light 2D ke sini
    [SerializeField] private float kecepatanRotasi = 15f; // Kecepatan smooth rotation

    private Vector2 arahTerakhir = Vector2.down; // Arah bawaan saat game mulai

    /// <summary>
    /// Panggil fungsi ini dari PlayerMovement saat input bergerak.
    /// </summary>
    public void UpdateArahCahaya(Vector2 arahInput)
    {
        if (arahInput.sqrMagnitude > 0.01f)
        {
            arahTerakhir = arahInput.normalized;
        }
    }

    private void Update()
    {
        if (targetLightTransform == null) return;

        // Hitung sudut rotasi Z berdasarkan Vector2 arah
        float angle = Mathf.Atan2(arahTerakhir.y, arahTerakhir.x) * Mathf.Rad2Deg;
        
        // Sesuaikan offset sudut jika sprite/mesh bawaan senter menghadap ke atas/samping.
        // Umumnya -90 derajat jika arah awal sprite senter menghadap ke atas.
        Quaternion targetRotation = Quaternion.Euler(0, 0, angle - 90f);

        // Rotasi dengan lerp agar transisi gerakan halus
        targetLightTransform.rotation = Quaternion.Lerp(
            targetLightTransform.rotation, 
            targetRotation, 
            Time.deltaTime * kecepatanRotasi
        );
    }
}