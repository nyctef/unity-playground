using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraOutputDepthParticles : MonoBehaviour
{

    Camera cam;


    void OnEnable()
    {
        cam = GetComponent<Camera>();
        cam.depthTextureMode = DepthTextureMode.DepthNormals;
        //var replacementShader = Shader.Find("Custom/DepthNormals");
        //Debug.Log($"Setting replacement shader to {replacementShader}");
        //cam.SetReplacementShader(replacementShader, null);
    }

    void OnDisable()
    {
        //Debug.Log($"Resetting camera settings");
        //cam.depthTextureMode = DepthTextureMode.None;
        //cam.ResetReplacementShader();
    }
}
