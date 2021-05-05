using GMath;
using Rendering;
using System;
using System.Collections.Generic;
using static GMath.Gfx;

namespace Renderer
{
    class Program
    {
        public struct PositionNormalCoordinate : INormalVertex<PositionNormalCoordinate>, ICoordinatesVertex<PositionNormalCoordinate>
        {
            public float3 Position { get; set; }
            public float3 Normal { get; set; }

            public float2 Coordinates { get; set; }

            public PositionNormalCoordinate Add(PositionNormalCoordinate other)
            {
                return new PositionNormalCoordinate
                {
                    Position = this.Position + other.Position,
                    Normal = this.Normal + other.Normal,
                    Coordinates = this.Coordinates + other.Coordinates
                };
            }

            public PositionNormalCoordinate Mul(float s)
            {
                return new PositionNormalCoordinate
                {
                    Position = this.Position * s,
                    Normal = this.Normal * s,
                    Coordinates = this.Coordinates * s
                };
            }

            public PositionNormalCoordinate Transform(float4x4 matrix)
            {
                float4 p = float4(Position, 1);
                p = mul(p, matrix);
                
                float4 n = float4(Normal, 0);
                n = mul(n, matrix);

                return new PositionNormalCoordinate
                {
                    Position = p.xyz / p.w,
                    Normal = n.xyz,
                    Coordinates = Coordinates
                };
            }
        }

        public struct Material
        {
            public Texture2D Diffuse;

            public float3 Specular;
            public float SpecularPower;

            public float Glossyness;

            public Sampler TextureSampler;

            public float3 EvalBRDF(PositionNormalCoordinate surfel, float3 wout, float3 win)
            {
                float3 diffuse = Diffuse.Sample(TextureSampler, surfel.Coordinates).xyz / pi;
                float3 H = normalize(win + wout);
                float3 specular = Specular * pow(max(0, dot(H, surfel.Normal)), SpecularPower) * (SpecularPower + 2) / two_pi;
                return diffuse * (1 - Glossyness) + specular * Glossyness;
            }
        }
        /// <summary>
        /// Payload used to pick a color from a hit intersection
        /// </summary>
        struct MyRayPayload
        {
            public float3 Color;
            public void ValidateColor(){
                Color.x = Math.Min(Color.x, 255);
                Color.y = Math.Min(Color.y, 255);
                Color.z = Math.Min(Color.z, 255);
            }
        }

        /// <summary>
        /// Payload used to flag when a ray was shadowed.
        /// </summary>
        struct ShadowRayPayload
        {
            public bool Shadowed;
        }

        static void Main(string[] args)
        {
            // Texture to output the image.
            Texture2D texture = new Texture2D(1024, 1024);

            RaycastingMesh(texture);

            texture.Save("test.rbm");
            Console.WriteLine("Done.");
        }


        public delegate float3 BRDF(float3 N, float3 Lin, float3 Lout);

        static BRDF LambertBRDF(float3 diffuse)
        {
            return (N, Lin, Lout) => diffuse / pi;
        }

        static BRDF BlinnBRDF(float3 specular, float power)
        {
            return (N, Lin, Lout) =>
            {
                float3 H = normalize(Lin + Lout);
                return specular * pow(max(0, dot(H, N)), power) * (power + 2) / two_pi;
            };
        }

        static BRDF Mixture(BRDF f1, BRDF f2, float alpha)
        {
            return (N, Lin, Lout) => lerp(f1(N, Lin, Lout), f2(N, Lin, Lout), alpha);
        }

        static void CreateMeshScene(Scene<PositionNormalCoordinate, Material> scene)
        {
            string rugose_texture_t = "texture.rbm";

            // Texture2D rugose_texture = Texture2DFunctions.LoadTextureFromRBM(rugose_texture_t);

            Texture2D planeTexture = new Texture2D(2, 2);
            planeTexture.Write(0, 0, float4(1, 0, 0, 1)); // red cell
            planeTexture.Write(0, 1, float4(1, 1, 0, 1)); // yellow cell
            planeTexture.Write(1, 0, float4(0, 1, 0, 1)); // green cell
            planeTexture.Write(1, 1, float4(0, 0, 1, 1)); // blue cell

            scene.Add(Raycasting.PlaneXZ.AttributesMap(a => new PositionNormalCoordinate { Position = a, Coordinates = float2(a.x, a.z), Normal = float3(0, 1, 0) }), new Material { Diffuse = planeTexture, TextureSampler = new Sampler { Wrap = WrapMode.Repeat } },
            Transforms.Identity); //Table

            scene.Add(Raycasting.PlaneYZ.AttributesMap(a => new PositionNormalCoordinate { Position = a, Coordinates = float2(a.y, a.z), Normal = float3(-1, 0, 0) }), new Material { Diffuse = planeTexture, TextureSampler = new Sampler { Wrap = WrapMode.Repeat } },
            mul(Transforms.Translate(10f, 0, 0), Transforms.Identity)); //Wall


            CoffeeMakerModel<PositionNormalCoordinate> CoffeeMaker = new CoffeeMakerModel<PositionNormalCoordinate>();
            Mesh<PositionNormalCoordinate> plastic_model = CoffeeMaker.GetPlasticMesh();
            plastic_model.ComputeNormals();

            Mesh<PositionNormalCoordinate> metal_model = CoffeeMaker.GetMetalMesh();
            metal_model.ComputeNormals();



            scene.Add(plastic_model.AsRaycast(), new Material { Diffuse = planeTexture, TextureSampler = new Sampler { Wrap = WrapMode.Repeat } }, Transforms.Identity);
            scene.Add(metal_model.AsRaycast(), new Material { Diffuse = planeTexture, TextureSampler = new Sampler { Wrap = WrapMode.Repeat } }, Transforms.Identity);

        }

        static void RaycastingMesh (Texture2D texture)
        {
            // Scene Setup
            float3 CameraPosition = float3(-12f, 6.6f, 0);
            // float3(-15, 13f, 25), ligth in the right side
            // float3(-15, 13f, 0), Light in the front
            float3[] Lights = {float3(-15, 13f, 25), float3(-15, 13f, -15)};
            float3 LightIntensity = float3(1, 1, 1) * 1500;

            // View and projection matrices
            float4x4 viewMatrix = Transforms.LookAtLH(CameraPosition, float3(0, 4, 0), float3(0, 1, 0));
            float4x4 projectionMatrix = Transforms.PerspectiveFovLH(pi_over_4, texture.Height / (float)texture.Width, 0.01f, 20);

            Scene<PositionNormalCoordinate, Material> scene = new Scene<PositionNormalCoordinate, Material>();
            CreateMeshScene(scene);

            BRDF[] brdfs =
            {
                Mixture(LambertBRDF(float3(1f, 1f, 0.8f)), BlinnBRDF(float3(1,1,1), 50), 0.2f), //table
                LambertBRDF(float3(0.86f, 0.76f, 0.75f)), //wall
                LambertBRDF(float3(0.1f, 0.1f, 0.1f)), //coffee_maker_plastic
                Mixture(LambertBRDF(float3(1f, 1f, 1f)), BlinnBRDF(float3(1,1,1), 70), 0.3f), //coffee_maker_metal
            };

            // Raycaster to trace rays and check for shadow rays.
            Raytracer<ShadowRayPayload, PositionNormalCoordinate, Material> shadower = new Raytracer<ShadowRayPayload, PositionNormalCoordinate, Material>();
            shadower.OnAnyHit += delegate (IRaycastContext context, PositionNormalCoordinate attribute, Material material, ref ShadowRayPayload payload)
            {
                // If any object is found in ray-path to the light, the ray is shadowed.
                payload.Shadowed = true;
                // No neccessary to continue checking other objects
                return HitResult.Stop;
            };

            List<Raytracer<MyRayPayload, PositionNormalCoordinate, Material>> raycasters = new List<Raytracer<MyRayPayload, PositionNormalCoordinate, Material>>();
            foreach(var LightPosition in Lights)
            {
                // Raycaster to trace rays and lit closest surfaces
                Raytracer<MyRayPayload, PositionNormalCoordinate, Material> raycaster = new Raytracer<MyRayPayload, PositionNormalCoordinate, Material>();
                raycaster.OnClosestHit += delegate (IRaycastContext context, PositionNormalCoordinate attribute, Material material, ref MyRayPayload payload)
                {
                    // Move geometry attribute to world space
                    attribute = attribute.Transform(context.FromGeometryToWorld);

                    float3 V = normalize(CameraPosition - attribute.Position);
                    float3 L = (LightPosition - attribute.Position);
                    float d = length(L);
                    L /= d; // normalize direction to light reusing distance to light

                    float3 N = attribute.Normal;

                    float lambertFactor = max(0, dot(N, L));

                    // Check ray to light...
                    ShadowRayPayload shadow = new ShadowRayPayload();
                    shadower.Trace(scene,
                        RayDescription.FromDir(attribute.Position + N * 0.001f, // Move an epsilon away from the surface to avoid self-shadowing 
                        L), ref shadow);

                    float3 Intensity = (shadow.Shadowed ? 0.0f : 1.0f) * LightIntensity / (d * d);

                    // payload.Color = brdfs[context.GeometryIndex](N, L, V) * Intensity * lambertFactor;
                    payload.Color = material.EvalBRDF(attribute, V, L) * Intensity * lambertFactor;
                };
                raycaster.OnMiss += delegate (IRaycastContext context, ref MyRayPayload payload)
                {
                    payload.Color = float3(0, 0, 1); // Blue, as the sky.
                };
                raycasters.Add(raycaster);
            }
            

            /// Render all points of the screen
            for (int px = 0; px < texture.Width; px++)
                for (int py = 0; py < texture.Height; py++)
                {
                    int progress = (px * texture.Height + py);
                    if (progress % 1000 == 0)
                    {
                        Console.Write("\r" + progress * 100 / (float)(texture.Width * texture.Height) + "%            ");
                    }

                    RayDescription ray = RayDescription.FromScreen(px + 0.5f, py + 0.5f, texture.Width, texture.Height, inverse(viewMatrix), inverse(projectionMatrix), 0, 1000);

                    MyRayPayload coloring = new MyRayPayload();
                    MyRayPayload aux = new MyRayPayload();

                    coloring.Color = float3(0, 0, 0);
                    foreach(var raycaster in raycasters)
                    {
                        raycaster.Trace(scene, ray, ref aux);
                        coloring.Color += aux.Color;
                        coloring.ValidateColor();
                    }

                    texture.Write(px, py, float4(coloring.Color, 1));
                }
        }
    }
}
