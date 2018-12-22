// ----------------------------------------------------------------------------  
// GameSetup.cs  
// <summary>  
// Manage the different Canvas in NetworkScene, handle the server creation/connexion
// </summary>  
// <author>Léo Pichat</author>  
// ----------------------------------------------------------------------------  

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

public class GameSetup : MonoBehaviour {

    public Canvas networkCanvas;
    public Canvas modeCanvas;
    public Canvas characterCanvas;
    public Text nameText;
    public Text adressText;
    
    //Spawnable prefabs : Players, bullets (then later flags, etc)
    public GameObject[] registerPrefabs;
    
    NetworkClient myClient;

    /// <summary>  
	/// GameObject Start
	/// </summary>  
    void Start () {
        //we have to register all prefabs that will be spawned during runtime
        foreach(GameObject obj in registerPrefabs)
            ClientScene.RegisterPrefab(obj);
        myClient = new NetworkClient();
        adressText.text = "127.0.0.1";

    }

    /// <summary>  
	/// "Rejoindre" button function, connect the client to the address' server
	/// </summary>  
    public void SetupClient()
    {
        //Callback register
        myClient.RegisterHandler(MsgType.Connect, OnConnected);
        myClient.Connect(adressText.text, 7777);
        //Players joining don't choose the game mode
        DisplaySelection(false);
    }

    /// <summary>  
	/// "Créer partie" button function, create a server and a local client connexion
	/// </summary>  
    public void SetupServerAndClient()
    {
        NetworkServer.Listen(7777);
        myClient.RegisterHandler(MsgType.Connect, OnConnected);
        myClient = ClientScene.ConnectLocalServer();
        //Player creating server choose the game mode
        DisplaySelection(true);
    }

    /// <summary>  
	/// Callback for succeded connexion
	/// </summary>
    public void OnConnected(NetworkMessage netMsg)
    {
        Debug.Log("Connected to server");
    }

    /// <summary> Display the selection Canvas if host else directly the character Canvas </summary>  
	/// <param name="serv">true if player is the host</param>
    void DisplaySelection(bool serv)
    {
        ManagerSpawner.myClient = myClient;
        networkCanvas.gameObject.SetActive(false);
        if (serv)
        {
            modeCanvas.gameObject.SetActive(true);
        }
        else
            characterCanvas.gameObject.SetActive(true);
    }

    /// <summary> Set the chosen gamemode and enable next canvas </summary>  
	/// <param name="m">picked gamemode</param>
    public void SetMode(int m)
    {
        PlayerManager.chosenGameMode = m;
        modeCanvas.gameObject.SetActive(false);
        characterCanvas.gameObject.SetActive(true);
    }

    /// <summary> Set the chosen character and load the scene </summary>  
	/// <param name="c">picked character</param>
    public void SetCharacter(int c)
    {
        ManagerSpawner.chosenCharacter = c;
        ManagerSpawner.chosenName = nameText.text;
        SceneManager.LoadScene("GameScene");
    }
}
