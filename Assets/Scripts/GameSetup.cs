using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameSetup : MonoBehaviour {

    public Canvas modeCanvas;
    public Canvas characterCanvas;
    public Text hint;
    
    public int character;

	// 1. gamemode selection
    // 2. character selection
	void Start () {
        characterCanvas.gameObject.SetActive(false);
        
	}

    //When gamemode is set, change menu
    public void SetMode(int m)
    {
        GameModeManager.chosenGameMode = m;
        modeCanvas.gameObject.SetActive(false);
        characterCanvas.gameObject.SetActive(true);
        hint.text = "Sélection de la classe de jeu";
    }

    //Then load the gamescene
    public void SetCharacter(int c)
    {
        GameModeManager.chosenCharacter = c;
        SceneManager.LoadScene("GameScene");
    }
}
