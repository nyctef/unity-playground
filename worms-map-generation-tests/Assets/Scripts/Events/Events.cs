using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Events
{
    public struct Explosion
    {
        public Vector3 worldSpacePosition;
        public int radius;

        public Explosion(Vector3 worldSpacePosition, int radius)
        {
            this.worldSpacePosition = worldSpacePosition;
            this.radius = radius;
        }
    }
}
