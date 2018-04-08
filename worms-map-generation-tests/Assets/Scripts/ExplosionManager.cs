using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionManager : MonoBehaviour
{
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
        // TODO spawn an explosion sprite and some particles
    }
}
