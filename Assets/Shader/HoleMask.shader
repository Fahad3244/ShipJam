Shader "Custom/HoleMask"
{
    SubShader
    {
        Tags { "Queue"="Geometry-1" "RenderType"="Opaque" }

        // Don’t draw, don’t write depth, don’t affect lighting
        ColorMask 0
        ZWrite Off
        ZTest Always
        Lighting Off
        Cull Off

        // Main stencil pass
        Pass
        {
            Name "StencilMask"
            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }
        }
    }

    // Explicitly block Unity from making a ShadowCaster pass
    SubShader
    {
        Tags { "LightMode"="ShadowCaster" }
        Pass { } // empty = no shadow casting
    }

    // Explicitly block DepthOnly (used for baked GI / lightmaps sometimes)
    SubShader
    {
        Tags { "LightMode"="DepthOnly" }
        Pass { } // empty
    }

    Fallback Off
}
