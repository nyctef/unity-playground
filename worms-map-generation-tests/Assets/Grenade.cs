using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grenade : MonoBehaviour
{
    public float LifetimeSeconds = 3;

    private float deathTime;

    void Start()
    {
        deathTime = Time.time + LifetimeSeconds;
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time > deathTime)
        {
            EventManager.Instance.TriggerEvent(new Events.Explosion(transform.position, 50));
            Destroy(gameObject);
        }
    }
}
