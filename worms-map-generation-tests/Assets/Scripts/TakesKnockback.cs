using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


// based on https://answers.unity.com/questions/242648/force-on-character-controller-knockback.html

// todo probably need to move this into the main player movement script so that we can stop the knockback effect at the right points

[RequireComponent(typeof(CharacterController))]
public class TakesKnockback : MonoBehaviour
{
    public float CharacterMass = 1.0f;
    public Vector3 Impact = Vector3.zero;

    private CharacterController _character;
 
    void Start()
    {
        _character = gameObject.RequireComponent<CharacterController>();
    }

    public void AddKnockback(Vector3 direction)
    {
        if (direction.y < 0) direction.y = -direction.y; // reflect down force on the ground
        Impact += direction / CharacterMass;
    }

    void Update()
    {
        // apply the impact force:
        if (Impact.magnitude > 0.2) _character.Move(Impact * Time.deltaTime);
        // consumes the impact energy each cycle:
        Impact = Vector3.Lerp(Impact, Vector3.zero, 5 * Time.deltaTime);
    }
}
