using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int targetGoldKeys = 3;
    public int goldKeysCollected = 0;

    public int targetSilverKeys = 2;
    public int silverKeysCollected = 0;

    public int totalScore = 0;
    public int pointsPerGoldKey = 100;
    public int pointsPerSilverKey = 50;
    public int pointsPerCoin = 10;
    public int pointsPerEnemy = 20;

    public TMP_Text pointText;
    public GameObject winPanel;
    public TMP_Text winText;
    public GameObject diePanel;
    public TMP_Text dieText;

    public AudioClip keyPickupSound;
    public AudioClip coinPickupSound;

    private bool hasWon = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (keyPickupSound == null)
        {
            keyPickupSound = Resources.Load<AudioClip>("Sounds/Pickup");
        }
        if (coinPickupSound == null)
        {
            coinPickupSound = Resources.Load<AudioClip>("Sounds/Pickup");
        }
    }

    private void Start()
    {
        Time.timeScale = 1f;

        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }

        if (diePanel != null)
        {
            diePanel.SetActive(false);
        }

        UpdateUI();
    }

    private void Update()
    {
        if ((diePanel != null && diePanel.activeSelf) || (winPanel != null && winPanel.activeSelf))
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                RestartGame();
            }
        }
    }

    public void CollectGoldKey()
    {
        if (hasWon) return;

        goldKeysCollected++;
        totalScore += pointsPerGoldKey;

        PlayKeyPickupSound();

        UpdateUI();
        CheckWinCondition();
    }

    public void CollectSilverKey()
    {
        if (hasWon) return;

        silverKeysCollected++;
        totalScore += pointsPerSilverKey;

        PlayKeyPickupSound();

        UpdateUI();
        CheckWinCondition();
    }

    public void CollectCoin()
    {
        totalScore += pointsPerCoin;
        PlayCoinPickupSound();
        UpdateUI();
    }

    public void AddPoints(int amount)
    {
        totalScore += amount;
        UpdateUI();
    }

    public void PlayKeyPickupSound()
    {
        if (keyPickupSound != null)
        {
            TopDownCharacter2D.FX.SoundManager.PlaySoundEffect(keyPickupSound);
        }
    }

    public void PlayCoinPickupSound()
    {
        if (coinPickupSound != null)
        {
            TopDownCharacter2D.FX.SoundManager.PlaySoundEffect(coinPickupSound);
        }
    }

    public void CheckWinCondition()
    {
        if (goldKeysCollected >= targetGoldKeys && silverKeysCollected >= targetSilverKeys)
        {
            Win();
        }
    }

    public void UpdateUI()
    {
        if (pointText != null)
        {
            pointText.text = $"Score: {totalScore} \nGold Keys: {goldKeysCollected}/{targetGoldKeys} \nSilver Keys: {silverKeysCollected}/{targetSilverKeys}";
        }
    }

    public void Win()
    {
        if (hasWon) return;
        hasWon = true;

        Debug.Log("VICTORY!");
        Debug.Log($" SCORE: {totalScore}");
  

        if (winPanel != null)
        {
            winPanel.SetActive(true);
        }

        if (winText != null)
        {
            winText.text = $"VICTORY! Score: {totalScore}";
            winText.gameObject.SetActive(true);
        }

        Time.timeScale = 0f;
    }

    public void GameOver()
    {
        if (hasWon) return;

        Debug.Log("GAME OVER!");

        if (diePanel != null)
        {
            diePanel.SetActive(true);
        }

        if (dieText != null)
        {
            dieText.text = "YOU DIED";
            dieText.gameObject.SetActive(true);
        }

        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
