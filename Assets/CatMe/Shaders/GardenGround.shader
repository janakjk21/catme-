Shader "CatMe/Garden Ground"
{
 SubShader {
 Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
 Pass {
 Tags {"LightMode"="UniversalForward"}
 HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 #pragma multi_compile_fog
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 struct A {float4 p:POSITION;float3 n:NORMAL;};
 struct V {float4 p:SV_POSITION;float3 w:TEXCOORD0;half3 n:TEXCOORD1;half fog:TEXCOORD2;};
 V Vert(A i){V o;o.w=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.w);o.n=TransformObjectToWorldNormal(i.n);o.fog=ComputeFogFactor(o.p.z);return o;}
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(a),hash(a+float2(1,0)),f.x),lerp(hash(a+float2(0,1)),hash(a+1),f.x),f.y);}
 half4 Frag(V i):SV_Target{
  float2 p=i.w.xz;
  float broad=noise(p*.65),fine=noise(p*65);
  float center=sin(p.y*.13)*.65;
  float edge=abs(p.x-center)-(1.25+sin(p.y*.3)*.18);
  float moss=smoothstep(-.35,.7,edge+ (broad-.5)*1.6);
  half3 gravel=lerp(half3(.40,.39,.35),half3(.58,.56,.49),fine)*lerp(.87,1.08,broad);
  half3 green=lerp(half3(.09,.13,.045),half3(.25,.30,.12),broad)*lerp(.8,1.15,fine);
  half3 color=lerp(gravel,green,moss);
  Light sun=GetMainLight(TransformWorldToShadowCoord(i.w));half3 n=normalize(i.n);
  half3 lighting=SampleSH(n)*.7+sun.color*saturate(dot(n,sun.direction))*lerp(.2,1,sun.shadowAttenuation);
  return half4(MixFog(color*lighting,i.fog),1);
 }
 ENDHLSL
 }
 UsePass "Universal Render Pipeline/Lit/ShadowCaster"
 }
}
