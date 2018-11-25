using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class AddPlayerMessage : MessageBase
{
    public int character;
    public string name;
}

public class ManagerSpawner : MonoBehaviour {

    public static int chosenCharacter;
    public static string chosenName;

    public static NetworkClient myClient;

    // Use this for initialization
    void Start()
    {
        ClientScene.Ready(myClient.connection);
        NetworkServer.SpawnObjects();

        //Send a spawn player message to the server
        AddPlayerMessage msg = new AddPlayerMessage
        {
            character = chosenCharacter,
            name = chosenName
        };
        myClient.Send(MsgType.AddPlayer, msg);
    }
}
