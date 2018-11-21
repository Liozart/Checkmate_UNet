using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameModeManager : MonoBehaviour {

    public static int chosenGameMode;
    public static int chosenCharacter;
    public GameObject[] playerPrefabs;
    public GameObject spawnPointTeam1;
    public GameObject spawnPointTeam2;

    private void Start()
    {
        GameObject.Instantiate(playerPrefabs[chosenCharacter], spawnPointTeam1.transform.position, spawnPointTeam1.transform.rotation);
    }

    /*
     * Back Button action in game scene
     * */
    public void OnClickBackButton()
    {
        SceneManager.LoadScene("SelectionScene");
    }
}
