using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

public class PlayerManager : NetworkBehaviour {

    public static int chosenGameMode;

    public GameObject[] playerPrefabs;
    public GameObject spawnPointTeamWhite;
    public GameObject spawnPointTeamBlack;

    public SyncListString teamWhite;
    public SyncListString teamBlack;

    [SyncVar]
    public int playersNumber;
    

    public void Start()
    {
    }

    public override void OnStartServer()
    {
        playersNumber = 0;
        NetworkServer.RegisterHandler(MsgType.AddPlayer, OnAddPlayerMessage);
    }

    void OnAddPlayerMessage(NetworkMessage netMsg)
    {
        playersNumber++;
        GameObject pplayer;

        //Get msg content
        AddPlayerMessage msg = netMsg.ReadMessage<AddPlayerMessage>();

        Debug.Log("GET N: " + msg.name + " c: " + msg.character);

        //Get team and write data in msg
        bool black;
        if (teamBlack.Count <= teamWhite.Count)
        {
            teamBlack.Add(msg.name);
            black = true;
            Debug.Log("ON BLACK");
        }
        else
        {
            teamWhite.Add(msg.name);
            black = false;
            Debug.Log("ON WHITE");
        }

        //Check team before spawn
        if (black)
            pplayer = Instantiate(playerPrefabs[msg.character], spawnPointTeamBlack.transform.position, spawnPointTeamBlack.transform.rotation);
        else
            pplayer = Instantiate(playerPrefabs[msg.character], spawnPointTeamWhite.transform.position, spawnPointTeamWhite.transform.rotation);

        // This spawns the new player on all clients
        NetworkServer.AddPlayerForConnection(netMsg.conn, pplayer, 0);
    }

    /*
     * Back Button action in game scene
     */
    public void OnClickBackButton()
    {
        NetworkServer.DisconnectAll();
        SceneManager.LoadScene("SelectionScene");
    }
}
