using System.Linq;
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
        Instantiate(Explosion50Prefab, explosion.WorldSpacePosition, Quaternion.identity);

        var explosionTargets = Physics.OverlapSphere(explosion.WorldSpacePosition, 50);
        Debug.Log("Explosion affecting "+explosionTargets.Length+ " targets");
        foreach (var h in explosionTargets)
        {
            var r = h.GetComponent<Rigidbody>();
            if (r != null)
            {
                r.AddExplosionForce(1000, explosion.WorldSpacePosition, 50, 10, ForceMode.Impulse);
            }

            var cc = h.GetComponent<CharacterController>();
            if (cc != null)
            {
                ; // todo move character
            }
        }
    }
}
