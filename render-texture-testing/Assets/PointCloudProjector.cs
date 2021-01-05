using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

[ExecuteAlways]
public class PointCloudProjector : MonoBehaviour
{
    private new Camera camera;
    private VisualEffect vfx;
    private RenderTexture tex;

    // Start is called before the first frame update
    void Start()
    {
        var dimension = 256;

        camera = GetComponentInChildren<Camera>();
        vfx = GetComponentInChildren<VisualEffect>();
        tex = new RenderTexture(dimension, dimension, 32, RenderTextureFormat.ARGBFloat);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.Create();

        camera.targetTexture = tex;

        vfx.SetTexture("Texture", tex);
        vfx.SetInt("Dimension", dimension);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
