using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class EjectorShoot : MonoBehaviour {

	public GameObject bullet;

	public void Fire(float bulletspeed, int bulletdamage){
		GameObject b = Instantiate(bullet, transform.position, transform.rotation);
		b.GetComponent<Bullet>().SetDamage(bulletdamage);
		b.GetComponent<Rigidbody>().velocity = transform.TransformDirection(Vector3.forward * bulletspeed);

        //Spawn the bullet on the server
        NetworkServer.Spawn(b);
    }
}
