// Tut: https://www.youtube.com/watch?v=DeATXF4Szqo&t=1s&ab_channel=NedMakesGames (6.9.2021)
Shader "Custom RP/Grass"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MidRange("Midtone range", Float) = 0.8
        _ShadowRange("Shadow range", Float) = 0.15
        _HighLightColor("Highlight color", Color) = (1, 1, 1, 1)
        _MidtoneColor("Midtone color", Color) = (.5, .5, .5, 1)
        _ShadowColor("Shadow color", Color) = (0, 0, 0, 1)
        _SelfShadow("Self shadowing", Float) = 0
        _Shadow("Shadowing", Float) = 0
        _minDepthDistance("Min depth distance", Range(-.5, .5)) = 0.01
    }
    SubShader
    {
        Pass
        {
            Tags { // Indicate that we are using a custom ligth mode
                "LightMode" = "CustomLit"
            }
            
            Name "ForwardLit"
            ZTest LEqual
            ZWrite On
            //Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            
            HLSLPROGRAM
            // Tell unity withc delpendencies we require
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            // Register functions
            #pragma vertex Vertex
            #pragma fragment Fragment
            
            // Include our logic file
            #include "Grass.hlsl"
            
            ENDHLSL
        }
    }
}
