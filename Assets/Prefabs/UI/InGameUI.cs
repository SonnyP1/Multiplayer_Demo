using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class InGameUI : MonoBehaviour
{
    [SerializeField] Button StartHostButton;
    [SerializeField] Button StartServerButton;
    [SerializeField] Button StartClientButton;
    void Start()
    {
        StartHostButton.onClick.AddListener(StartHost);
        StartServerButton.onClick.AddListener(StartServer);
        StartClientButton.onClick.AddListener(StartClient);
    }

    private void StartHost()
    {
        NetworkManager.Singleton.StartHost();
    }

    private void StartServer()
    {
        NetworkManager.Singleton.StartServer();
    }

    private void StartClient()
    {
        NetworkManager.Singleton.StartClient();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
