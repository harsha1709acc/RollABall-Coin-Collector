using UnityEngine;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// One small state owner keeps scoring, time, and the end screen consistent.
public class GameManager : MonoBehaviour
{
    public enum GameState { Playing, Won, Lost }
    public int totalCoins = 12;
    public float timeLimit = 60f;
    public TMP_Text scoreText;
    public TMP_Text timerText;
    public TMP_Text messageText;
    private int score;
    private float timeLeft;
    public GameState State { get; private set; }

    private void Start()
    {
        timeLeft = timeLimit;
        State = GameState.Playing;
        if (messageText != null) messageText.text = string.Empty;
        UpdateUI();
    }

    private void Update()
    {
        if (State == GameState.Playing)
        {
            timeLeft -= Time.deltaTime;
            if (timeLeft <= 0f) Lose();
        }
        UpdateUI();
        if (RestartPressed())
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEngine.SceneManagement.SceneManager.LoadScene(scene.buildIndex);
        }
    }

    public void CollectCoin()
    {
        if (State != GameState.Playing) return;
        score++;
        if (score >= totalCoins) Finish(GameState.Won);
    }

    public void Lose() { if (State == GameState.Playing) Finish(GameState.Lost); }

    private void Finish(GameState result)
    {
        State = result;
        if (messageText != null)
        {
            messageText.text = (result == GameState.Won ? "YOU WIN!" : "GAME OVER") + "\nPress R to restart";
            messageText.color = result == GameState.Won ? Color.green : Color.red;
        }
    }

    private void UpdateUI()
    {
        if (scoreText != null) scoreText.text = "Coins: " + score + " / " + totalCoins;
        if (timerText != null) timerText.text = "Time: " + Mathf.CeilToInt(Mathf.Max(0f, timeLeft));
    }

    private bool RestartPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.R);
#endif
    }
}
