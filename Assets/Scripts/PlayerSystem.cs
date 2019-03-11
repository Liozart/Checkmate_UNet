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

    // Shooting object s
    EjectorShoot ejector;
    ParticleSystem particlesFire;
    ParticleSystem particlesAbilityTargeted;
    // Attached FirstPersonController script  
    public FirstPersonController FPController;
    //Top camera for waiting players
    public GameObject waitCamera;

    //Cooldown bool to limit ability use
    bool canUseAbility;

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
        //Particles when firing
        particlesFire = transform.GetChild(0).GetComponentInChildren<ParticleSystem>();
        particlesFire.Stop();
        //Particles when targeted by ability
        particlesAbilityTargeted = gameObject.GetComponent<ParticleSystem>();
        particlesAbilityTargeted.Stop();

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

            canUseAbility = true;
        }

    }

    /// <summary>  
    /// GameObject Update  
    /// </summary>  
    void Update()
    {
        if (!isLocalPlayer)
            return;

        //No inputs if waiting
        if (playState == PlayState.Waiting)
            return;

        // Get Mouse1 input  
        if (Input.GetButton("Fire1"))
        {
            //Check if player can shoot already  
            if (Time.time > nextFire) {
                nextFire = Time.time + fireRate;
                particlesFire.Play();
                CmdFire();
            }
        }

        // Get special ability input
        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            if (canUseAbility)
            {
                //Raycast radius is large so players don't have to be exactly precise
                float castingRadius = 1.5f;
                float castingDistance = 20f;
                RaycastHit hit;
                Vector3 ray = transform.position + gameObject.GetComponent<CharacterController>().center;

                //if ability hit someone
                if (Physics.SphereCast(ray, castingRadius, transform.forward, out hit, castingDistance))
                {
                    //Tell server to apply effect on player
                    CmdApplyAbilityEffect(gameObject, hit.transform.gameObject);
                    //start cooldown
                    StartCoroutine(AbilityCooldown());
                    canUseAbility = false;
                }
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
        if (canUseAbility)
            statsText.text += "\nAbility Available";
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
            RpcSendDead();
        }
    }

    /// <summary>  
    /// Get called when the server got the player dead's message
    /// </summary>  
    [ClientRpc]
    public void RpcSendDead()
    {
        if (isLocalPlayer)
        {
            playState = PlayState.Waiting;
            SwitchPlayState(playState);
            //Send dead message to server
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
            health = baseHealth;
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
            scorewhite = win[0];
            scoreblack = win[1];
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
            scorewhite = win[0];
            scoreblack = win[1];
            scoresCanvas.text = "White team : " + win[0] + "\nBlack team : " + win[1];
            //Display black team win Canvas
            winBlackText.gameObject.SetActive(true);
            //Wait until next round
            playState = PlayState.Waiting;
            SwitchPlayState(playState);
        }
    }

    /// <summary>  
    /// Call the server to target player with ability
    /// </summary>  
    [Command]
    public void CmdApplyAbilityEffect(GameObject sender, GameObject target)
    {
        //Ability effect is different if target is ally or enemy
        if (GameObject.Find("Player Manager").GetComponent<PlayerManager>().areInSameTeam(sender, target))
            target.GetComponent<PlayerSystem>().RpcGetAbilityEffect_Healing();
        else
            target.GetComponent<PlayerSystem>().RpcGetAbilityEffect_Stopping();
    }

    /// <summary>  
    /// Get called by the server when the player is targeted by a friendly player ability
    /// </summary>  
    [ClientRpc]
    public void RpcGetAbilityEffect_Healing()
    {
        health = baseHealth;
        particlesAbilityTargeted.startColor = Color.green;
        particlesAbilityTargeted.Play();
    }

    /// <summary>  
    /// Get called by the server when the player is targeted by a enemy player ability
    /// </summary>  
    [ClientRpc]
    public void RpcGetAbilityEffect_Stopping()
    {
        speed = 20;
        FPController.ChangeCharacterSpeedTemporary(speed);
        particlesAbilityTargeted.startColor = Color.red;
        particlesAbilityTargeted.Play();
        StartCoroutine(RechangeSpeed());
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

    IEnumerator AbilityCooldown()
    {
        yield return new WaitForSeconds(3.0f);
        canUseAbility = true;
    }

    IEnumerator RechangeSpeed()
    {
        yield return new WaitForSeconds(4.0f);
        speed = baseSpeed;
    }
}
