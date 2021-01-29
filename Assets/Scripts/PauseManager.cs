﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class PauseManager : MonoBehaviour
{
    GameManager GM;
    public static PauseManager pauseManager;
    public static bool IsGamePaused = false;
    [SerializeField] string sceneToLoad;
    [SerializeField] private GameObject pauseMenuUI, resultsMenuUI;
    [SerializeField] private string[] missionResults;
    public TextMeshProUGUI[] pointsText;

    private void Start()
    {
        pauseManager = this;
        GM = GameManager.instance;
    }
    public void PauseButtonPressed()
    {
        if (IsGamePaused)
            Resume();
        else
            Pause();
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        IsGamePaused = false;
    }
    private void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale=0f;
        IsGamePaused = true;
    }
    public void Results()
    {
        resultsMenuUI.SetActive(true);
        DisplayResults();
        Time.timeScale=0f;
        IsGamePaused = true;
    }
    private void DisplayResults()
    {
        missionResults = Mission.instance.scoreText;
        pointsText[0].text = ($"{Mission.instance.menuTimer.text}\r\n {Mission.instance.missionScore * 5}%\r\n {Mission.instance.enemiesKilled}\r\n {Mission.instance.damageCount}\r\n {Mission.instance.retryCount}");//stats
        pointsText[1].text = ($"{missionResults[0]}p\r\n {missionResults[1]}p\r\n {missionResults[2]}p\r\n {missionResults[3]}p\r\n {missionResults[4]}p");
        pointsText[2].text = ($"{missionResults[5]}p");
    }
    public void LoadMenu()
    {
        IsGamePaused = false;
        Time.timeScale = 1f;
        Destroy(Mission.instance.gameObject);
        MusicManager.instance.StopMusic();
        GM.RestoreCheckpointStart();
        SceneTransitionController.instance.LoadScene(sceneToLoad);
    }
    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    
}
