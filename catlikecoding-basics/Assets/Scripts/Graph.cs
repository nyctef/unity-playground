using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Graph : MonoBehaviour
{
    public Transform pointPrefab = default;

    [Range(10, 100)]
    public int resolution = 10;

    Transform[] points;

    void Awake()
    {
        points = new Transform[resolution];
        var position = Vector3.zero;
        var step = (points.Length / 2f);
        var scale = Vector3.one / step;

        for (int i = 0; i < points.Length; i++)
        {
            Transform point = Instantiate(pointPrefab);
            point.SetParent(transform, false);
            position.x = (i + 0.5f) / step - 1f;
            point.localPosition = position;
            point.localScale = scale;
            points[i] = point;
        }

    }

    void Update()
    {
        for (int i = 0; i < points.Length; i++)
        {
            var position = points[i].transform.localPosition;
            position.y = Mathf.Sin(Mathf.PI * (position.x + Time.time));
            points[i].transform.localPosition = position;
        }
    }
}
