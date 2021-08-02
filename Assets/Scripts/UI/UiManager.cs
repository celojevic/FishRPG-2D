using System.Collections.Generic;
using UnityEngine;

public class UiManager : MonoBehaviour
{

    /// <summary>
    /// Singleton instance of the UI manager.
    /// </summary>
    public static UiManager Instance;

    public static Player Player;

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


}
