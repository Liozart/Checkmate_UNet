using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameSetup : MonoBehaviour {

    public Canvas modeCanvas;
    public Canvas characterCanvas;
    public Text hint;

    public int mode;
    public int character;

	// Use this for initialization
	void Start () {
        characterCanvas.gameObject.SetActive(false);
	}

    public void SetMode(int m)
    {
        mode = m;
        modeCanvas.gameObject.SetActive(false);
        characterCanvas.gameObject.SetActive(true);
        hint.text = "Sélection de la classe de jeu";
    }

    public void SetCharacter(int c)
    {
        character = c;
        SceneManager.LoadScene("GameScene");
    }
}
