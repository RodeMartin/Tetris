#version 330 core
in vec2 TexCoord;
out vec4 FragColor;

uniform vec4 u_Color;
uniform float u_Time;
uniform float u_IsGhost;

const float UV_CENTER = 0.5;
const float NORMALIZED_RANGE_SCALE = 2.0;
const float ZERO_UNIT = 0.0;
const float FULL_UNIT = 1.0;
const float OUTER_BORDER_START = 0.76;
const float OUTER_BORDER_END = 0.94;
const float INNER_CORE_END = 0.72;
const float PULSE_BASE = 0.88;
const float PULSE_AMPLITUDE = 0.12;
const float PULSE_SPEED = 5.0;
const float CORE_BASE_BRIGHTNESS = 0.35;
const float CORE_INNER_BOOST = 0.25;
const float EDGE_GLOW_INTENSITY = 1.75;
const float GHOST_FLAG_THRESHOLD = 0.5;
const float GHOST_RING_INNER_START = 0.78;
const float GHOST_RING_INNER_END = 0.90;
const float GHOST_RING_OUTER_START = 0.94;
const float GHOST_COLOR_INTENSITY = 1.4;
const float GHOST_ALPHA_BASE = 0.45;
const float GHOST_ALPHA_PULSE = 0.15;
const float GHOST_PULSE_SPEED = 6.0;

void main()
{
    vec2 centerDist = abs(TexCoord - UV_CENTER) * NORMALIZED_RANGE_SCALE;
    float maxDist = max(centerDist.x, centerDist.y);

    float outerBorder = smoothstep(OUTER_BORDER_START, OUTER_BORDER_END, maxDist);
    float innerCore = FULL_UNIT - smoothstep(ZERO_UNIT, INNER_CORE_END, maxDist);
    float pulse = PULSE_BASE + PULSE_AMPLITUDE * sin(u_Time * PULSE_SPEED);

    vec3 coreColor = u_Color.rgb * (CORE_BASE_BRIGHTNESS + CORE_INNER_BOOST * innerCore);
    vec3 edgeGlow = u_Color.rgb * EDGE_GLOW_INTENSITY * pulse;

    vec3 color = mix(coreColor, edgeGlow, outerBorder);
    float alpha = FULL_UNIT;

    if (u_IsGhost > GHOST_FLAG_THRESHOLD)
    {
        float innerEdge = smoothstep(GHOST_RING_INNER_START, GHOST_RING_INNER_END, maxDist);
        float outerEdge = FULL_UNIT - smoothstep(GHOST_RING_OUTER_START, FULL_UNIT, maxDist);
        float ghostRing = innerEdge * outerEdge;

        color = u_Color.rgb * GHOST_COLOR_INTENSITY;
        alpha = ghostRing * (GHOST_ALPHA_BASE + GHOST_ALPHA_PULSE * sin(u_Time * GHOST_PULSE_SPEED));
    }

    FragColor = vec4(color, alpha);
}