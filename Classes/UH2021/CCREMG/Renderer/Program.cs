using GMath;
using Rendering;
using System;
using System.Collections.Generic;
using static GMath.Gfx;

namespace Renderer
{
    class Program
    {
        struct PositionNormal : INormalVertex<PositionNormal>
        {
            public float3 Position { get; set; }
            public float3 Normal { get; set; }

            public PositionNormal Add(PositionNormal other)
            {
                return new PositionNormal
                {
                    Position = this.Position + other.Position,
                    Normal = this.Normal + other.Normal
                };
            }

            public PositionNormal Mul(float s)
            {
                return new PositionNormal
                {
                    Position = this.Position * s,
                    Normal = this.Normal * s
                };
            }

            public PositionNormal Transform(float4x4 matrix)
            {
                float4 p = float4(Position, 1);
                p = mul(p, matrix);
                
                float4 n = float4(Normal, 0);
                n = mul(n, matrix);

                return new PositionNormal
                {
                    Position = p.xyz / p.w,
                    Normal = n.xyz
                };
            }
        }

        /// <summary>
        /// Payload used to pick a color from a hit intersection
        /// </summary>
        struct MyRayPayload
        {
            public float3 Color;
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
            Texture2D texture = new Texture2D(512, 512);

            RaycastingMesh(texture);

            texture.Save("test.rbm");
            Console.WriteLine("Done.");
        }

        static Mesh<PositionNormal> CreateCoffeeModel()
        {
            CoffeeMakerModel<PositionNormal> CoffeeMaker = new CoffeeMakerModel<PositionNormal>();
            Mesh<PositionNormal> model = CoffeeMaker.GetMesh();
            model.ComputeNormals();
            return model;
        }

        static void CreateMeshScene(Scene<PositionNormal> scene)
        {
            var model = CreateCoffeeModel();
            scene.Add(model.AsRaycast(), Transforms.Identity);
        }

        static void RaycastingMesh (Texture2D texture)
        {
            // Scene Setup
            float3 CameraPosition = float3(-12f, 6.6f, 0);
            float3 LightPosition = float3(-12, 6.6f, -20);
            // View and projection matrices
            float4x4 viewMatrix = Transforms.LookAtLH(CameraPosition, float3(0, 4, 0), float3(0, 1, 0));
            float4x4 projectionMatrix = Transforms.PerspectiveFovLH(pi_over_4, texture.Height / (float)texture.Width, 0.01f, 20);

            Scene<PositionNormal> scene = new Scene<PositionNormal>();
            CreateMeshScene(scene);

            // Raycaster to trace rays and check for shadow rays.
            Raytracer<ShadowRayPayload, PositionNormal> shadower = new Raytracer<ShadowRayPayload, PositionNormal>();
            shadower.OnAnyHit += delegate (IRaycastContext context, PositionNormal attribute, ref ShadowRayPayload payload)
            {
                // If any object is found in ray-path to the light, the ray is shadowed.
                payload.Shadowed = true;
                // No neccessary to continue checking other objects
                return HitResult.Stop;
            };

            // Raycaster to trace rays and lit closest surfaces
            Raytracer<MyRayPayload, PositionNormal> raycaster = new Raytracer<MyRayPayload, PositionNormal>();
            raycaster.OnClosestHit += delegate (IRaycastContext context, PositionNormal attribute, ref MyRayPayload payload)
            {
                // Move geometry attribute to world space
                attribute = attribute.Transform(context.FromGeometryToWorld);

                float3 V = normalize(CameraPosition - attribute.Position);
                float3 L = normalize(LightPosition - attribute.Position);
                float lambertFactor = max(0, dot(attribute.Normal, L));

                // Check ray to light...
                ShadowRayPayload shadow = new ShadowRayPayload();
                shadower.Trace(scene,
                    RayDescription.FromTo(attribute.Position + attribute.Normal * 0.001f, // Move an epsilon away from the surface to avoid self-shadowing 
                    LightPosition), ref shadow);

                payload.Color = shadow.Shadowed ? float3(0, 0, 0) : float3(1, 1, 1) * lambertFactor;
            };
            raycaster.OnMiss += delegate (IRaycastContext context, ref MyRayPayload payload)
            {
                payload.Color = float3(0, 0, 1); // Blue, as the sky.
            };

            /// Render all points of the screen
            for (int px = 0; px < texture.Width; px++)
                for (int py = 0; py < texture.Height; py++)
                {
                    int progress = (px * texture.Height + py);
                    if (progress % 100 == 0)
                    {
                        Console.Write("\r" + progress * 100 / (float)(texture.Width * texture.Height) + "%            ");
                    }

                    RayDescription ray = RayDescription.FromScreen(px + 0.5f, py + 0.5f, texture.Width, texture.Height, inverse(viewMatrix), inverse(projectionMatrix), 0, 1000);

                    MyRayPayload coloring = new MyRayPayload();

                    raycaster.Trace(scene, ray, ref coloring);

                    texture.Write(px, py, float4(coloring.Color, 1));
                }
        }
    }
}
