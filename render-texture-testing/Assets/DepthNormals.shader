// based on https://williamchyr.com/unity-shaders-depth-and-normal-textures-part-3/
Shader "Custom/DepthNormals" {
Properties {}
SubShader {
Tags { "RenderType"="Opaque" }
Pass{
CGPROGRAM

#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"

 sampler2D _CameraDepthNormalsTexture;
// float _StartingTime;
// float _showNormalColors = 0; //when this is 1, show normal values as colors. when 0, show depth values as colors.
 float4x4 _ViewProjectInverse;

struct v2f {
    float4 pos : SV_POSITION;
    float4 scrPos: TEXCOORD1;
    float4 ray: TEXCOORD2;
};

v2f vert (appdata_base v){
    v2f o;
    o.pos = UnityObjectToClipPos(v.vertex);
     o.scrPos = ComputeScreenPos(o.pos);
     o.scrPos.y = 1 - o.scrPos.y;
    // TODO: Shader warning in 'Custom/DepthNormals': Use of UNITY_MATRIX_MV is detected. To transform a vertex into view space, consider using UnityObjectToViewPos for better performance.
    o.ray.xyz = mul(UNITY_MATRIX_MV, v.vertex).xyz * float3(-1.0, -1.0, 1.0);
    o.ray.w = 1;
    return o;
}

half4 frag (v2f i) : COLOR {
    
   // return float4(1,0,1,1);

   //return float4(i.ray.xyz, 1);

     float3 normalValues;
     float depthValue;
     //extract depth value and normal values

     DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, i.scrPos.xy), depthValue, normalValues);
     float linearDepth = LinearEyeDepth(depthValue);
     return depthValue;

    // // one idea from https://forum.unity.com/threads/worldposition-from-depth-value.221115/#post-1475531
      float4 H = float4( i.scrPos.x, i.scrPos.y, linearDepth, 1.0f );
      float4 D = mul( _ViewProjectInverse, H );
      return (D/D.w) / 4;
      return fmod(D / D.w, 1);        
    

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