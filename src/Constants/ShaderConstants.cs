namespace BalatroSeedOracle.Constants
{
    /// <summary>
    /// Shader constants for Balatro-inspired paint-mixing visualization.
    /// This centralized location ensures shader consistency across all renderers.
    /// </summary>
    public static class ShaderConstants
    {
        /// <summary>
        /// The main Balatro paint-mixing shader with all visual effects.
        /// Features:
        /// - RGB color mixing with paint-like blending
        /// - Pixelated/posterized effect with parallax support
        /// - Center swirl animation with configurable spin
        /// - Saturation boost for dynamic color intensity
        /// - Zoom effects for responsive visuals
        /// - Dynamic loop count for performance tuning
        /// </summary>
        public const string BALATRO_SHADER =
            @"
                    uniform float2 resolution;
                    uniform float time;
                    uniform float spin_time;
                    uniform float4 colour_1;
                    uniform float4 colour_2;
                    uniform float4 colour_3;
                    uniform float contrast;
                    uniform float spin_amount;
                    uniform float parallax_x;
                    uniform float parallax_y;
                    uniform float zoom_scale;
                    uniform float saturation_amount;
                    uniform float saturation_amount_2;
                    uniform float pixel_size;  // Was const, now uniform!
                    uniform float spin_ease;   // Was const, now uniform!
                    uniform float loop_count;  // Controls paint effect complexity (1-64)

                    // Helper function to convert RGB to HSV
                    float3 rgb2hsv(float3 c) {
                        float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                        float4 p = mix(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                        float4 q = mix(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
                        float d = q.x - min(q.w, q.y);
                        float e = 1.0e-10;
                        return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
                    }

                    // Helper function to convert HSV to RGB
                    float3 hsv2rgb(float3 c) {
                        float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                        float3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
                        return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
                    }

                    float4 main(float2 screen_coords) {
                        // Convert to UV coords with pixelation and parallax
                        float pix_size = length(resolution) / pixel_size;
                        float2 uv = (floor(screen_coords * (1.0 / pix_size)) * pix_size - 0.5 * resolution) / length(resolution) - float2(parallax_x, parallax_y);
                        float uv_len = length(uv);

                        // Center swirl that changes with time
                        float speed = (spin_time * spin_ease * 0.2) + 302.2;
                        float new_pixel_angle = atan(uv.y, uv.x) + speed - spin_ease * 20.0 * (1.0 * spin_amount * uv_len + (1.0 - 1.0 * spin_amount));
                        float2 mid = (resolution / length(resolution)) / 2.0;
                        uv = float2((uv_len * cos(new_pixel_angle) + mid.x), (uv_len * sin(new_pixel_angle) + mid.y)) - mid;

                        // Paint effect with zoom scale
                        uv *= (30.0 + zoom_scale);
                        speed = time * 2.0;
                        float2 uv2 = float2(uv.x + uv.y);

                        // Dynamic loop count (raised cap)
                        int max_loops = int(clamp(loop_count, 1.0, 64.0));
                        for (int i = 0; i < 64; i++) {
                            if (i >= max_loops) break;  // Early exit based on uniform
                            uv2 += sin(max(uv.x, uv.y)) + uv;
                            uv += 0.5 * float2(cos(5.1123314 + 0.353 * uv2.y + speed * 0.131121), sin(uv2.x - 0.113 * speed));
                            uv -= 1.0 * cos(uv.x + uv.y) - 1.0 * sin(uv.x * 0.711 - uv.y);
                        }

                        // Paint amount calculation
                        float contrast_mod = (0.25 * contrast + 0.5 * spin_amount + 1.2);
                        float paint_res = min(2.0, max(0.0, length(uv) * 0.035 * contrast_mod));
                        float c1p = max(0.0, 1.0 - contrast_mod * abs(1.0 - paint_res));
                        float c2p = max(0.0, 1.0 - contrast_mod * abs(paint_res));
                        float c3p = 1.0 - min(1.0, c1p + c2p);

                        // Apply saturation boost to colour_1 if requested
                        float4 adjusted_colour_1 = colour_1;
                        if (saturation_amount > 0.01) {
                            float3 hsv = rgb2hsv(colour_1.rgb);
                            float satBoost = saturation_amount * 0.3;
                            hsv.y = clamp(hsv.y + satBoost, 0.0, 1.0);
                            adjusted_colour_1 = float4(hsv2rgb(hsv), colour_1.a);
                        }

                        // Apply saturation boost to colour_2 if requested
                        float4 adjusted_colour_2 = colour_2;
                        if (saturation_amount_2 > 0.01) {
                            float3 hsv2 = rgb2hsv(colour_2.rgb);
                            float satBoost2 = saturation_amount_2 * 0.3;
                            hsv2.y = clamp(hsv2.y + satBoost2, 0.0, 1.0);
                            adjusted_colour_2 = float4(hsv2rgb(hsv2), colour_2.a);
                        }

                        // Final color mixing
                        float4 ret_col = (0.3 / contrast) * adjusted_colour_1 + (1.0 - 0.3 / contrast) * (adjusted_colour_1 * c1p + adjusted_colour_2 * c2p + float4(c3p * colour_3.rgb, c3p * colour_1.a));
                        return ret_col;
                    }";

        public const string PSYCHEDELIC_SHADER =
            @"
                uniform float2 resolution;
                uniform float time;
                uniform float speed;
                uniform float fractal_complexity;
                uniform float color_cycle;
                uniform float kaleidoscope;
                uniform float fluid_flow;
                uniform float2 mouse;
                uniform float melody;
                uniform float chords;
                uniform float bass;

                float3 hsv2rgb(float3 c) {
                    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                    float3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
                    return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
                }

                float fractalNoise(float2 p) {
                    float value = 0.0;
                    float amplitude = 1.0;
                    float frequency = 1.0;
                    for (int i = 0; i < 6; i++) {
                        value += amplitude * sin(frequency * p.x) * cos(frequency * p.y);
                        p = float2(p.x * 1.7 - p.y * 0.7, p.y * 1.7 + p.x * 0.7);
                        amplitude *= 0.5;
                        frequency *= 2.0;
                    }
                    return value;
                }

                float2 kaleidoscopeUV(float2 uv, float segments) {
                    float angle = atan(uv.y, uv.x);
                    float radius = length(uv);
                    angle = mod(angle, 3.14159265 * 2.0 / segments);
                    angle = abs(angle - 3.14159265 / segments);
                    return float2(cos(angle), sin(angle)) * radius;
                }

                float2 fluidDistortion(float2 p, float t) {
                    float2 q = float2(
                        fractalNoise(p + float2(0.0, t * 0.3)),
                        fractalNoise(p + float2(t * 0.4, 0.0))
                    );
                    return p + fluid_flow * 0.2 * q;
                }

                float tunnel(float2 uv, float t) {
                    float r = length(uv);
                    float a = atan(uv.y, uv.x);
                    float depth = 1.0 / (r + 0.01);
                    float spiral = a + depth * 0.5 + t * 0.5;
                    return sin(depth * 3.0 + t) * 0.5 +
                           cos(spiral * 8.0) * 0.3 +
                           sin(a * 6.0 + t) * 0.2;
                }

                float4 main(float2 screen_coords) {
                    float2 uv = (screen_coords - resolution * 0.5) / min(resolution.x, resolution.y);
                    float t = time * speed;

                    float2 mouseOffset = (mouse - 0.5) * 2.0;
                    uv -= mouseOffset * 0.3 * (bass + 0.5);

                    if (kaleidoscope > 0.1) {
                        uv = kaleidoscopeUV(uv, kaleidoscope);
                    }

                    uv = fluidDistortion(uv, t);

                    float zoom = 1.0 + bass * 0.3 + sin(t * 0.5) * 0.2;
                    uv *= zoom;

                    float rotation = t * 0.3 + chords * 3.14159265;
                    float2x2 rot = float2x2(cos(rotation), -sin(rotation), sin(rotation), cos(rotation));
                    uv = rot * uv;

                    float color_base = 0.0;
                    float2 p = uv * fractal_complexity;
                    for (int i = 0; i < 5; i++) {
                        float layer = fractalNoise(p + t * (0.1 + melody * 0.2));
                        color_base += layer / (float(i + 1) * 2.0);
                        p = p * 1.5 + float2(sin(t * 0.2), cos(t * 0.3));
                    }

                    color_base += tunnel(uv, t) * 0.5;

                    float hue = fract(color_base * 0.3 + t * color_cycle * 0.1 + melody * 0.5);
                    float saturation = clamp(0.7 + chords * 0.3 + sin(t * 2.0) * 0.1, 0.0, 1.0);
                    float brightness = clamp(0.6 + melody * 0.3 + abs(sin(t * 1.5)) * 0.1, 0.0, 1.0);

                    float3 color = hsv2rgb(float3(hue, saturation, brightness));
                    float glow = exp(-length(uv) * 0.5) * 0.3;
                    color += glow * float3(0.3, 0.1, 0.5);
                    float vignette = smoothstep(0.0, 1.0, 1.0 - length(uv * 0.7));
                    color *= mix(0.3, 1.0, vignette);
                    float scanline = sin(screen_coords.y * 0.5) * 0.03 + 0.97;
                    color *= scanline;
                    return float4(color, 1.0);
                }";
    }
}
