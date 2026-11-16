#version 330 core
in vec2 TexCoord;
out vec4 FragColor;

uniform vec4 u_Color;
uniform float u_Alpha;

const float UV_CENTER = 0.5;
const float NORMALIZED_RANGE_SCALE = 2.0;
const float GLOW_INNER_EDGE = 0.0;
const float GLOW_OUTER_EDGE = 1.0;
const float CORE_OUTER_EDGE = 0.35;
const float BASE_BRIGHTNESS = 1.2;
const float CORE_INTENSITY = 1.5;

void main()
{
    vec2 centered = (TexCoord - UV_CENTER) * NORMALIZED_RANGE_SCALE;
    float dist = length(centered);

    float glow = GLOW_OUTER_EDGE - smoothstep(GLOW_INNER_EDGE, GLOW_OUTER_EDGE, dist);
    float core = GLOW_OUTER_EDGE - smoothstep(GLOW_INNER_EDGE, CORE_OUTER_EDGE, dist);

    vec3 finalColor = u_Color.rgb * (BASE_BRIGHTNESS + core * CORE_INTENSITY);
    FragColor = vec4(finalColor, glow * u_Alpha);
}