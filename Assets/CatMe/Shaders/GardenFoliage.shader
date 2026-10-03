Shader "CatMe/Garden Foliage"
{
 Properties {
  _BaseMap("Albedo",2D)="white"{}
  _BaseColor("Tint",Color)=(1,1,1,1)
  _Cutoff("Cutout",Range(0,1))=0.42
  _Wind("Wind",Float)=0.025
 }
 SubShader {
 Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest"}
 Cull Off
 HLSLINCLUDE
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
 CBUFFER_START(UnityPerMaterial)
 float4 _BaseMap_ST; half4 _BaseColor; float _Cutoff; float _Wind;
 float _CatMeWindScale;
 CBUFFER_END
 struct Attributes {float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0;};
 struct Varyings {float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float3 positionWS:TEXCOORD1; half3 normalWS:TEXCOORD2; half fog:TEXCOORD3;};
 float3 WindPosition(float3 p) {
  float3 w=TransformObjectToWorld(p);
  float weight=saturate(p.y*.7);
  w.xz += float2(1,.42)*sin(_Time.y*1.35+w.x*.61+w.z*.47)*_Wind*weight*_CatMeWindScale;
  return w;
 }
 Varyings Vert(Attributes v) {
  Varyings o; o.positionWS=WindPosition(v.positionOS.xyz); o.positionCS=TransformWorldToHClip(o.positionWS);
  o.normalWS=TransformObjectToWorldNormal(v.normalOS); o.uv=TRANSFORM_TEX(v.uv,_BaseMap);o.fog=ComputeFogFactor(o.positionCS.z);return o;
 }
 half4 SampleLeaf(float2 uv) {half4 c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv)*_BaseColor;clip(c.a-_Cutoff);return c;}
 ENDHLSL
 Pass {
 Name "ForwardLit" Tags {"LightMode"="UniversalForward"}
 HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 #pragma multi_compile_fog
 half4 Frag(Varyings i, FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target {
  half4 c=SampleLeaf(i.uv);half3 n=normalize(i.normalWS)*IS_FRONT_VFACE(face,1,-1);
  Light sun=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
  half diffuse=saturate(dot(n,sun.direction)*.65+.35);
  half3 light=SampleSH(n)*.8+sun.color*diffuse*lerp(.3,1,sun.shadowAttenuation);
  return half4(MixFog(c.rgb*light,i.fog),1);
 }
 ENDHLSL
 }
 Pass {
 Name "ShadowCaster" Tags {"LightMode"="ShadowCaster"}
 ZWrite On ZTest LEqual ColorMask 0
 HLSLPROGRAM
 #pragma vertex ShadowVert
 #pragma fragment ShadowFrag
 float3 _LightDirection;
 Varyings ShadowVert(Attributes v) {
  Varyings o=Vert(v);float3 normal=TransformObjectToWorldNormal(v.normalOS);
  o.positionCS=TransformWorldToHClip(ApplyShadowBias(o.positionWS,normal,_LightDirection));
  #if UNITY_REVERSED_Z
  o.positionCS.z=min(o.positionCS.z,UNITY_NEAR_CLIP_VALUE);
  #else
  o.positionCS.z=max(o.positionCS.z,UNITY_NEAR_CLIP_VALUE);
  #endif
  return o;
 }
 half4 ShadowFrag(Varyings i):SV_Target {SampleLeaf(i.uv);return 0;}
 ENDHLSL
 }
 }
}
