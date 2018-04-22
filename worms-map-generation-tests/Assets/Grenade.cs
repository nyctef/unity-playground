using UnityEngine;

public class Grenade : MonoBehaviour
{
    public float LifetimeSeconds = 3;

    private float _deathTime;

    void Start()
    {
        _deathTime = Time.time + LifetimeSeconds;
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time > _deathTime)
        {
            EventManager.Instance.TriggerEvent(new Events.Explosion(transform.position, 50));
            Destroy(gameObject);
        }
    }
}
