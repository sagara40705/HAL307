

Shader "Custom/VertAnimShader"
{
    Properties
    {
        //MAIN TEXTURE---------------------------
        _MainTex("Texture", 2D) = "white" {}

        //VAT TEXTURE-----------------------------
        //Stores vertex positions
        _PosTex("Position texture", 2D) = "black" {}
        //Stores normals
        _NmlTex("Normal texture", 2D) = "white" {}

        //ANIMATION SETTINGS----------------------
        _Length("Animation Length", Float) = 1
        _FramesPerAnim("Frames Per Animation", Float) = 60
    }

    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            //Enable GPU instancing
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            //TEXTURES-------------------------------
            sampler2D _MainTex, _PosTex, _NmlTex;
            float4 _PosTex_TexelSize;

            float _Length;
            float _FramesPerAnim;

            UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(float, _AnimIndex)
            UNITY_DEFINE_INSTANCED_PROP(float, _DT)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata
            {
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v, uint vid : SV_VertexID)
            {
                UNITY_SETUP_INSTANCE_ID(v);

                v2f o;

                // Normalized looping time
                float animIndex = UNITY_ACCESS_INSTANCED_PROP(Props, _AnimIndex);
                float dt = UNITY_ACCESS_INSTANCED_PROP(Props, _DT);

                float t = (_Time.y - dt) / _Length;
                t = frac(t);

                // ? FIXED: discrete frame sampling
                float frame = floor(t * (_FramesPerAnim - 1));

                // Texture sampling coords
                float x = (vid + 0.5) * _PosTex_TexelSize.x;
                float y = (animIndex * _FramesPerAnim + frame + 0.5) * _PosTex_TexelSize.y;

                // Sample VAT
                float4 pos = tex2Dlod(_PosTex, float4(x, y, 0, 0));
                float3 normal = tex2Dlod(_NmlTex, float4(x, y, 0, 0)).xyz;

                o.vertex = UnityObjectToClipPos(pos);
                o.normal = UnityObjectToWorldNormal(normal);
                o.uv = v.uv;

                return o;
            }

            half4 frag(v2f i) : SV_TARGET
            {
                half3 lightDir = normalize(float3(0, 1, 0));
                half diff = dot(i.normal, lightDir) * 0.5 + 0.5;

                half4 col = tex2D(_MainTex, i.uv);
                return col * diff;
            }

            ENDCG
        }
    }
}
