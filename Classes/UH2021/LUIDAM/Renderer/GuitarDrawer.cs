using GMath;
using static GMath.Gfx;
using Rendering;
using System;
using System.Collections.Generic;
using System.Text;
using static Renderer.Program;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Renderer
{

    public static class RenderUtils
    {
        
        public static void DrawArea<T>(int id, int x0, int y0, int xf, int yf, Raytracer<MyRayPayload, T> raycaster, Texture2D texture, float4x4 viewMatrix, float4x4 projectionMatrix, Scene<T> scene, int step = 1) where T : struct
        {
            for (int px = x0; px < xf; px += step)
                for (int py = y0; py < yf; py += step)
                {
                    //int progress = (px * yf + py);
                    //if (progress % 100 == 0)
                    //{
                    //    Console.WriteLine($"{id}: " + progress * 100 / (xf * yf) + "%            ");
                    //}

                    RayDescription ray = RayDescription.FromScreen(px + 0.5f, py + 0.5f, texture.Width, texture.Height, inverse(viewMatrix), inverse(projectionMatrix), 0, 1000);

                    MyRayPayload coloring = new MyRayPayload();

                    raycaster.Trace(scene, ray, ref coloring);
                    for (int i = 0; i < step; i++)
                    {
                        for (int j = 0; j < step; j++)
                        {
                            texture.Write(Math.Min(px + i, texture.Width - 1), Math.Min(py + j, texture.Height - 1), float4(coloring.Color, 1));
                        }
                    }
                }
            Console.WriteLine($"Done {id}");
        }

        public static void Draw<T>(Texture2D texture, Raytracer<MyRayPayload, T> raytracer, Scene<T> scene, float4x4 viewMatrix, float4x4 projectionMatrix, int rendStep = 1, int gridXDiv = 8, int gridYDiv = 8) where T : struct
        {
            var start = new Stopwatch();

            start.Start();

            var tasks = new List<Task>();
            int id = 0, xStep = texture.Width / gridXDiv, yStep = texture.Height / gridYDiv;
            for (int i = 0; i * yStep < texture.Height; i++)
            {
                for (int j = 0; j * xStep < texture.Width; j++)
                {
                    int threadId = id, x0 = j * xStep, y0 = i * yStep, maxX = Math.Min((j + 1) * xStep, texture.Width), maxY = Math.Min((i + 1) * yStep, texture.Height);
                    tasks.Add(Task.Run(() => RenderUtils.DrawArea(threadId, x0, y0, maxX, maxY, raytracer, texture, viewMatrix, projectionMatrix, scene, rendStep)));
                    id++;
                }
            }
            Task.WaitAll(tasks.ToArray());
            start.Stop();
            Console.WriteLine($"Elapsed {start.ElapsedMilliseconds} milliseconds");
        }
    }

    public class GuitarDrawer
    {
        public static int DrawStep { get; set; } = 1;
        public static int YGrid { get; set; } = 1;
        public static int XGrid { get; set; } = 1;

        private static GuitarBuilder CreateCSGGuitar(float4x4 worldTransformation)
        {
            var guitar = new GuitarBuilder();
            var mesh = guitar.GuitarMesh();
            float scale = 3;
            float3 lower = mesh.BoundBox.oppositeCorner, upper = mesh.BoundBox.topCorner;
            guitar.CSGWorldTransformation = guitar.StackTransformations(
                Transforms.RotateZ(pi),
                Transforms.RotateX(-pi / 2),
                MyTransforms.FitIn(lower, upper, 1, 1, 1),
                Transforms.Scale(scale, scale, scale),
                worldTransformation
                );
            return guitar;
        }

        public static void CreateCSGGuitarScene(Scene<float3> scene, float4x4 worldTransformation)
        {
            var guitar = CreateCSGGuitar(worldTransformation);
            guitar.Guitar(scene);
        }

        public static Mesh<PositionNormal> CreateGuitarMesh()
        {
            var box = 1;
            var meshScalar = 1f;

            var model = new GuitarBuilder() { MeshScalar = meshScalar }.GuitarMesh().ApplyTransforms(Transforms.Identity
                            //MeshShapeGenerator<PositionNormal>.Box(4).ApplyTransforms(
                            //Transforms.Scale(2 * 18, 2 * 2, 2 * 30)

                            , Transforms.RotateX(-pi / 2.0f - 11.3f * pi / 180.0f)
                            ).FitIn(box, box, box).FitIn(box, box, box);

            model = model.ApplyTransforms(Transforms.Identity
                                        , Transforms.Translate(1, 0, .8f)
                                            //, Transforms.RotateY(pi / 5)
                                            //, Transforms.Scale(scale, scale, scale)
                                            )
                            .Weld();

            model.ComputeNormals();

            return model;
        }

        public static void CreateGuitarMeshScene(Scene<PositionNormal> scene, float4x4 worldTransformation)
        {
            var model = CreateGuitarMesh();
            scene.Add(model.AsRaycast(), worldTransformation);
            var wall = Manifold<PositionNormal>.Surface(4, 4, (x, y) => float3(2 * x, 2 * y, 0));
            var floor = Manifold<PositionNormal>.Surface(4, 4, (x, z) => float3(2 * x, 0, 2 * z));
            wall.ComputeNormals();
            floor.ComputeNormals();
            scene.Add(wall.AsRaycast(), mul(Transforms.Translate(0, 0, 1), worldTransformation));
            scene.Add(floor.AsRaycast(), worldTransformation);
        }

        public static void GuitarCSGRaycast(Texture2D texture, float4x4 worldTransformation)
        {
            Raytracer<MyRayPayload, float3> raycaster = new Raytracer<MyRayPayload, float3>();

            // View and projection matrices
            float4x4 viewMatrix = Transforms.LookAtLH(float3(2, 1f, 4), float3(0, 0, 0), float3(0, 1, 0));
            float4x4 projectionMatrix = Transforms.PerspectiveFovLH(pi_over_4, texture.Height / (float)texture.Width, 0.01f, 20);

            Scene<float3> scene = new Scene<float3>();
            CreateCSGGuitarScene(scene, worldTransformation);

            raycaster.OnClosestHit += delegate (IRaycastContext context, float3 attribute, ref MyRayPayload payload)
            {
                payload.Color = attribute;
            };

            RenderUtils.Draw(texture, raycaster, scene, viewMatrix, projectionMatrix, DrawStep, XGrid, YGrid);
        }

        public static void GuitarRaycast(Texture2D texture, float4x4 worldTransformation)
        {
            // Scene Setup
            float3 CameraPosition = float3(1f, 1f, -1f);
            float3 LightPosition = float3(2f, 1f, -.5f);
            // View and projection matrices
            float4x4 viewMatrix = Transforms.LookAtLH(CameraPosition, float3(1f, .5f, .5f), float3(0, 1, 0));
            float4x4 projectionMatrix = Transforms.PerspectiveFovLH(pi_over_4, texture.Height / (float)texture.Width, 0.01f, 20);

            Scene<PositionNormal> scene = new Scene<PositionNormal>();
            CreateGuitarMeshScene(scene, worldTransformation);

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
                var l = dot(attribute.Normal, L);
                if (l < 0)
                {
                    l = -l;
                    attribute.Normal *= -1;
                }
                float lambertFactor = max(0, l);

                // Check ray to light...
                ShadowRayPayload shadow = new ShadowRayPayload();
                shadower.Trace(scene,
                    RayDescription.FromTo(attribute.Position + attribute.Normal * 0.001f, // Move an epsilon away from the surface to avoid self-shadowing 
                    LightPosition), ref shadow);

                payload.Color = shadow.Shadowed ? float3(.1f, .1f, .1f) : float3(1, 1, 1) * lambertFactor;
            };
            raycaster.OnMiss += delegate (IRaycastContext context, ref MyRayPayload payload)
            {
                payload.Color = float3(0, 0, 1); // Blue, as the sky.
            };

            RenderUtils.Draw(texture, raycaster, scene, viewMatrix, projectionMatrix, DrawStep, XGrid, YGrid);
        }

    }
}
