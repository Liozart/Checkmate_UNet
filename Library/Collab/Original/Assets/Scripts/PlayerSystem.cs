// ----------------------------------------------------------------------------  
// PlayerSystem.cs  
// <summary>  
// Attached to a player prefab, it manages its properties, UI, movement and shooting input  
// </summary>  
// <author>Léo Pichat</author>  
// ----------------------------------------------------------------------------  

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityStandardAssets.Characters.FirstPerson;

//Play state : Waiting for the next round to begin, or playing
public enum PlayState
{
    Waiting, Playing
}

public class PlayerSystem : NetworkBehaviour {

    //Selected character
    public int character;
    // Base character properties : these values are changed according
    // to the chosen character, then aren't modified
    public int baseHealth = 100;
	public int baseSpeed = 100;
	public int baseDamage = 10; 
    public int baseBulletSpeed = 150;
    public float baseFireRate = 0.18f;

    //Game runtime properties : these values can be modified during gameplay
    [SyncVar]
    int health;

    int speed;
    int damage;
	int bulletSpeed;
    float fireRate;
	float nextFire = 0;
    Vector3 spawnPoint;

    //State of the current player
    public PlayState playState;

    // Shooting object 
    EjectorShoot ejector;
    ParticleSystem particles;
    // Attached FirstPersonController script  
    public FirstPersonController FPController;
    //Top camera for waiting players
    public GameObject waitCamera;

    // UI text
    //Stats
    public Text statsText;
    //Win labels
    public Text winWhiteText;
    public Text winBlackText;
    //Score
    public Text scoresCanvas;
    int scorewhite;
    int scoreblack;

    //Pre-made PlayerMessage with name
    PlayerMessage playerNetMsg;

    /// <summary>  
    /// GameObject Start  
    /// </summary>  
    void Start()
    {
        //Waiting at first
        playState = PlayState.Waiting;
        scorewhite = 0;
        scoreblack = 0;

        //The spawn point is where the player appears
        spawnPoint = gameObject.transform.position;

        // Get the chosen character and change the base values
        character = ManagerSpawner.chosenCharacter;
        InitChosenCharacter();

        ejector = gameObject.GetComponentInChildren<EjectorShoot>();
        particles = gameObject.GetComponentInChildren<ParticleSystem>();
        particles.Stop();


        if (!isLocalPlayer)
        {
            gameObject.GetComponent<FirstPersonController>().enabled = false;
            gameObject.GetComponentInChildren<Camera>().enabled = false;
            gameObject.GetComponentInChildren<AudioListener>().enabled = false;
            gameObject.GetComponentInChildren<FlareLayer>().enabled = false;
            gameObject.GetComponentInChildren<Canvas>().gameObject.SetActive(false);
        }
        else
        {
            //Also disabling the local mesh gameobject to prevent the camera's view to be obstructed
            MeshRenderer[] robjs = gameObject.GetComponentsInChildren<MeshRenderer>();
            for (int i = 0; i < robjs.Length; i++)
                if (robjs[i].gameObject.name == "MeshModel")
                    robjs[i].gameObject.SetActive(false);

            //Set wait camera
			waitCamera = GameObject.FindGameObjectWithTag("WaitCamera");
            SwitchPlayState(playState);

            //Finally send ready message to server
            playerNetMsg = (PlayerMessage)GameObject.Find("Manager Spawner").GetComponent<ManagerSpawner>().CreatePlayerMessage();
            ManagerSpawner.myClient.Send(PlayerManager.OnPlayerReadyMessageCode, playerNetMsg);
        }

    }

	/// <summary>  
    /// GameObject Update  
    /// </summary>  
	void Update ()
    {
        if (!isLocalPlayer)
            return;

        //No inputs if waiting
        if (playState == PlayState.Waiting)
            return;

        // Get Mouse1 input  
        if (Input.GetButton("Fire1")) {
            //Check if player can shoot already  
            if (Time.time > nextFire) {
				nextFire = Time.time + fireRate;
                particles.Play();
                CmdFire();
			}
		}

        //Display scoreboard
        if (Input.GetKeyDown(KeyCode.Tab))
            scoresCanvas.gameObject.SetActive(true);
        if (Input.GetKeyUp(KeyCode.Tab))
            scoresCanvas.gameObject.SetActive(false);

        UpdateUI();
    }

    /// <summary>  
    /// Change the base player properties for the selected character 
    /// </summary>  
    void InitChosenCharacter()
    {
        switch (character)
        {
            // King
            case 0:
                baseHealth = 200;
                baseSpeed = 80;
                baseDamage = 20;
                baseBulletSpeed = 40;
                break;
            // Queen
            case 1:
                baseHealth = 110;
                baseSpeed = 110;
                baseBulletSpeed = 200;
                break;
            // Pawn
            case 2:
                baseHealth = 80;
                baseSpeed = 140;
                baseDamage = 5;
                baseFireRate = 0.1f;
                break;
        }

        health = baseHealth;
        speed = baseSpeed;
        damage = baseDamage;
        bulletSpeed = baseBulletSpeed;
        fireRate = baseFireRate;

        // The caracter speed is set in the FPController script  
        FPController.SetCharacterSpeed(speed);
    }

    /// <summary>  
    /// Set the text UI with player properties  
    /// </summary>  
    void UpdateUI()
    {
        statsText.text = "| " + health + "\n| " + speed + "\n| " + damage +
                        "\n| " + fireRate + "\n| " + bulletSpeed;
    }

    /// <summary>  
    /// Call the ejector's fire function and particles  
    /// </summary>  
    [Command]
    void CmdFire()
    {
        ejector.Fire(bulletSpeed, damage);
    }

    /// <summary>  
    /// Get called by a bullet when hit  
    /// </summary>
    public void Damage(int dam)
    {
        if (!isServer)
            return;

        health -= dam;
        //Check if player is dead
        if (health <= 0)
        {
            health = baseHealth;
            RpcSendDead();
        }
    }

    [ClientRpc]
    public void RpcSendDead()
    {
        if (isLocalPlayer)
        {
            playState = PlayState.Waiting;
            SwitchPlayState(playState);
            //Send dead message to server
            Debug.Log("ded : " + playerNetMsg.name);
            ManagerSpawner.myClient.Send(PlayerManager.OnPlayerDeadMessageCode, playerNetMsg);

        }
    }

    /// <summary>  
    /// Get called when dead
    /// </summary>
    [ClientRpc]
    public void RpcRespawn()
    {
        if (isLocalPlayer)
        {
            transform.position = spawnPoint;
        }
        //The server resets life
        else
            health = baseHealth;
    }

    /// <summary>  
    /// Get called when a new round start
    /// </summary>
    [ClientRpc]
    public void RpcPlay()
    {
        if (isLocalPlayer)
        {
            //Hide win text
            winWhiteText.gameObject.SetActive(false);
            winBlackText.gameObject.SetActive(false);
            playState = PlayState.Playing;
            SwitchPlayState(playState);
        }
    }

    /// <summary>  
    /// Get when white team wins
    /// </summary>
    [ClientRpc]
    public void RpcWinWhite(int[] win)
    {
        if (isLocalPlayer)
        {
            //Update scoreboard
            scoresCanvas.text = "White team : " + win[0] + "\nBlack team : " + win[1];
            //Display white team win Canvas
            winWhiteText.gameObject.SetActive(true);
            //Wait until next round
            playState = PlayState.Waiting;
            SwitchPlayState(playState);
        }
    }

    /// <summary>  
    /// Get called when black team wins<
    /// </summary>
    [ClientRpc]
    public void RpcWinBlack(int[] win)
    {
        if (isLocalPlayer)
        {
            //Update scoreboard
            scoresCanvas.text = "White team : " + win[0] + "\nBlack team : " + win[1];
            //Display black team win Canvas
            winBlackText.gameObject.SetActive(true);
            //Wait until next round
            playState = PlayState.Waiting;
            SwitchPlayState(playState);
        }
    }

    /// <summary>  
    /// Set the properties of the player for it's current state
    /// <param name="state"/>The state to switch to</param>
    /// </summary>
    void SwitchPlayState(PlayState state)
    {
        if (isLocalPlayer)
        {
            //Player is waiting for round
            if (state == PlayState.Waiting)
            {
                //Enable spectator camera and disable inputs
                waitCamera.SetActive(true);
                gameObject.GetComponent<FirstPersonController>().enabled = false;
                gameObject.GetComponentInChildren<Camera>().enabled = false;
                gameObject.GetComponentInChildren<AudioListener>().enabled = false;
            }
            else
            {
                //Enable player object camera and enable inputs
                waitCamera.SetActive(false);
                gameObject.GetComponent<FirstPersonController>().enabled = true;
                gameObject.GetComponentInChildren<Camera>().enabled = true;
                gameObject.GetComponentInChildren<AudioListener>().enabled = true;
            }
        }
    }
}

