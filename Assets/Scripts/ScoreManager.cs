using TMPro;
using UnityEngine;

/// <summary>
/// Tracks and displays the current score and high score.
/// The score increments each time the bird passes through a pipe gap.
/// Uses PlayerPrefs to persist the high score between sessions.
///
/// Usage: Call AddPoint() from the ScoreTrigger's OnTriggerEnter2D.
/// The ScoreTrigger collider is a child of each pipe pair prefab.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text highScoreText;
    [SerializeField] private TMP_Text finalScoreText;

    private const string HIGH_SCORE_KEY = "FlappyHighScore";

    private int currentScore = 0;
    private int highScore = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        highScore = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
    }

    public void ResetScore()
    {
        currentScore = 0;
        UpdateScoreDisplay();
    }

    public void AddPoint()
    {
        currentScore++;
        UpdateScoreDisplay();
    }

    public int GetCurrentScore() => currentScore;
    public int GetHighScore() => highScore;

    public void SaveHighScore()
    {
        if (currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt(HIGH_SCORE_KEY, highScore);
            PlayerPrefs.Save();
        }

        if (finalScoreText != null)
            finalScoreText.text = $"Score: {currentScore}\nBest: {highScore}";
    }

    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
            scoreText.text = currentScore.ToString();

        if (highScoreText != null)
            highScoreText.text = $"Best: {highScore}";
    }
}
