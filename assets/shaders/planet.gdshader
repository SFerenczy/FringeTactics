shader_type canvas_item;

// The base shape of the planet (sphere0.png)
uniform sampler2D sphere_texture;
// The surface detail (noiseX.png)
uniform sampler2D noise_texture : repeat_enable;
// The lighting/shadow overlay (lightX.png)
uniform sampler2D light_texture;

// The color of the planet
uniform vec4 planet_color : source_color = vec4(1.0);
// Speed of rotation
uniform float rotation_speed = 0.05;
// Random offset so planets don't look identical
uniform float time_offset = 0.0;

void fragment() {
    // 1. Sample the base sphere to get the alpha/shape
    vec4 sphere = texture(sphere_texture, UV);
    
    // If outside the sphere, discard pixel (optimization)
    if (sphere.a < 0.01) {
        discard;
    }

    // 2. Calculate rotating UVs for the noise
    // We scroll the UV.x based on time to simulate rotation
    vec2 noise_uv = UV;
    noise_uv.x += (TIME * rotation_speed) + time_offset;
    
    // 3. Sample the noise
    vec4 noise = texture(noise_texture, noise_uv);
    
    // 4. Sample the light
    vec4 light = texture(light_texture, UV);

    // 5. Combine: 
    // Mix the color with the noise (grayscale noise darkens the color)
    vec3 final_color = planet_color.rgb * noise.rgb;
    
    // Apply lighting (Multplying usually works best for shadows)
    final_color *= light.rgb;
    
    // Output
    COLOR = vec4(final_color, sphere.a);
}
