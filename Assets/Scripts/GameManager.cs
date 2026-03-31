using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages game state transitions: Start → Playing → Game Over → Restart
///
/// State machine:
///   Idle      → Player presses flap input → Playing
///   Playing   → Bird dies                 → GameOver
///   GameOver  → Player presses Restart    → Idle (scene reload)
///
/// Inspector Setup:
///   - Assign all serialized references in the Unity Inspector
///   - Restart Button's OnClick() should call GameManager.RestartGame()
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Idle, Playing, GameOver }
    public GameState State { get; private set; } = GameState.Idle;

    [Header("Scene References")]
    [SerializeField] private BirdController bird;
    [SerializeField] private PipeSpawner pipeSpawner;

    [Header("UI Panels")]
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject gameOverPanel;

    private Vector3 birdStartPosition;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (bird == null) Debug.LogError("[GameManager] Bird reference not set!");
        if (pipeSpawner == null) Debug.LogError("[GameManager] PipeSpawner reference not set!");

        birdStartPosition = bird != null ? bird.transform.position : new Vector3(-3, 0, 0);

        ShowStartScreen();
    }

    private void Update()
    {
        // Listen for flap input to start the game from Idle
        if (State == GameState.Idle)
        {
            bool anyInput = Input.GetKeyDown(KeyCode.Space)
                         || Input.GetMouseButtonDown(0)
                         || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);

            if (anyInput)
            {
                StartGame();
            }
        }
    }

    private void ShowStartScreen()
    {
        State = GameState.Idle;

        if (startPanel != null) startPanel.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        if (bird != null) bird.ResetBird(birdStartPosition);
        if (pipeSpawner != null) pipeSpawner.ClearAllPipes();
        if (ScoreManager.Instance != null) ScoreManager.Instance.ResetScore();
    }

    private void StartGame()
    {
        State = GameState.Playing;

        if (startPanel != null) startPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        if (bird != null) bird.Activate();
        if (pipeSpawner != null) pipeSpawner.SetSpawning(true);
    }

    /// <summary>Called by BirdController when the bird collides with a pipe or ground</summary>
    public void OnBirdDied()
    {
        if (State == GameState.GameOver) return;
        State = GameState.GameOver;

        if (pipeSpawner != null) pipeSpawner.SetSpawning(false);
        if (ScoreManager.Instance != null) ScoreManager.Instance.SaveHighScore();

        // Small delay before showing Game Over panel (feels better)
        Invoke(nameof(ShowGameOver), 0.6f);
    }

    private void ShowGameOver()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
    }

    /// <summary>Called by the Restart button's OnClick() event in the Inspector</summary>
    public void RestartGame()
    {
        // Reload the scene — cleanest restart method
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
