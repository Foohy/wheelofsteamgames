#version 150

// It was expressed that some drivers required this next line to function properly
precision highp float;
const int MAX_POINT_LIGHTS = 2;
const int MAX_SPOT_LIGHTS = 2;
const int MAX_SHADOW_CASTERS = 2;

in vec4 ex_LightSpacePos;
in vec2 ex_UV;
in vec3 ex_Normal;
in vec3 ex_Tangent;

in vec3 WorldPos0;  

out vec4 gl_FragColor;

struct BaseLight
{
	vec3 Color;
	float AmbientIntensity;
	float DiffuseIntensity;
};

struct DirectionalLight
{
	BaseLight Base;
	vec3 Direction;
};

struct Attenuation
{
	float Constant;
	float Linear;
	float Exp;
};

struct PointLight
{
	BaseLight Base;
	vec3 Position;
	Attenuation Atten;
};

struct SpotLight
{
	PointLight Base;
	vec3 Direction;
	float Cutoff;
};

struct ShadowCaster
{
	PointLight Base;
	vec3 Direction;
	float Cutoff;
	float Brightness;
	int Cheap;
};

vec2 poissonDisk[16] = vec2[]( 
   vec2( -0.94201624, -0.39906216 ), 
   vec2( 0.94558609, -0.76890725 ), 
   vec2( -0.094184101, -0.92938870 ), 
   vec2( 0.34495938, 0.29387760 ), 
   vec2( -0.91588581, 0.45771432 ), 
   vec2( -0.81544232, -0.87912464 ), 
   vec2( -0.38277543, 0.27676845 ), 
   vec2( 0.97484398, 0.75648379 ), 
   vec2( 0.44323325, -0.97511554 ), 
   vec2( 0.53742981, -0.47373420 ), 
   vec2( -0.26496911, -0.41893023 ), 
   vec2( 0.79197514, 0.19090188 ), 
   vec2( -0.24188840, 0.99706507 ), 
   vec2( -0.81409955, 0.91437590 ), 
   vec2( 0.19984126, 0.78641367 ), 
   vec2( 0.14383161, -0.14100790 ) 
);

uniform PointLight gPointLights[MAX_POINT_LIGHTS];
uniform SpotLight gSpotLights[MAX_SPOT_LIGHTS];
uniform ShadowCaster gShadowCasters[MAX_SHADOW_CASTERS];
uniform DirectionalLight gDirectionalLight;

uniform mat4 _pmatrix;
uniform mat4 _vmatrix;
uniform float _time;
uniform int gNumPointLights;
uniform int gNumSpotLights;
uniform int gNumShadowCasters;
uniform float gMatSpecularIntensity;
uniform float gSpecularPower;
uniform float gAlpha = 1.0;
uniform vec3 gEyeWorldPos;
uniform vec3 _color = vec3( 1.0, 1.0, 1.0);

uniform sampler2D sampler;
uniform sampler2D sampler_normal;
uniform sampler2D sampler_shadow;
uniform sampler2D sampler_shadow_tex;
uniform sampler2D sampler_spec;
uniform sampler2D sampler_alpha;

// Returns a random number based on a vec3 and an int.
float random(vec3 seed, int i){
	vec4 seed4 = vec4(seed,i);
	float dot_product = dot(seed4, vec4(12.9898,78.233,45.164,94.673));
	return fract(sin(dot_product) * 43758.5453);
}

float CalcShadowFactor(vec4 LightSpacePos)
{
    vec3 ProjCoords = LightSpacePos.xyz / LightSpacePos.w;
    vec2 UVCoords;
    UVCoords.x = 0.5 * ProjCoords.x + 0.5;
    UVCoords.y = 0.5 * ProjCoords.y + 0.5;
    float z = 0.5 * ProjCoords.z + 0.5;

	float visibility = 1.0;

	for (int i=0;i<4;i++)
	{
		int index = int(16.0*random(gl_FragCoord.xyy, i))%16;
		float Depth = texture2D(sampler_shadow, UVCoords + poissonDisk[index]/900.0).x;
		if ( Depth < z + 0.00000001)
		{
			visibility -= 0.25;
		}
	}

	return visibility;
} 

vec4 CalcLightInternal( BaseLight Light, vec3 LightDirection, vec3 Normal, float ShadowFactor)
{
    vec4 AmbientColor = vec4(Light.Color, 1.0f) * Light.AmbientIntensity;                   
    float DiffuseFactor = dot(Normal, -LightDirection);                                     
                                                                                            
    vec4 DiffuseColor  = vec4(0, 0, 0, 0);                                                  
    vec4 SpecularColor = vec4(0, 0, 0, 0);                                                  
                                                                                            
    if (DiffuseFactor > 0) {                                                                
        DiffuseColor = vec4(Light.Color, 1.0f) * Light.DiffuseIntensity * DiffuseFactor;    
                                                                                            
        vec3 VertexToEye = normalize(gEyeWorldPos - WorldPos0);                             
        vec3 LightReflect = normalize(reflect(LightDirection, Normal));                     
        float SpecularFactor = dot(VertexToEye, LightReflect);                              
        SpecularFactor = pow(SpecularFactor, gSpecularPower * texture( sampler_spec, ex_UV.st).x);                            
        if (SpecularFactor > 0) {                                                           
            SpecularColor = vec4(Light.Color, 1.0f) * gMatSpecularIntensity * SpecularFactor;                         
        }                                                                                   
    }                                                                                       
                                                                                            
    return (AmbientColor + ShadowFactor * (DiffuseColor + SpecularColor));    
}

vec4 CalcDirectionalLight( vec3 Normal )
{
	return CalcLightInternal( gDirectionalLight.Base, gDirectionalLight.Direction, Normal, 1.0 );
}

vec4 CalcPointLight(PointLight l, vec3 Normal, vec4 LightSpacePos)
{
    vec3 LightDirection = WorldPos0 - l.Position;
    float Distance = length(LightDirection);
    LightDirection = normalize(LightDirection);

    vec4 Color = CalcLightInternal(l.Base, LightDirection, Normal, 1.0);

    float Attenuation =  l.Atten.Constant +
                         l.Atten.Linear * Distance +
                         l.Atten.Exp * Distance * Distance;

    return Color / Attenuation;
}

vec4 CalcSpotLight(SpotLight l, vec3 Normal, vec4 LightSpacePos)
{
    vec3 LightToPixel = normalize(WorldPos0 - l.Base.Position);
    float SpotFactor = dot(LightToPixel, l.Direction);

    if (SpotFactor > l.Cutoff) 
	{
        vec4 Color = CalcPointLight(l.Base, Normal, LightSpacePos);

        return Color * (1.0 - (1.0 - SpotFactor) * 1.0/(1.0 - l.Cutoff));
    }
    else 
	{
        return vec4(0,0,0,0);
    }
}  

vec4 CalcShadowPointLight(PointLight l, vec3 Normal, vec4 LightSpacePos)
{
    vec3 LightDirection = WorldPos0 - l.Position;
    float Distance = length(LightDirection);
    LightDirection = normalize(LightDirection);
	float ShadowFactor = CalcShadowFactor( ex_LightSpacePos);

    vec4 Color = CalcLightInternal(l.Base, LightDirection, Normal, ShadowFactor);

    float Attenuation =  l.Atten.Constant +
                         l.Atten.Linear * Distance +
                         l.Atten.Exp * Distance * Distance;

    return Color / Attenuation;
}

vec4 CalcShadowSpotLight(ShadowCaster l, vec3 Normal, vec4 LightSpacePos)
{
    vec3 LightToPixel = normalize(WorldPos0 - l.Base.Position);
    float SpotFactor = dot(LightToPixel, l.Direction);

	vec3 ProjCoords = LightSpacePos.xyz / LightSpacePos.w;
	vec2 UVCoords;
	UVCoords.x = 0.5 * ProjCoords.x + 0.5;
	UVCoords.y = 0.5 * ProjCoords.y + 0.5;

    if (UVCoords.x < 1 && UVCoords.y < 1 && UVCoords.x > 0 && UVCoords.y > 0) 
	{
		vec4 Color = vec4( 1, 0, 0, 0 );
		/*
		if (l.Cheap > 0)
		{
			Color = CalcPointLight(l.Base, Normal, LightSpacePos);
		}
		else
		{
			Color = CalcShadowPointLight(l.Base, Normal, LightSpacePos);
		}
		*/
		Color = CalcShadowPointLight(l.Base, Normal, LightSpacePos);

		Color *= texture2D(sampler_shadow_tex, -UVCoords );


        return Color;
    }
    else 
	{
        return vec4(0,0,0,0);
    }
}  

vec3 CalcBumpedNormal()
{
    vec3 Normal = normalize(ex_Normal);
    vec3 Tangent = normalize(ex_Tangent);
    Tangent = normalize(Tangent - dot(Tangent, Normal) * Normal);
    vec3 Bitangent = cross(Tangent, Normal);
    vec3 BumpMapNormal = texture(sampler_normal, ex_UV).xyz;
    BumpMapNormal = 2.0 * BumpMapNormal - vec3(1.0, 1.0, 1.0);
    vec3 NewNormal;
    mat3 TBN = mat3(Tangent, Bitangent, Normal);
    NewNormal = TBN * BumpMapNormal;
    NewNormal = normalize(NewNormal);
    return NewNormal;
}

void main()
{
	vec3 Normal = CalcBumpedNormal();
	vec4 TotalLight = CalcDirectionalLight(Normal);
	
	for (int i = 0; i < gNumPointLights; i++)
	{
		TotalLight += CalcPointLight( gPointLights[i], Normal, ex_LightSpacePos );
	}

	for (int i = 0 ; i < gNumSpotLights ; i++) 
	{
        TotalLight += CalcSpotLight(gSpotLights[i], Normal, ex_LightSpacePos);
    }

	for (int i = 0; i < gNumShadowCasters; i++ )
	{
		TotalLight += CalcShadowSpotLight(gShadowCasters[i], Normal, ex_LightSpacePos );
	}

	gl_FragColor = vec4(texture2D( sampler, ex_UV.st).rgb, gAlpha * texture2D( sampler, ex_UV.st).a * texture(sampler_alpha, ex_UV.st) ) * vec4(_color * TotalLight.rgb, 1.0 );
}