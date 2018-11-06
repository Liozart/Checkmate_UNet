using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.Characters.FirstPerson;

public class PlayerSystem : MonoBehaviour {

	public int baseHealth = 100;
	public int baseSpeed = 100;
	public int baseDamage = 10;
    public int baseBulletSpeed = 150;
    public float baseFireRate = 0.18f;

    int health;
    int speed;
    int damage;
	int bulletSpeed;
    float fireRate;

	float nextFire = 0;

	EjectorShoot ejector;
    ParticleSystem particles;
    FirstPersonController FPController;


    public Text healthText;

	// Use this for initialization
	void Start () {
        health = baseHealth;
        speed = baseSpeed;
        damage = baseDamage;
        bulletSpeed = baseBulletSpeed;
        fireRate = baseFireRate;

        ejector = gameObject.GetComponentInChildren<EjectorShoot>();
        particles = gameObject.GetComponentInChildren<ParticleSystem>();
        FPController = gameObject.GetComponent<FirstPersonController>();

    }
	
	// Update is called once per frame
	void Update () {
		//Shoot
		if (Input.GetButton("Fire1")) {
			if (Time.time > nextFire) {
				nextFire = Time.time + fireRate;
				Fire();
			}
		}

        //UI
        healthText.text = "Health | " + health;
	}

	void Fire()
    {
        particles.Play();
        ejector.Fire(bulletSpeed, damage);
	}

    public void Damage(int dam)
    {
        health -= dam;
        if (health <= 0)
        {
            Debug.Log("Dead");
        }
    }
}
