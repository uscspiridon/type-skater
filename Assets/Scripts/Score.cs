using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using ScriptableObjectArchitecture;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(TextMeshProUGUI))]
public class Score : MonoBehaviour {
    public static Score Instance;

    // score-keeping data
    [HideInInspector] public int score;
    private int unsecuredScore;
    [SerializeField] private FloatReference currentMultiplier;
    private List<string> staleTricks = new List<string>();
    public int maxStaleTricks;
    
    // score animation
    public int minimumFontSize;
    public int maximumFontSize;
    
    // unsecured score
    public bool scoreIsUnsecured;
    public GameObject unsecuredScorePrefab;
    private TextMeshProUGUI unsecuredScoreText;
    private Animator unsecuredScoreAnimator;
    public GameObject unsecuredScoreLocation;
    
    // floating score
    public GameObject flyingScorePrefab;
    
    // component stuff
    private TextMeshProUGUI scoreText;
    public GameObject completedTrickTextPrefab;
    public Color completedTrickTextColor;
    
    private void Awake() {
        if (Instance == null) Instance = this;
        scoreText = GetComponent<TextMeshProUGUI>();
    }

    private void Start() {
        scoreText.text = score.ToString();

        TrickManager.Instance.OnCompleteTrick += CountTrick;

        Player.Instance.OnJump += () => {
            // instantiate unsecured score
            GameObject unsecuredScoreObject = Instantiate(unsecuredScorePrefab, unsecuredScoreLocation.transform, false);
            unsecuredScoreText = unsecuredScoreObject.GetComponent<TextMeshProUGUI>();
            unsecuredScoreAnimator = unsecuredScoreObject.GetComponent<Animator>();
            scoreIsUnsecured = true;
        };
        Player.Instance.OnSafeLanding += s => {
            SecureScore();
        };
        Player.Instance.OnUnsafeLanding += () => {
            unsecuredScore = 0;
            scoreIsUnsecured = false;
            // destroy unsecured animator
            if (unsecuredScoreAnimator != null) {
                unsecuredScoreAnimator.SetTrigger("lost");
                unsecuredScoreAnimator = null;
                unsecuredScoreText = null;
            }
        };

        if (GameManager.Instance) GameManager.Instance.OnGameOver += OnGameOver;
    }

    private void SecureScore() {
        // add score
        score += unsecuredScore;
        unsecuredScore = 0;
        scoreIsUnsecured = false;
        scoreText.text = score.ToString();
        // animate score
        scoreText.fontSize = Mathf.Lerp(minimumFontSize, maximumFontSize, score / 400f);
        Color originalColor = scoreText.color;
        scoreText.color = Color.white;
        scoreText.DOColor(originalColor, 1f);

        // animate unsecured score
        if (unsecuredScoreAnimator != null) {
            unsecuredScoreAnimator.SetBool("secured", true);
            unsecuredScoreAnimator = null;
            unsecuredScoreText = null;
        }
    }

    private void CountTrick(Trick trick) {
        int score = 0;
        if (trick.trickScore > 0) {
            // count how many times the trick appears in the stale list
            int appearing = 0;
            foreach (var staleTrick in staleTricks) {
                if (trick.Text == staleTrick) appearing++;
            }
            // calculate score
            float roughScore = trick.trickScore * currentMultiplier.Value;
            float staleMultiplier = 0.6f;
            for (int i = 0; i < appearing; i++) {
                var newRoughScore = roughScore * staleMultiplier;
                if (roughScore - newRoughScore < 1) {
                    roughScore--;
                }
                else roughScore = newRoughScore;
            }
            score = Mathf.FloorToInt(roughScore);
            if (score <= 0) score = 1;
            AddScore(score);
        
            // add to stale tricks
            staleTricks.Add(trick.Text);
            // push a trick out if stale maximum reached
            if(staleTricks.Count > maxStaleTricks) staleTricks.RemoveAt(0);
        }
        
        // spawn completed trick text
        GameObject completedTrickText = Instantiate(completedTrickTextPrefab, Player.Instance.transform.position, Quaternion.identity);
        TextMeshProUGUI text = completedTrickText.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        text.text = trick.trickScore > 0 ? trick.Text + " " + score : trick.Text;
        text.color = completedTrickTextColor;
        
        // game feel on tricks
        if (trick.trickScore > 0) {
            // screen shake
            StartCoroutine(CameraShake.Instance.Shake(0.05f));
            // player flash white
            PlayerAnimator.Instance.Flash();
        }
    }

    private void AddScore(int addition) {
        // if on ground
        if (Player.Instance.state == Player.State.OnGround) {
            score += addition;
            scoreText.text = score.ToString();
        }
        // if in the air, score is unsecured
        else {
            unsecuredScore += addition;
            unsecuredScoreText.text = unsecuredScore.ToString();

            AudioManager.Instance.PlayTrickSound();
            
            var flyingScoreText = Instantiate(flyingScorePrefab, unsecuredScoreText.transform, false)
                .GetComponent<TextMeshProUGUI>();
            flyingScoreText.text = "+" + addition.ToString();
        }
    }

    public int GetUnsecuredScore() {
        return unsecuredScore;
    }

    private void OnGameOver() {
        // secure unsecured score
        SecureScore();
        
        // compare with high score
        int highScore = PlayerPrefs.GetInt(SceneManager.GetActiveScene().name + "HighScore", 0);
        if (score > highScore) {
            highScore = score;
            PlayerPrefs.SetInt(SceneManager.GetActiveScene().name + "HighScore", highScore);
        }
    }
}
