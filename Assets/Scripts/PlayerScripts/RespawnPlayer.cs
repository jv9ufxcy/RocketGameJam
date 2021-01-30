﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RespawnPlayer : MonoBehaviour
{
    private GameManager gm;
    public static event Action PlayerRespawnedFromCheckpoint;

    private void Start()
    {
        gm = GameManager.instance;
        transform.position = gm.lastCheckpointPos;
    }
    public void Respawn()
    {
        if (Mission.instance != null)
            Mission.instance.OnPlayerContinue();
        StartCoroutine(RespawnWait());
    }
    private IEnumerator RespawnWait()
    {
        yield return new WaitForSeconds(2f);
        SceneTransitionController.instance.LoadScene(SceneManager.GetActiveScene().name);
    }
}
