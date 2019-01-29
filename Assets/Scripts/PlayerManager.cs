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
    public static short OnPlayerReadyMessageCode = 1000;
    public static short OnPlayerDeadMessageCode = 1001;

    //0 : black king, 1 : black queen, 2 : black pawn
    //3 : white king, 4 : white queen, 5 : white pawn
    public GameObject[] playerPrefabs;

    //Respawn points position
    public GameObject spawnPointTeamWhite;
    public GameObject spawnPointTeamBlack;

    //Synced lists of pseudos by team
    public SyncListString teamWhite;
    public SyncListString teamBlack;

    //Spawned players gameobjects
    public List<GameObject> playersList;

    //Number of in-game players
    [SyncVar]
    public int playersNumber;

    //Number of players waiting for the next round
    public int playersReady;

    //Number of alive and playing players
    public int teamWhiteAlive;
    public int teamBlackAlive;

    //Scores
    [SyncVar]
    public int scoresWhite;
    [SyncVar]
    public int scoresBlack;

    //State of the round
    public PlayState roundState;


    /// <summary>  
	/// GameObject Start on server side
	/// </summary>  
    public override void OnStartServer()
    {
        scoresWhite = 0;
        scoresBlack = 0;

        playersNumber = 0;
        playersList = new List<GameObject>();
        teamWhiteAlive = 0;
        teamBlackAlive = 0;

        //Wait for player to join
        roundState = PlayState.Waiting;

        //Add handlers for messages
        NetworkServer.RegisterHandler(MsgType.AddPlayer, OnAddPlayerMessage);
        NetworkServer.RegisterHandler(MsgType.Disconnect, OnDisconnectMessage);
        NetworkServer.RegisterHandler(OnPlayerReadyMessageCode, OnPlayerReadyMessage);
        NetworkServer.RegisterHandler(OnPlayerDeadMessageCode, OnPlayerDeadMessage);

        //Additionnal game mode setups
        /*switch (chosenGameMode)
        {
            //Deathmatch
            case 0:
                break;
        }*/
    }

    /// <summary>  
	/// GameObject Update
	/// </summary>  
    /*void Update()
    {
        if (!isServer)
            return;

        //start the round if theres enough players
        if (roundState == PlayState.Waiting)
            if (playersReady >= 2)
                StartNewRound();
    }*/

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
        playersList.Add(pplayer);
    }

    /// <summary>  
	/// Callback for PlayerReady message reception
	/// </summary>  
    void OnPlayerReadyMessage(NetworkMessage netMsg)
    {
        playersReady++;

        //Start the first round
        if (roundState == PlayState.Waiting)
            if (playersReady >= 2)
                StartNewRound();
    }

    /// <summary>  
	/// Callback for PlayerDead message reception
	/// </summary>  
    void OnPlayerDeadMessage(NetworkMessage netMsg)
    {
        PlayerMessage msg = netMsg.ReadMessage<PlayerMessage>();

        if (teamWhite.Contains(msg.name))
        {
            teamWhiteAlive--;
            Debug.Log(msg.name + " added to white deads");
        }
        else
        {
            teamBlackAlive--;
            Debug.Log(msg.name + " added to black deads");
        }

        //Additionnal gamemode checks
        switch (chosenGameMode)
        {
            //Deathmatch
            //Check if a team is dead, send to players the winner
            //and wait 5 seconds before starting a new round
            case 0:
                if (teamWhiteAlive == 0)
                {
                    scoresBlack++;
                    foreach (GameObject p in playersList)
                        p.GetComponent<PlayerSystem>().RpcWinBlack(GetScores());
                    roundState = PlayState.Waiting;
                    StartCoroutine(WaitForNextRound());
                }
                else if (teamBlackAlive == 0)
                {
                    scoresWhite++;
                    foreach (GameObject p in playersList)
                        p.GetComponent<PlayerSystem>().RpcWinWhite(GetScores());
                    roundState = PlayState.Waiting;
                    StartCoroutine(WaitForNextRound());
                }
                break;
        }
    }

    /// <summary>  
	/// Start a new round
	/// </summary> 
    void StartNewRound()
    {
        roundState = PlayState.Playing;
        teamWhiteAlive = teamWhite.Count;
        teamBlackAlive = teamBlack.Count;

        //Call ready functions of every players
        foreach (GameObject p in playersList)
        {
            p.GetComponent<PlayerSystem>().RpcRespawn();
            p.GetComponent<PlayerSystem>().RpcPlay();
        }
    }

    /// <summary>  
	/// Returns the current scores in an array 0->white team, 1->black team
	/// </summary> 
    public int[] GetScores()
    {
        int[] i = new int[] {scoresWhite, scoresBlack};
        return i;
    }

    IEnumerator WaitForNextRound()
    {
        yield return new WaitForSeconds(3.0f);
        StartNewRound();
    }
}

