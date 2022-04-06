using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class InGameUI : MonoBehaviour
{
    [SerializeField] Button StartHostButton;
    [SerializeField] Button StartServerButton;
    [SerializeField] Button StartClientButton;
    [SerializeField] Button RefreshButton;
    [SerializeField] LobbyBtn LobbyButtonPrefab;
    [SerializeField] GameObject LobbyPanel;

    private List<GameObject> currentLobbyList = new List<GameObject>();
    void Start()
    {
        StartHostButton.onClick.AddListener(StartHost);
        StartServerButton.onClick.AddListener(StartServer);
        StartClientButton.onClick.AddListener(StartClient);
        RefreshButton.onClick.AddListener(RefreshLobbyBtnClick);
    }
    public void RefreshLobbyBtnClick()
    {
        RefreshLobbyList();
    }
    private async Task RefreshLobbyList()
    {
        foreach(GameObject previousLobbyButton in currentLobbyList)
        {
            Destroy(previousLobbyButton);
        }

        Task<QueryResponse> task = FindObjectOfType<LobbyManager>().QueryLobbies();
        QueryResponse response = await task;
        foreach(Lobby lobby in response.Results)
        {
            LobbyBtn button = Instantiate(LobbyButtonPrefab,LobbyPanel.transform);
            button.Init(lobby.Id, lobby.Name);
            currentLobbyList.Add(button.gameObject);
        }
    }

    private void StartHost()
    {
        FindObjectOfType<LobbyManager>().HostLobby("Beep Boop Lobby");
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
