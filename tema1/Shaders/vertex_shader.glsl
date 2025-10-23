#version 330 core
layout (location = 0) in vec3 aPosition;
uniform float sX;
uniform float sY;


void main()
{
    mat3 scalingMatrix = mat3(
        sX, 0.0, 0.0,
        0.0, sY, 0.0,
        0.0, 0.0, 1.0
    );

    vec3 scaledPosition = scalingMatrix * aPosition;
    gl_Position = vec4(scaledPosition, 1.0);
}
