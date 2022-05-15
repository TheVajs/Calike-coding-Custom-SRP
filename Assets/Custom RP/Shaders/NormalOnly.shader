Shader "hidden/NoemalOnly"
{
    Properties { }
    SubShader
    {
        Pass
        {
            //ZWrite On
            ColorMask 0
            Tags  { "LightMode" = "NormalOnly" }

            HLSLPROGRAM
            #pragma target 3.5 
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Assets/Custom RP/ShaderLibrary/NormalOnly.hlsl"
            ENDHLSL
        }
    }
}
