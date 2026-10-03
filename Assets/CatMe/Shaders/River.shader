// Cheap stylised river: scrolling ripple bands, bright sun-glint sparkles, deep-to-shallow gradient and foam at the banks.
// Opaque and unlit (no realtime reflections, no depth texture) so it is affordable on phones.
Shader "CatMe/River"
{
    Properties
    {
        _Deep ("Deep colour", Color) = (0.08, 0.38, 0.60, 1)
        _Shallow ("Shallow colour", Color) = (0.34, 0.80, 0.90, 1)
        _Foam ("Foam colour", Color) = (1, 1, 1, 1)
        _FlowDirection ("Flow direction (xz)", Vector) = (0, 1, 0, 0)
        _Speed ("Flow speed", Float) = 0.35
        _RippleScale ("Ripple scale", Float) = 1.4
        _Sparkle ("Sparkle strength", Range(0, 2)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Deep, _Shallow, _Foam;
                float4 _FlowDirection;
                float _Speed, _RippleScale, _Sparkle;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float fogFactor : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertex = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertex.positionCS;
                output.positionWS = vertex.positionWS;
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(vertex.positionCS.z);
                return output;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 p = input.positionWS.xz * _RippleScale;
                float2 flow = normalize(_FlowDirection.xz + 0.0001) * _Time.y * _Speed;
                float2 q = p - flow;

                // Layered sine ripples give long soft bands that drift downstream.
                float r1 = sin(q.x * 1.7 + q.y * 0.6 + _Time.y * 0.9);
                float r2 = sin(q.y * 2.3 - q.x * 1.1 - _Time.y * 1.3);
                float r3 = sin((q.x + q.y) * 3.1 + _Time.y * 2.0);
                float ripple = (r1 + r2 + r3) / 3.0 * 0.5 + 0.5;

                // Sparkles: small cells that flash when the ripple crests line up.
                float2 cell = floor(q * 3.0);
                float rnd = Hash21(cell);
                float twinkle = step(0.82, rnd) * smoothstep(0.62, 0.95, ripple) * (0.5 + 0.5 * sin(_Time.y * 6.0 + rnd * 40.0));
                float glint = pow(saturate(ripple), 9.0);

                // Bank-to-centre gradient from the cube's UV: edges shallow and foamy, centre deep.
                float edge = abs(input.uv.x * 2.0 - 1.0);
                half3 body = lerp(_Deep.rgb, _Shallow.rgb, smoothstep(0.05, 0.95, edge) * 0.8 + ripple * 0.18);
                half3 colour = body + glint * 0.18 + twinkle * _Sparkle;

                float foam = smoothstep(0.86, 0.99, edge) * (0.55 + 0.45 * ripple);
                colour = lerp(colour, _Foam.rgb, saturate(foam));

                colour = MixFog(colour, input.fogFactor);
                return half4(colour, 1);
            }
            ENDHLSL
        }
    }
}
