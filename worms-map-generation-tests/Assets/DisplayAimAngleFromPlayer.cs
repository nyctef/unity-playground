using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisplayAimAngleFromPlayer : MonoBehaviour
{
    // TODO: should this be a direct reference?
    public CharacterMovement CharacterWithAim;

    // Update is called once per frame
    void Update()
    {
        var angle = CharacterWithAim.AimAngle;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
}
