using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    public Transform Target;
    public float Distance;

    void Update()
    {
        if (Target == null)
        {
            Debug.LogError("FollowPlayer no player to follow");
            return;
        }

        // just do a really simple follow for now
        transform.SetPositionAndRotation(Target.position + Vector3.back * Distance, Quaternion.identity);
    }
}
