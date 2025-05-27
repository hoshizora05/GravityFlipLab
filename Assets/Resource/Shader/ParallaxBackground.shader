Shader "GravityFlipLab/ParallaxBackground"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1,1,1,1)
        _Offset ("UV Offset", Vector) = (0,0,0,0)
        [Toggle] _EnableLoop ("Enable Loop", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Background"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        
        LOD 100
        
        Blend [_SrcBlend] [_DstBlend]
        ZWrite Off
        Cull Off
        Lighting Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            
            #include "UnityCG.cginc"
            
            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            // MaterialPropertyBlock�Ή��̃v���p�e�B
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Offset)
                UNITY_DEFINE_INSTANCED_PROP(float, _EnableLoop)
            UNITY_INSTANCING_BUFFER_END(Props)
            
            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color * UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                
                // UV���W�̕ϊ��ƃI�t�Z�b�g�K�p
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                float4 offset = UNITY_ACCESS_INSTANCED_PROP(Props, _Offset);
                o.texcoord.x += offset.x;
                o.texcoord.y += offset.y;
                
                #ifdef PIXELSNAP_ON
                o.vertex = UnityPixelSnap(o.vertex);
                #endif
                
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.texcoord;
                
                // ���[�v���L���ȏꍇ�AUV���W�����b�v
                float enableLoop = UNITY_ACCESS_INSTANCED_PROP(Props, _EnableLoop);
                if (enableLoop > 0.5)
                {
                    // frac�֐���UV���W��0-1�͈̔͂Ƀ��b�v
                    // ���̒l���������������邽�߂�fmod�̑����frac���g�p
                    uv.x = frac(uv.x);
                    uv.y = frac(uv.y);
                }
                
                // �e�N�X�`���T���v�����O
                fixed4 c = tex2D(_MainTex, uv) * i.color;
                
                // �A���t�@�v���}���`�v���C�Ή�
                c.rgb *= c.a;
                
                return c;
            }
            ENDCG
        }
    }
    
    Fallback "Sprites/Default"
}