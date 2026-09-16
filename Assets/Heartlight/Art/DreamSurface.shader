Shader "Heartlight/Dream Surface"
{
    Properties
    {
        _Color("Base color",Color)=(.55,.67,.72,1)
        _TopColor("Upper tint",Color)=(.75,.83,.8,1)
        _Scale("World texture scale",Float)=1.2
        _Variation("Surface variation",Range(0,.2))=.06
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        fixed4 _Color,_TopColor;float _Scale,_Variation;
        struct Input {float3 worldPos;float3 worldNormal;};
        float hash(float3 p){p=frac(p*.1031);p+=dot(p,p.yzx+33.33);return frac((p.x+p.y)*p.z);}
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            float3 q=IN.worldPos*_Scale;float n=hash(floor(q*7));
            float up=saturate(IN.worldNormal.y)*.35+saturate(IN.worldPos.y*.13)*.16;
            o.Albedo=lerp(_Color.rgb,_TopColor.rgb,up)*(1+(n-.5)*_Variation);
            o.Metallic=0;o.Smoothness=.16;o.Alpha=1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
