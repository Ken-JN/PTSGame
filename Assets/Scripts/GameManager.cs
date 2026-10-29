using UnityEngine;
using UnityEngine.Serialization;
using TMPro;

/// <summary>
///     GAME MANAGER
///     Manages the score system and victory condition.
///     Player wins after collecting the required number of Gold Keys AND Fake/Silver Keys!
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("=== 1. VICTORY CONDITIONS (GOLD & FAKE KEYS) ===")]
    [Tooltip("Target number of Gold Keys required to win")]
    [FormerlySerializedAs("targetKunciEmas")]
    public int targetGoldKeys = 3;

    [Tooltip("Number of Gold Keys collected so far")]
    [FormerlySerializedAs("kunciEmasTerkumpul")]
    public int goldKeysCollected = 0;

    [Tooltip("Target number of Fake/Silver Keys required to win")]
    [FormerlySerializedAs("targetKunciKw")]
    public int targetFakeKeys = 2;

    [Tooltip("Number of Fake/Silver Keys collected so far")]
    [FormerlySerializedAs("kunciKwTerkumpul")]
    public int fakeKeysCollected = 0;

    [Header("=== 2. SCORE SYSTEM ===")]
    [Tooltip("Current total player score")]
    [FormerlySerializedAs("totalPoin")]
    public int totalScore = 0;

    [Tooltip("Points awarded for each Gold Key collected")]
    [FormerlySerializedAs("poinPerKunciEmas")]
    [FormerlySerializedAs("poinPerKunci")]
    public int pointsPerGoldKey = 100;

    [Tooltip("Points awarded for each Fake Key collected")]
    [FormerlySerializedAs("poinPerKunciKw")]
    public int pointsPerFakeKey = 50;

    [Tooltip("Points awarded for each Coin collected")]
    [FormerlySerializedAs("poinPerKoin")]
    public int pointsPerCoin = 10;

    [Tooltip("Points awarded when defeating an Enemy")]
    [FormerlySerializedAs("poinPerMusuh")]
    public int pointsPerEnemy = 20;

    [Header("=== 3. UI DISPLAY ===")]
    [Tooltip("Text UI component to display score & key counts (TextMeshPro)")]
    public TMP_Text pointText;

    [Tooltip("Win panel UI GameObject displayed upon winning")]
    public GameObject winPanel;

    [Tooltip("Win text UI component")]
    public TMP_Text winText;

    private bool hasWon = false;

    private void Awake()
    {
        // Singleton pattern: ensure only one GameManager instance exists
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
        // Hide win panel at start
        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }

        UpdateUI();
    }

    /// <summary>
    ///     Called when the player collects a Gold Key
    /// </summary>
    public void CollectGoldKey()
    {
        if (hasWon) return;

        goldKeysCollected++;
        totalScore += pointsPerGoldKey;

        Debug.Log($"<color=yellow><b>[GOLD KEY]</b> Collected! ({goldKeysCollected}/{targetGoldKeys}) | Total Score: {totalScore}</color>");

        UpdateUI();
        CheckWinCondition();
    }

    /// <summary>
    ///     Called when the player collects a Fake Key
    /// </summary>
    public void CollectFakeKey()
    {
        if (hasWon) return;

        fakeKeysCollected++;
        totalScore += pointsPerFakeKey;

        Debug.Log($"<color=cyan><b>[FAKE KEY]</b> Collected! ({fakeKeysCollected}/{targetFakeKeys}) | Total Score: {totalScore}</color>");

        UpdateUI();
        CheckWinCondition();
    }

    /// <summary>
    ///     Checks if victory condition has been met (both Gold and Fake keys collected)
    /// </summary>
    public void CheckWinCondition()
    {
        if (goldKeysCollected >= targetGoldKeys && fakeKeysCollected >= targetFakeKeys)
        {
            Win();
        }
    }

    /// <summary>
    ///     Adds points when picking up a coin
    /// </summary>
    public void CollectCoin()
    {
        totalScore += pointsPerCoin;
        Debug.Log($"<color=yellow>[COIN]</color> +{pointsPerCoin} Points! Total: {totalScore}");
        UpdateUI();
    }

    /// <summary>
    ///     Adds a specific amount of score points
    /// </summary>
    public void AddPoints(int amount)
    {
        totalScore += amount;
        UpdateUI();
    }

    /// <summary>
    ///     Updates UI score and key counter texts
    /// </summary>
    public void UpdateUI()
    {
        if (pointText != null)
        {
            pointText.text = $"Score: {totalScore} | Gold: {goldKeysCollected}/{targetGoldKeys} | Fake: {fakeKeysCollected}/{targetFakeKeys}";
        }
    }

    /// <summary>
    ///     Handles win state when all requirements are met
    /// </summary>
    public void Win()
    {
        if (hasWon) return;
        hasWon = true;

        Debug.Log("<color=green><b>====================================================</b></color>");
        Debug.Log("<color=green><b>CONGRATULATIONS! YOU WIN! ALL KEYS COLLECTED!</b></color>");
        Debug.Log($"<color=green><b>Gold Keys: {goldKeysCollected}/{targetGoldKeys} | Fake Keys: {fakeKeysCollected}/{targetFakeKeys}</b></color>");
        Debug.Log($"<color=green><b>FINAL SCORE: {totalScore}</b></color>");
        Debug.Log("<color=green><b>====================================================</b></color>");

        if (winPanel != null)
        {
            winPanel.SetActive(true);
        }

        if (winText != null)
        {
            winText.text = $"VICTORY!\nAll Gold Keys ({goldKeysCollected}/{targetGoldKeys}) & Fake Keys ({fakeKeysCollected}/{targetFakeKeys}) Found!\nTotal Score: {totalScore}";
            winText.gameObject.SetActive(true);
        }

        // Pause game upon victory
        Time.timeScale = 0f;
    }

    private void OnEnable()
    {
        Enemy.OnZombieDeath += OnEnemyDied;
    }

    private void OnDisable()
    {
        Enemy.OnZombieDeath -= OnEnemyDied;
    }

    private void OnEnemyDied(Enemy zombie)
    {
        AddPoints(pointsPerEnemy);
    }
}
