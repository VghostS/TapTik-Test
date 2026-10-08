using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GM : MonoBehaviour
{
    [Header("timer")]
    public TextMeshProUGUI timerText;
    public int timer = 120;

    [Space(20)]
    public TextMeshProUGUI coins_txt;
    public int coinCount = 0;
    [Space(12)]
    public GameObject WinWindow;
    public void Win()
    {
        Debug.Log("You Win!");
        Invoke("WinWindowActive", 1f);
        PlayerPrefs.SetInt("CoinCount_Prefs", coinCount + 40);
        CancelInvoke("UpdateTimer");
    }

    void WinWindowActive()
    {
        WinWindow.SetActive(true);
    }

    void Start()
    {
        Application.targetFrameRate = 60;
        InvokeRepeating("UpdateTimer", 0f, 1f);

        coinCount = PlayerPrefs.GetInt("CoinCount_Prefs", 0);
        coins_txt.text = coinCount.ToString();
    }

    void UpdateTimer()
    {
        timer--;
        // conver to minutes and seconds
        timerText.text = string.Format("{0:00}:{1:00}", timer / 60, timer % 60);

        if (timer <= 0)
        {
            CancelInvoke("UpdateTimer");
            Debug.Log("Game Over!");
        }
    }

    public void settings(int timeScale)
    {
        Time.timeScale = timeScale;
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
     }

}
