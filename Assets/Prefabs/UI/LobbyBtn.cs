using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyBtn : MonoBehaviour
{
    string lobbyId;
    private void Start()
    {
        Button btn = GetComponent<Button>();
        if(btn != null)
        {
            btn.onClick.AddListener(JoinLobby);
        }
    }

    private void JoinLobby()
    {
        FindObjectOfType<LobbyManager>().JoinLobby(lobbyId);
    }

    public void Init(string id,string name)
    {
        lobbyId = id;
        TextMeshProUGUI Text = GetComponentInChildren<TextMeshProUGUI>();
        if(Text!= null)
        {
            Text.text = name;
        }
    }
}
