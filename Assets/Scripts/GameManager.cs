using UnityEngine;
using UnityEngine.Serialization;
using TMPro;

/// <summary>
///     GAME MANAGER
///     Mengatur sistem poin dan kondisi kemenangan (Victory Condition).
///     Pemain akan MENANG setelah mengumpulkan sejumlah Kunci Emas DAN Kunci KW yang ditentukan!
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("=== 1. SYARAT KEMENANGAN (KUNCI EMAS & KUNCI KW) ===")]
    [Tooltip("Target jumlah Kunci Emas yang harus dikumpulkan")]
    public int targetKunciEmas = 3;

    [Tooltip("Jumlah Kunci Emas yang sudah dikumpulkan")]
    public int kunciEmasTerkumpul = 0;

    [Tooltip("Target jumlah Kunci KW yang harus dikumpulkan")]
    public int targetKunciKw = 2;

    [Tooltip("Jumlah Kunci KW yang sudah dikumpulkan")]
    public int kunciKwTerkumpul = 0;

    [Header("=== 2. SISTEM POIN ===")]
    [Tooltip("Total poin pemain saat ini")]
    public int totalPoin = 0;

    [Tooltip("Poin yang didapatkan setiap mengambil 1 Kunci Emas")]
    [FormerlySerializedAs("poinPerKunci")]
    public int poinPerKunciEmas = 100;

    [Tooltip("Poin yang didapatkan setiap mengambil 1 Kunci KW")]
    public int poinPerKunciKw = 50;

    [Tooltip("Poin yang didapatkan setiap mengambil 1 Koin")]
    public int poinPerKoin = 10;

    [Tooltip("Poin yang didapatkan saat mengalahkan Musuh")]
    public int poinPerMusuh = 20;

    [Header("=== 3. UI DISPLAY (OPSIONAL) ===")]
    [Tooltip("Text UI untuk menampilkan skor & jumlah kunci (TextMeshPro)")]
    public TMP_Text pointText;

    [Tooltip("Panel UI Kemenangan (misal WinPanel yang muncul saat menang)")]
    public GameObject winPanel;

    [Tooltip("Text UI Kemenangan")]
    public TMP_Text winText;

    private bool sudahMenang = false;

    private void Awake()
    {
        // Singleton pattern: memastikan hanya ada 1 GameManager di scene
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Sembunyikan panel kemenangan di awal game
        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }

        UpdateUI();
    }

    /// <summary>
    ///     Dipanggil ketika pemain mengambil Kunci Emas
    /// </summary>
    public void KumpulkanKunciEmas()
    {
        if (sudahMenang) return;

        kunciEmasTerkumpul++;
        totalPoin += poinPerKunciEmas;

        Debug.Log($"<color=yellow><b>[KUNCI EMAS]</b> Berhasil diambil! ({kunciEmasTerkumpul}/{targetKunciEmas}) | Total Poin: {totalPoin}</color>");

        UpdateUI();
        PeriksaKondisiKemenangan();
    }

    /// <summary>
    ///     Dipanggil ketika pemain mengambil Kunci KW
    /// </summary>
    public void KumpulkanKunciKw()
    {
        if (sudahMenang) return;

        kunciKwTerkumpul++;
        totalPoin += poinPerKunciKw;

        Debug.Log($"<color=cyan><b>[KUNCI KW]</b> Berhasil diambil! ({kunciKwTerkumpul}/{targetKunciKw}) | Total Poin: {totalPoin}</color>");

        UpdateUI();
        PeriksaKondisiKemenangan();
    }

    /// <summary>
    ///     Mengecek apakah syarat kemenangan sudah terpenuhi:
    ///     Kunci Emas DAN Kunci KW keduanya harus sudah mencapai target!
    /// </summary>
    private void PeriksaKondisiKemenangan()
    {
        if (kunciEmasTerkumpul >= targetKunciEmas && kunciKwTerkumpul >= targetKunciKw)
        {
            Menang();
        }
    }

    /// <summary>
    ///     Menambah poin saat mengambil koin
    /// </summary>
    public void AmbilKoin()
    {
        totalPoin += poinPerKoin;
        Debug.Log($"<color=yellow>[KOIN]</color> +{poinPerKoin} Poin! Total: {totalPoin}");
        UpdateUI();
    }

    /// <summary>
    ///     Menambah poin dalam jumlah tertentu
    /// </summary>
    public void TambahPoin(int jumlahPoin)
    {
        totalPoin += jumlahPoin;
        UpdateUI();
    }

    /// <summary>
    ///     Memperbarui teks antarmuka (UI) skor dan kunci
    /// </summary>
    private void UpdateUI()
    {
        if (pointText != null)
        {
            pointText.text = $"Poin: {totalPoin} | Emas: {kunciEmasTerkumpul}/{targetKunciEmas} | KW: {kunciKwTerkumpul}/{targetKunciKw}";
        }
    }

    /// <summary>
    ///     Kondisi saat semua kunci telah berhasil dikumpulkan
    /// </summary>
    private void Menang()
    {
        if (sudahMenang) return;
        sudahMenang = true;

        Debug.Log("<color=green><b>====================================================</b></color>");
        Debug.Log("<color=green><b>SELAMAT! KAMU MENANG! SEMUA KUNCI EMAS & KW TERKUMPUL!</b></color>");
        Debug.Log($"<color=green><b>Kunci Emas: {kunciEmasTerkumpul}/{targetKunciEmas} | Kunci KW: {kunciKwTerkumpul}/{targetKunciKw}</b></color>");
        Debug.Log($"<color=green><b>TOTAL POIN AKHIR: {totalPoin}</b></color>");
        Debug.Log("<color=green><b>====================================================</b></color>");

        // Tampilkan panel kemenangan jika ada
        if (winPanel != null)
        {
            winPanel.SetActive(true);
        }

        if (winText != null)
        {
            winText.text = $"VICTORY!\nSemua Kunci Emas ({kunciEmasTerkumpul}/{targetKunciEmas}) & Kunci KW ({kunciKwTerkumpul}/{targetKunciKw}) Ditemukan!\nTotal Poin: {totalPoin}";
            winText.gameObject.SetActive(true);
        }

        // Hentikan permainan saat menang
        Time.timeScale = 0f;
    }

    private void OnEnable()
    {
        Enemy.OnZombieMati += TambahPoinSaatMusuhMati;
    }

    private void OnDisable()
    {
        Enemy.OnZombieMati -= TambahPoinSaatMusuhMati;
    }

    private void TambahPoinSaatMusuhMati(Enemy zombie)
    {
        TambahPoin(poinPerMusuh);
    }
}
