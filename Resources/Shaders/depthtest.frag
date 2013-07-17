#version 150
// It was expressed that some drivers required this next line to function properly
precision highp float;

in vec2 ex_UV;
in vec3 ex_Normal;
uniform mat4 _pmatrix;
uniform mat4 _vmatrix;
uniform float _time;
uniform sampler2D sampler;
uniform sampler2D sampler_normal;
uniform sampler2D sampler_shadow;

out vec4 ex_FragColor;
void main()
{
	vec2 flipped = vec2(ex_UV.x, -ex_UV.y);
    float Depth = texture(sampler_shadow, flipped).x;
    Depth = 1.0 - (1.0 - Depth) * 25.0;
    ex_FragColor = vec4(Depth);
}