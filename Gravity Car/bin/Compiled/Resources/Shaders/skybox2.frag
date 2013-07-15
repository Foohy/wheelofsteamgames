#version 150
// It was expressed that some drivers required this next line to function properly
precision highp float;

in vec2 ex_UV;
in vec3 ex_Normal;

in vec4 color1;
in vec4 color2;
in vec3 v3Direction;

uniform mat4 _pmatrix;
uniform mat4 _vmatrix;
uniform float _time;
uniform sampler2D sampler;

uniform vec3 v3LightPos;
uniform float g;
uniform float g2;

out vec4 gl_FragColor;
void main()
{
	float fCos = dot(v3LightPos, v3Direction) / length(v3Direction);
	float fMiePhase = 1.5 * ((1.0 - g2) / (2.0 + g2)) * (1.0 + fCos*fCos) / pow(1.0 + g2 - 2.0*g*fCos, 1.5);
	gl_FragColor = color1 + fMiePhase * color2;
	gl_FragColor.a = gl_FragColor.b;
}