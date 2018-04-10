using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionManager : MonoBehaviour
{
    public GameObject Explosion50Prefab;

    void OnEnable()
    {
        EventManager.Instance.StartListening<Events.Explosion>(OnExplosion);
    }

    void OnDisable()
    {
        //EventManager.Instance.StopListening<Events.Explosion>(OnExplosion);
    }

    private void OnExplosion(Events.Explosion explosion)
    {
        // TODO add some particles and other effects
        // TODO cope with different explosion sizes
        Instantiate(Explosion50Prefab, explosion.worldSpacePosition, Quaternion.identity);
    }
}
