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

public class PlayerSystem : NetworkBehaviour {

    //Selected character
    public int character;
    // Base character properties : these values are changed according to the character  
    public int baseHealth = 100;
	public int baseSpeed = 100;
	public int baseDamage = 10;
    public int baseBulletSpeed = 150;
    public float baseFireRate = 0.18f;

    [SyncVar]
    int health;

    int speed;
    int damage;
	int bulletSpeed;
    float fireRate;
	float nextFire = 0;
    Vector3 spawnPoint;

    // Shooting object 
    EjectorShoot ejector;
    ParticleSystem particles;
    // Attached FirstPersonController script  
    public FirstPersonController FPController;

    // UI text  
    public Text statsText;

    /// <summary>  
    /// GameObject Start  
    /// </summary>  
    void Start()
    {
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
            //Also disabling the local mesh gameobject
            MeshRenderer[] robjs = gameObject.GetComponentsInChildren<MeshRenderer>();
            for (int i = 0; i < robjs.Length; i++)
                if (robjs[i].gameObject.name == "MeshModel")
                    robjs[i].gameObject.SetActive(false);
        }
    }

	/// <summary>  
    /// GameObject Update  
    /// </summary>  
	void Update ()
    {
        if (!isLocalPlayer)
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
        if (health <= 0)
        {
            health = baseHealth;
            RpcRespawn();
        }
    }

    /// <summary>  
    /// Get called when dead
    /// </summary>
    [ClientRpc]
    void RpcRespawn()
    {
        if (isLocalPlayer)
        {
            transform.position = spawnPoint;
        }
    }
}
