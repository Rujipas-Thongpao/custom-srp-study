Shader "Hidden/Custom RP/PostFXStackPasses"
{
    SubShader{
        Cull Off
        ZTest Always
        ZWrite Off

        HLSLINCLUDE
        #include "../ShaderLibrary/Common.hlsl"
        #include "PostFXStackPasses.hlsl"
        ENDHLSL

        Pass {
            Name "copy"

            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment CopyPassFragment
            ENDHLSL
        }
        Pass {
            Name "Bloom Horizaontal"

            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment BloomHorizontalPassFragment 
            ENDHLSL
        }
        Pass {
            Name "Bloom Vertical"

            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment BloomVerticalPassFragment 
            ENDHLSL
        }
        Pass {
            Name "Bloom Add"

            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment BloomCombinePassFragment 
            ENDHLSL
        }
        Pass {
            Name "Pre filter"

            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment BloomPrefilterPassFragment 
            ENDHLSL
        }
        Pass {
            Name "Pre filter Fireflies"

            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment BloomPrefilterFirefliesPassFragment 
            ENDHLSL
        }
        Pass {
            Name "Bloom Scatter"

            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment BloomScatterPassFragment 
            ENDHLSL
        }
        Pass {
            Name "Bloom Scatter Final"

            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment BloomCombinePassFragment 
            ENDHLSL
        }
        Pass {
            Name "Tone Mapping Reinhard"

            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment ToneMappingReinhardPassFragment
            ENDHLSL
        }
        Pass {
            Name "Tone Mapping Neutral"

            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment ToneMappingNeutralPassFragment
            ENDHLSL
        }
        Pass {
            Name "Tone Mapping ACES"

            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment ToneMappingACESPassFragment
            ENDHLSL
        }
        Pass {
            Name "Tone Mapping None"

            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment ToneMappingNonePassFragment
            ENDHLSL
        }
        Pass {
            Name "Final"

            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex DefaultPassVertex
                #pragma fragment FinalPassFragment
            ENDHLSL
        }
    }
}
