// based on https://williamchyr.com/unity-shaders-depth-and-normal-textures-part-3/
Shader "Custom/DepthNormals" {
Properties {}
SubShader {
Tags { "RenderType"="Opaque" }
Pass{
CGPROGRAM

#pragma vertex vert
#pragma fragment frag
#pragma target 5.0
#include "UnityCG.cginc"

 sampler2D _CameraDepthNormalsTexture;
 //sampler2D _CameraDepthTexture;
// float _StartingTime;
// float _showNormalColors = 0; //when this is 1, show normal values as colors. when 0, show depth values as colors.
 float4x4 _ViewProjectInverse;
 float4x4 _CameraLocalToWorld;


                const float Deg2Rad = (UNITY_PI * 2.0) / 360.0;

struct v2f {
    float4 pos : SV_POSITION;
    float4 wpos : TEXCOORD0;
    float4 scrPos: TEXCOORD1;
};

v2f vert (appdata_base v){
    v2f o;
    o.pos =  UnityObjectToClipPos(v.vertex);
    o.wpos =  mul (unity_ObjectToWorld, v.vertex);
     o.scrPos = ComputeScreenPos(o.pos);
     o.scrPos.y = 1 - o.scrPos.y;
    // TODO: Shader warning in 'Custom/DepthNormals': Use of UNITY_MATRIX_MV is detected. To transform a vertex into view space, consider using UnityObjectToViewPos for better performance.
    //o.ray.xyz = mul(UNITY_MATRIX_MV, v.vertex).xyz * float3(-1.0, -1.0, 1.0);

    return o;
}

float mod(float x, float y)
{
  return x - y * floor(x/y);
}

half4 frag (v2f i) : COLOR {
    
    return i.wpos;
   float camHorizFov = 72; // TODO parameterize
   // ratio between the width of the projected image and the distance from the camera
   float camHorizFovRatio =  tan(Deg2Rad * (camHorizFov/2));
   camHorizFovRatio = 1; // hack something about right

     float3 normalValues;
     float depthValue;
     //extract depth value and normal values
         DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, i.scrPos.xy), depthValue, normalValues);
    //float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.scrPos.xy);
    //float linearDepth = LinearEyeDepth(depthValue);


    float clippingDistance = 1000;
    float worldDepth = depthValue * clippingDistance;

    //if (worldDepth > 5) return 0;

    //return mul(_CameraLocalToWorld, float4(1, 0,0,1));

    float x = (i.scrPos.x - 0.5) * worldDepth * camHorizFovRatio;
    float y = (i.scrPos.y - 0.5) * worldDepth * camHorizFovRatio;

    //return worldDepth;
    float4 cameraspacePos = float4(x,y, worldDepth, 1);
    float4 res = mul(_CameraLocalToWorld, cameraspacePos);
    return float4(res.xyz, 1);

    
    return mod((worldDepth + _WorldSpaceCameraPos.z), 1);





     //float3 worldspace = i.worldDirection * linearDepth + _WorldSpaceCameraPos;

    //  float4 pos;

    // pos.x = (i.scrPos.x * 2.0f) - 1.0f;
    // pos.y = (i.scrPos.y * 2.0f - 1.0f);
    // pos.z = depthValue;
    // pos.w = 1.0f;

    // //float4x4 ivp = inverse(mul(UNITY_MATRIX_P,UNITY_MATRIX_V));
    // float4x4 ivp = _ViewProjectInverse;
    // pos = mul(ivp, pos );
    // pos /= pos.w;
     

    // float4 color = float4(pos.xyz, 1.0);
    // return color/ 4;

    // // one idea from https://forum.unity.com/threads/worldposition-from-depth-value.221115/#post-1475531
    //   float4 H = float4( i.scrPos.x, i.scrPos.y, linearDepth, 1.0f );
    //   float4 D = mul( _ViewProjectInverse, H );
    //   return (D/D.w);     
    

    // return linearDepth;
    
    //return float4(i.scrPos.xxy, 1);
    //return tex2D(_CameraDepthNormalsTexture, i.scrPos.xy);

    // if (_showNormalColors == 1){
        //  float4 normalColor = float4(normalValues, 1);
        //  return normalColor;
    // } else {
    //     float4 depth = depthValue;
    //     return depth;
    // }
}




ENDCG
}
}
FallBack "Diffuse"
}