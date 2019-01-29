// ----------------------------------------------------------------------------  
// ManagerSpawner.cs  
// <summary>  
// Finalize the client connexion and call the server for a gameobject player instance when ready.
// </summary>  
// <author>Léo Pichat</author>  
// ----------------------------------------------------------------------------  

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary> Custom netMsg with player properties </summary>
public class AddPlayerMessage : MessageBase
{
    public int character;
    public string name;
}

/// <summary> Custom netMsg for Ready message sending</summary>
public class PlayerMessage : MessageBase
{
    public string name;
}

public class ManagerSpawner : MonoBehaviour {

    public static int chosenCharacter;
    public static string chosenName;

    public static NetworkClient myClient;

    /// <summary>
    /// GameObject Start
    /// </summary>
    void Start()
    {
        ClientScene.Ready(myClient.connection);
        NetworkServer.SpawnObjects();

        //Send a spawn player message to the server
        AddPlayerMessage msg = (AddPlayerMessage)CreateAddPlayerMessage();
        myClient.Send(MsgType.AddPlayer, msg);
    }

    public MessageBase CreateAddPlayerMessage()
    {
        AddPlayerMessage msg = new AddPlayerMessage
        {
            character = chosenCharacter,
            name = chosenName
        };
        return msg;
    }

    public MessageBase CreatePlayerMessage()
    {
        PlayerMessage msg = new PlayerMessage
        {
            name = chosenName
        };
        return msg;
    }
}
