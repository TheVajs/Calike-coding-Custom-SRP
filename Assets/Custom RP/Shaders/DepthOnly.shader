Shader "hidden/DepthOnly"
{
    Properties { }
    SubShader
    {
        Pass
        {
            ZWrite On
            ColorMask 0
            Tags  { "LightMode" = "DepthOnly" }

            HLSLPROGRAM
            #pragma target 3.5 
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Assets/Custom RP/ShaderLibrary/DepthOnly.hlsl"
            ENDHLSL
        }
    }
}
