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
    
    public GameObject[] registerPrefabs;
    
    NetworkClient myClient;

    void Start () {
        foreach(GameObject obj in registerPrefabs)
            ClientScene.RegisterPrefab(obj);
        myClient = new NetworkClient();
        adressText.text = "127.0.0.1";

    }

    // Create a client and connect to the server port
    public void SetupClient()
    {
        myClient.RegisterHandler(MsgType.Connect, OnConnected);
        myClient.Connect(adressText.text, 7777);
        DisplaySelection(false);
    }

    // Create a a server and local client and connect to the local server
    public void SetupServerAndClient()
    {
        NetworkServer.Listen(7777);
        myClient = ClientScene.ConnectLocalServer();
        myClient.RegisterHandler(MsgType.Connect, OnConnected);
        DisplaySelection(true);
    }

    public void OnConnected(NetworkMessage netMsg)
    {
        Debug.Log("Connected to server");
    }

    //Display mode selection if host
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

    //When gamemode is set, change menu
    public void SetMode(int m)
    {
        PlayerManager.chosenGameMode = m;
        modeCanvas.gameObject.SetActive(false);
        characterCanvas.gameObject.SetActive(true);
    }

    //Then load the gamescene
    public void SetCharacter(int c)
    {
        ManagerSpawner.chosenCharacter = c;
        ManagerSpawner.chosenName = nameText.text;
        SceneManager.LoadScene("GameScene");
    }
}
