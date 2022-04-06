using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using Unity.Services.Lobbies.Models;
using System;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] UnityTransport transport;

    private async Task Start()
    {
        Debug.Log("Start hosting, initialing unity gameing services");
        await UnityServices.InitializeAsync();
        Debug.Log("Services are initialized");
        //sign in with unity
        Debug.Log("Start logging in");
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        Debug.Log("Logged In");
    }
    public async Task HostLobby(string lobbyName)
    {

        //start a Relay server on your local machine
        Allocation allocation = await Relay.Instance.CreateAllocationAsync(2);
        //a join code is needed to join a relay server (a relay server is a server created on a local machine)
        string joinCode = await Relay.Instance.GetJoinCodeAsync(allocation.AllocationId);
        Debug.Log($"Relay Server establish with join code: {joinCode}");

        //kick start the lobby
        CreateLobbyOptions lobbyOptions = new CreateLobbyOptions();
        lobbyOptions.Data = new Dictionary<string, DataObject>
        {
            { "joinCode",new DataObject(DataObject.VisibilityOptions.Member,joinCode)}
        };
        lobbyOptions.IsPrivate = false;
        Lobby lobby = await Lobbies.Instance.CreateLobbyAsync(lobbyName,2,lobbyOptions);
        Debug.Log($"Lobby created with name {lobby.Name}, and id: {lobby.Id}");
        StartCoroutine(PingLobby(lobby.Id));
        //populate transport information
        transport.SetRelayServerData(allocation.RelayServer.IpV4,(ushort)allocation.RelayServer.Port,allocation.AllocationIdBytes,allocation.Key,allocation.ConnectionData);

        //start host
        NetworkManager.Singleton.StartHost();
    }

    internal async Task JoinLobby(string lobbyId)
    {

        JoinLobbyByIdOptions options = new JoinLobbyByIdOptions();
        Lobby lobbyJoined = await Lobbies.Instance.JoinLobbyByIdAsync(lobbyId,options);
        string joinCode = lobbyJoined.Data["joinCode"].Value;
        Debug.Log($"Lobby {lobbyJoined.Name} Joined Succesfully with code : {joinCode}");

        JoinAllocation joinAllocation =  await Relay.Instance.JoinAllocationAsync(joinCode);
        Debug.Log($"Relay server join successfully at {joinAllocation.RelayServer.IpV4}");

        transport.SetRelayServerData(joinAllocation.RelayServer.IpV4, (ushort)joinAllocation.RelayServer.Port, joinAllocation.AllocationIdBytes, joinAllocation.Key, joinAllocation.ConnectionData, joinAllocation.HostConnectionData);

        //start client
        NetworkManager.Singleton.StartClient();
    }

    IEnumerator PingLobby(string lobbyId)
    {
        while(true)
        {
            Lobbies.Instance.SendHeartbeatPingAsync(lobbyId);
            Debug.Log("pinging lobby");
            yield return new WaitForSeconds(10f);
        }
    }

    public Task<QueryResponse> QueryLobbies()
    {
        QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions();
        Task<QueryResponse> lobbies = Lobbies.Instance.QueryLobbiesAsync(queryLobbiesOptions);
        return lobbies;
    }
}
