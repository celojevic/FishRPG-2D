﻿using FishNet.Managing;
using UnityEngine;
using UnityEngine.UI;

public class NetworkHudCanvases : MonoBehaviour
{
    private NetworkManager _networkManager;
    public GameObject ServerButton;
    public GameObject ClientButton;

    public bool AutoStart = true;

    private void Start()
    {
        _networkManager = FindObjectOfType<NetworkManager>();
        if (AutoStart)
        {
            OnClick_Server();
            OnClick_Client();
        }
    }
    private void Update()
    {
        Text t;
        
        t= ServerButton.GetComponentInChildren<Text>();
        if (_networkManager.ServerManager.Active)
            t.text = "Stop Server";
        else
            t.text = "Start Server";

        t = ClientButton.GetComponentInChildren<Text>();
        if (_networkManager.ClientManager.Active)
            t.text = "Stop Client";
        else
            t.text = "Start Client";
    }

    public void OnClick_Server()
    {
        if (_networkManager.ServerManager.Active)
            _networkManager.TransportManager.Transport.StopConnection(true);
        else
            _networkManager.TransportManager.Transport.StartConnection(true);
    }


    public void OnClick_Client()
    {
        if (_networkManager.ClientManager.Active)
            _networkManager.TransportManager.Transport.StopConnection(false);
        else
            _networkManager.TransportManager.Transport.StartConnection(false);
    }
}
