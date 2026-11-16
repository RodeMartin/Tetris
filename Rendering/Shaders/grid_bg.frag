#version 330 core
in vec2 TexCoord;
out vec4 FragColor;

uniform float u_Time;
uniform vec2 u_GridSize;

const float LINE_THICKNESS = 0.04;
const float FULL_UNIT = 1.0;
const float ZERO_UNIT = 0.0;
const float WAVE_OFFSET = 0.5;
const float WAVE_AMPLITUDE = 0.5;
const float WAVE_FREQUENCY = 8.0;
const float WAVE_SPEED = 2.5;
const float LINE_INTENSITY = 0.45;
const vec3 BASE_COLOR = vec3(0.03, 0.03, 0.06);
const vec3 WAVE_COLOR_PRIMARY = vec3(0.12, 0.05, 0.25);
const vec3 WAVE_COLOR_SECONDARY = vec3(0.0, 0.6, 0.8);

void main()
{
    vec2 gridUV = TexCoord * u_GridSize;
    vec2 cellFract = fract(gridUV);

    float lineX = step(cellFract.x, LINE_THICKNESS) + step(FULL_UNIT - LINE_THICKNESS, cellFract.x);
    float lineY = step(cellFract.y, LINE_THICKNESS) + step(FULL_UNIT - LINE_THICKNESS, cellFract.y);
    float gridLine = clamp(lineX + lineY, ZERO_UNIT, FULL_UNIT);

    float wave = WAVE_OFFSET + WAVE_AMPLITUDE * sin(TexCoord.y * WAVE_FREQUENCY - u_Time * WAVE_SPEED);
    vec3 lineColor = mix(WAVE_COLOR_PRIMARY, WAVE_COLOR_SECONDARY, wave) * LINE_INTENSITY;

    vec3 finalColor = mix(BASE_COLOR, lineColor, gridLine);
    FragColor = vec4(finalColor, FULL_UNIT);
}