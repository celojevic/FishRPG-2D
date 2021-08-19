using System;
using System.Collections.Generic;
using UnityEngine;

public class UiManager : MonoBehaviour
{

    #region Singleton

    /// <summary>
    /// Singleton instance of the UI manager.
    /// </summary>
    public static UiManager Instance;

    void InitSingleton()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);
    }

    #endregion

    private static Player _player;
    public static Player Player
    {
        get => _player;
        set
        {
            _player = value;
            OnPlayerAssigned?.Invoke();
        }
    }
    public static event Action OnPlayerAssigned;

    [SerializeField] private KeyCode _closeWindowKey = KeyCode.Escape;

    private UiPanel[] _panels;

    private void Awake()
    {
        InitSingleton();
        FindUiPanels();
    }

    private void Start()
    {
        foreach (var item in _panels)
        {
            item.OnStart();
        }
    }

    private void Update()
    {
        if (Player == null) return;

        if (Input.GetKeyDown(_closeWindowKey))
        {
            CloseLastWindow();
        }

        foreach (var item in _panels)
        {
            item.CheckKeyDown();
        }
    }

    private void OnDestroy()
    {
        foreach (var item in _panels)
        {
            item.OnStop();
        }
    }

    void FindUiPanels()
    {
        _panels = GetComponentsInChildren<UiPanel>();
    }

    void CloseLastWindow()
    {
        for (int i = 0; i < _panels.Length; i++)
        {
            if (_panels[i].gameObject.activeInHierarchy)
            {
                _panels[i].gameObject.SetActive(false);
                return;
            }
        }
    }

}
