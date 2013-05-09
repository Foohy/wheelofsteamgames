#version 150

in vec3 _Position;
in vec2 _UV;
in vec3 _Normal;
uniform mat4 _mmatrix;
uniform mat4 _pmatrix;
uniform mat4 _vmatrix;
uniform float _time;

out vec2 ex_UV;
out vec3 ex_Normal;

vec4 vert;
void main() {
	vert = vec4( _Position.x, _Position.y, _Position.z, 1.0);
    gl_Position = _mmatrix * _pmatrix * _vmatrix * vert;
	ex_UV = _UV;
	ex_Normal = _Normal;
}