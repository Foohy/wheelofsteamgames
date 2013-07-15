#version 150
//#version 150

in vec3 _Position;
in vec2 _UV;
in vec3 _Normal;
in vec3 _Tangent;
uniform mat4 _mmatrix;
uniform mat4 _pmatrix;
uniform mat4 _vmatrix;
uniform mat4 gLightWVP;
uniform float _time;

out vec4 ex_LightSpacePos;
out vec2 ex_UV;
out vec3 ex_Normal;
out vec3 ex_Tangent;

out vec3 WorldPos0;

vec4 vert;
void main() {
	vert = vec4( _Position.x, _Position.y, _Position.z, 1.0);
    gl_Position = _mmatrix * _pmatrix * _vmatrix * vert;
	ex_LightSpacePos = _mmatrix * gLightWVP * _vmatrix * vert;
	//vec4 v1 = _vmatrix * vert;
	//vec4 v2 = _pmatrix * v1;
	//vec4 v3 = _mmatrix * v2;

	//gl_Position = v3;

	ex_UV = _UV;
	ex_Normal = (_vmatrix * vec4(_Normal, 0.0)).xyz; 
	ex_Tangent = (_vmatrix * vec4(_Tangent, 0.0)).xyz; 
	WorldPos0  = (_vmatrix * vec4(_Position, 1.0)).xyz; 
	//ex_Normal = _Normal;
	//WorldPos0 = _Position;
}