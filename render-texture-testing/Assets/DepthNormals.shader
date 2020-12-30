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


struct v2f {
    float4 pos : SV_POSITION;
    float4 wpos : TEXCOORD0;
};

v2f vert (appdata_base v){
    v2f o;
    o.pos =  UnityObjectToClipPos(v.vertex);
    o.wpos =  mul (unity_ObjectToWorld, v.vertex);

    return o;
}


half4 frag (v2f i) : COLOR {
    
    return i.wpos;
}


ENDCG
}
}
FallBack "Diffuse"
}