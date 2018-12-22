// ----------------------------------------------------------------------------  
// PlayerManager.cs  
// <summary>  
// Executed by the server, it instantiate new players on the network and add them to a team.
// </summary>  
// <author>Léo Pichat</author>  
// ----------------------------------------------------------------------------  

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

public class PlayerManager : NetworkBehaviour
{
    //Host selected gamemode
    public static int chosenGameMode;

    public static short RespawnMessageCode = 1000;

    //0 : black king, 1 : black queen, 2 : black pawn
    //3 : white king, 4 : white queen, 5 : white pawn
    public GameObject[] playerPrefabs;

    //Respawn points position
    public GameObject spawnPointTeamWhite;
    public GameObject spawnPointTeamBlack;

    //Synced teams lists
    public SyncListString teamWhite;
    public SyncListString teamBlack;

    [SyncVar]
    public int playersNumber;

    /// <summary>  
	/// GameObject Start on server side
	/// </summary>  
    public override void OnStartServer()
    {
        playersNumber = 0;
        //Listen for AddPlayer messages from players
        NetworkServer.RegisterHandler(MsgType.AddPlayer, OnAddPlayerMessage);
        NetworkServer.RegisterHandler(RespawnMessageCode, OnRespawnMessage);
        NetworkServer.RegisterHandler(MsgType.Disconnect, OnDisconnectMessage);
    }

    void OnDisconnectMessage(NetworkMessage netMsg)
    {
        Debug.Log("Destroy disconnected player");
        NetworkServer.DestroyPlayersForConnection(netMsg.conn);
    }

    /// <summary>  
	/// Callback for AddPlayer message reception
	/// </summary>  
    void OnAddPlayerMessage(NetworkMessage netMsg)
    {
        playersNumber++;
        GameObject pplayer;

        //Get msg content
        AddPlayerMessage msg = netMsg.ReadMessage<AddPlayerMessage>();

        //Check which team has less players and add the player in
        bool black;
        if (teamBlack.Count <= teamWhite.Count)
        {
            teamBlack.Add(msg.name);
            black = true;
        }
        else
        {
            teamWhite.Add(msg.name);
            black = false;
        }

        //Spawn the GameObject in its team's spawn point 
        if (black)
            pplayer = Instantiate(playerPrefabs[msg.character], spawnPointTeamBlack.transform.position, spawnPointTeamBlack.transform.rotation);
        else
            pplayer = Instantiate(playerPrefabs[(3 + msg.character)], spawnPointTeamWhite.transform.position, spawnPointTeamWhite.transform.rotation);

        // This spawns the new player on all clients
        NetworkServer.AddPlayerForConnection(netMsg.conn, pplayer, 0);
    }

    void OnRespawnMessage(NetworkMessage netMsg)
    {
        RespawnMessage msg = netMsg.ReadMessage<RespawnMessage>();
        Debug.Log("respawned : " + msg.name);
    }
}
