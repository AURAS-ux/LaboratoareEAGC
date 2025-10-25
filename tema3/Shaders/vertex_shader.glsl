#version 330 core
layout (location = 0) in vec3 aPosition;
uniform float sX;
uniform float sY;
uniform float uRotation;

void main()
{
    float cosTheta = cos(uRotation);
    float sinTheta = sin(uRotation);
    
    mat2 rotationMatrix = mat2(
        cosTheta, -sinTheta,
        sinTheta, cosTheta
    );
    
    mat3 scalingMatrix = mat3(
        sX, 0.0, 0.0,
        0.0, sY, 0.0,
        0.0, 0.0, 1.0
    );

    vec3 scaledPosition = scalingMatrix * aPosition;
    scaledPosition.xy = rotationMatrix * scaledPosition.xy;
    gl_Position = vec4(scaledPosition, 1.0);
}
