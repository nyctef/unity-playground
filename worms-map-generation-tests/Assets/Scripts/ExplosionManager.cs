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
        foreach (var target in explosionTargets)
        {
            var explosionForce = 1000;

            var r = target.GetComponent<Rigidbody>();
            if (r != null)
            {
                r.AddExplosionForce(explosionForce, explosion.WorldSpacePosition, 50, 10, ForceMode.Impulse);
            }

            var tk = target.GetComponent<CharacterMovement>();
            if (tk != null)
            {
                var dir = target.transform.position - explosion.WorldSpacePosition;
                var force = explosionForce;
                var kb = dir.normalized * force / 300;
                Debug.Log("Applying knockback " + kb + " to player");
                tk.AddKnockback(kb);
            }
        }
    }
}
