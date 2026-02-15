using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    int score;
    public static GameManager inst;

    [SerializeField] Text ScoreText;

    [SerializeField] Player_Movement playerMovement;

    public void IncrementScore ()
    {
        score++;
        if (ScoreText != null)
            ScoreText.text = "SCORE: " + score;
        else
        {
            Debug.LogWarning("ScoreText is null; score increased but not visible in UI.");
        }
        playerMovement.Speed += playerMovement.speedIncreasePerPoint; 
    }
    private void Awake()
    {
        inst = this;

        if (ScoreText == null)
        {
            ScoreText = GameObject.Find("ScoreText")?.GetComponent<Text>();
            if (ScoreText == null)
            {
                Debug.LogError("ScoreText reference is missing. Assign it in the Inspector or name the UI object 'ScoreText'.");
            }
        }

        if (ScoreText != null)
            ScoreText.text = "SCORE: " + score;

    }
}
