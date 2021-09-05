Shader "Custom/AddOutlines_Shader"
{
    
    Properties
    {
        // _MainTex gets populated with the render result up to this point
	    [HideInInspector]_MainTex ("Base (RGB)", 2D) = "white" {}
    }

    // The SubShader block containing the Shader code. 
    SubShader
    {
        Pass
        {
            // The HLSL code block. Unity SRP uses the HLSL language.
            HLSLPROGRAM
            
            // This line defines the name of the vertex shader. 
            #pragma vertex vert
            // This line defines the name of the fragment shader. 
            #pragma fragment frag

            // The Core.hlsl file contains definitions of frequently used HLSL
            // macros and functions, and also contains #include references to other
            // HLSL files (for example, Common.hlsl, SpaceTransforms.hlsl, etc.).
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"       

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

            TEXTURE2D(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);

            TEXTURE2D(_CameraDepthNormalsTexture);
            SAMPLER(sampler_CameraDepthNormalsTexture);     

            // The structure definition defines which variables it contains.
            // This example uses the Attributes structure as an input structure in
            // the vertex shader.
            struct Attributes
            {
                // The positionOS variable contains the vertex positions in object
                // space.
                float4 positionOS   : POSITION;
                float2 uv               : TEXCOORD0;
            };

            struct Varyings
            {
                // The positions in this struct must have the SV_POSITION semantic.
                float4 positionHCS  : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float3 DecodeNormal(float4 enc)
            {
                // TODO: see if we can just get a proper normals texture instead of using CameraDepthNormals
                float kScale = 1.7777;
                float3 nn = enc.xyz*float3(2*kScale,2*kScale,0) + float3(-kScale,-kScale,1);
                float g = 2.0 / dot(nn.xyz,nn.xyz);
                float3 n;
                n.xy = g*nn.xy;
                n.z = g-1;
                return n;
            }

            float Depth(float2 uv) { return SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, uv); }
            float3 Normal(float2 uv) { return DecodeNormal(SAMPLE_TEXTURE2D(_CameraDepthNormalsTexture, sampler_CameraDepthNormalsTexture, uv)); }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // TODO: the shader just goes black if we don't include this step - why?
                // we're not using the position at all.
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;

                return OUT;
            }

            // The fragment shader definition.            
            half4 frag(Varyings IN) : SV_Target
            {
                half4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                float2 leftPixel = IN.uv - (float2(1, 0) * _MainTex_TexelSize.xy);
                float2 bottomPixel = IN.uv + (float2(0, 1) * _MainTex_TexelSize.xy);

                float normalDist = 0;
                normalDist += length(Normal(leftPixel) - Normal(IN.uv));
                normalDist += length(Normal(bottomPixel) - Normal(IN.uv));

                float depthDist = 0; 
                depthDist += abs(Depth(leftPixel) - Depth(IN.uv));
                depthDist += abs(Depth(bottomPixel) - Depth(IN.uv));

                // return half4(normalDist, depthDist, 0, 0);

                if (normalDist > 0.01) {
                    baseColor = 0;
                }

                if (depthDist > 0.01) {
                    baseColor = 0;
                }

                return baseColor;
            }
            ENDHLSL
        }
    }
}
