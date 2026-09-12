Shader "Custom/VertAnimShader"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}

        _PosTex("Position texture", 2D) = "black" {}

        _NmlTex("Normal texture", 2D) = "white" {}

        _Length("Animation Length", Float) = 1

        _FramesPerAnim("Frames Per Animation", Float) = 60

        _AnimIndex("Animation Index", Float) = 0
    }


    SubShader
    {
        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing

            #include "UnityCG.cginc"


            // =================================================
            // TEXTURES
            // =================================================

            sampler2D _MainTex;

            sampler2D _PosTex;

            sampler2D _NmlTex;


            // =================================================
            // VAT DATA
            // =================================================

            float4 _PosTex_TexelSize;

            float _Length;

            float _FramesPerAnim;


            // =================================================
            // ANIMATION INDEX
            // =================================================
            //
            // IMPORTANT:
            //
            // This is now a normal material property.
            //
            // It is NOT an instanced property.
            //
            // Every character in this CrowdSpawner uses
            // the same animation.
            //
            float _AnimIndex;


            // =================================================
            // PER-INSTANCE DATA
            // =================================================
            //
            // DT is still different for every character.
            //
            UNITY_INSTANCING_BUFFER_START(Props)

                UNITY_DEFINE_INSTANCED_PROP(
                    float,
                    _DT
                )

            UNITY_INSTANCING_BUFFER_END(Props)


            // =================================================
            // VERTEX INPUT
            // =================================================

            struct appdata
            {
                float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            // =================================================
            // VERTEX OUTPUT
            // =================================================

            struct v2f
            {
                float2 uv : TEXCOORD0;

                float3 normal : TEXCOORD1;

                float4 vertex : SV_POSITION;
            };


            // =================================================
            // VERTEX SHADER
            // =================================================

            v2f vert(
                appdata v,
                uint vid : SV_VertexID
            )
            {
                UNITY_SETUP_INSTANCE_ID(v);


                v2f o;


                // -------------------------------------------------
                // ANIMATION INDEX
                // -------------------------------------------------
                //
                // Normal material property.
                //
                float animIndex =
                    _AnimIndex;


                // -------------------------------------------------
                // INSTANCE TIME OFFSET
                // -------------------------------------------------

                float dt =
                    UNITY_ACCESS_INSTANCED_PROP(
                        Props,
                        _DT
                    );


                // -------------------------------------------------
                // ANIMATION TIME
                // -------------------------------------------------

                float t =
                    (_Time.y - dt) /
                    _Length;


                t =
                    frac(t);


                // -------------------------------------------------
                // FRAME
                // -------------------------------------------------

                float frame =
                    floor(
                        t *
                        (_FramesPerAnim - 1)
                    );


                // -------------------------------------------------
                // POSITION TEXTURE COORDINATES
                // -------------------------------------------------

                float x =
                    (vid + 0.5) *
                    _PosTex_TexelSize.x;


                float y =
                    (
                        animIndex *
                        _FramesPerAnim
                        +
                        frame
                        +
                        0.5
                    ) *
                    _PosTex_TexelSize.y;


                // -------------------------------------------------
                // VAT POSITION
                // -------------------------------------------------

                float4 pos =
                    tex2Dlod(
                        _PosTex,
                        float4(
                            x,
                            y,
                            0,
                            0
                        )
                    );


                // -------------------------------------------------
                // VAT NORMAL
                // -------------------------------------------------

                float3 normal =
                    tex2Dlod(
                        _NmlTex,
                        float4(
                            x,
                            y,
                            0,
                            0
                        )
                    ).xyz;


                // -------------------------------------------------
                // TRANSFORM POSITION
                // -------------------------------------------------

                o.vertex =
                    UnityObjectToClipPos(pos);


                // -------------------------------------------------
                // TRANSFORM NORMAL
                // -------------------------------------------------

                o.normal =
                    UnityObjectToWorldNormal(
                        normal
                    );


                // -------------------------------------------------
                // UV
                // -------------------------------------------------

                o.uv =
                    v.uv;


                return o;
            }


            // =================================================
            // FRAGMENT SHADER
            // =================================================

            half4 frag(
                v2f i
            ) : SV_TARGET
            {
                // -------------------------------------------------
                // SIMPLE LIGHT
                // -------------------------------------------------

                half3 lightDir =
                    normalize(
                        float3(
                            0,
                            1,
                            0
                        )
                    );


                half diff =
                    dot(
                        i.normal,
                        lightDir
                    ) *
                    0.5 +
                    0.5;


                // -------------------------------------------------
                // ALBEDO
                // -------------------------------------------------

                half4 col =
                    tex2D(
                        _MainTex,
                        i.uv
                    );


                return col * diff;
            }

            ENDCG
        }
    }
}
