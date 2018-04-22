using UnityEngine;

public static class Events
{
    public struct Explosion
    {
        public Vector3 WorldSpacePosition;
        public readonly int Radius;

        public Explosion(Vector3 worldSpacePosition, int radius)
        {
            WorldSpacePosition = worldSpacePosition;
            Radius = radius;
        }
    }
}
