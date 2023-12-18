﻿using System;
using System.Collections.Generic;
using UnityEngine;

public class PauseMenuManager : MonoBehaviour {
    public KeyCode pauseButton;
    [SerializeField] private GameObject pauseMenu;
    [Space]
    public List<GameObject> objectsToDisable;

    private bool _gameStoppedBeforePause;
    private bool _gameIsOver = false;
    private List<bool> _wereObjectsActive = new List<bool>();

    private void Awake() {
        pauseMenu.SetActive(false);
    }

    private void Start() {
        foreach (var t in objectsToDisable) {
            _wereObjectsActive.Add(t.activeSelf);
        }
        ClosePauseMenu();

        GameManager.Instance.OnGameOver += OnGameOver;
    }
    
    private void Update() {
        if (GameManager.Instance && GameManager.Instance.gameStopped) return;
        
        if (Input.GetKeyDown(pauseButton)) {
            if (pauseMenu.activeSelf) ClosePauseMenu();
            else OpenPauseMenu();
        }
    }

    private void OpenPauseMenu() {
        if (_gameIsOver)
            return;
        
        pauseMenu.SetActive(true);
        
        for (int i = 0; i < objectsToDisable.Count; i++) {
            _wereObjectsActive[i] = objectsToDisable[i].activeSelf;
            objectsToDisable[i].SetActive(false);
        }
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        _gameStoppedBeforePause = GameManager.Instance.gameStopped;
        if(!_gameStoppedBeforePause) Pause();
    }

    public void ClosePauseMenu() {
        pauseMenu.SetActive(false);

        for (int i = 0; i < objectsToDisable.Count; i++) {
            objectsToDisable[i].SetActive(_wereObjectsActive[i]);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        GameManager.Instance.gameStopped = _gameStoppedBeforePause;
        if(!GameManager.Instance.gameStopped) Resume();
    }
    
    private void Pause(bool pauseAudio = true) {
        GameManager.Instance.Pause(pauseAudio);
    }

    private void Resume(bool resumeAudio = true) {
        GameManager.Instance.Resume(resumeAudio);
    }

    private void OnGameOver() {
        ClosePauseMenu();
        _gameIsOver = true;
    }
}