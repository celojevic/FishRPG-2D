using System;
using System.Collections.Generic;
using UnityEngine;

public class UiManager : MonoBehaviour
{

    /// <summary>
    /// Singleton instance of the UI manager.
    /// </summary>
    public static UiManager Instance;

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
    [SerializeField] private UiPanel[] _panels = null;

    private void Awake()
    {
        InitSingleton();
        FindUiPanels();
    }

    void InitSingleton()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);
    }

    void FindUiPanels()
    {
        _panels = FindObjectsOfType<UiPanel>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(_closeWindowKey))
        {
            CloseLastWindow();
        }
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
