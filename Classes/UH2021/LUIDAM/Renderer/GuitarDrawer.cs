using GMath;
using static GMath.Gfx;
using Rendering;
using System;
using System.Collections.Generic;
using System.Text;
using static Renderer.Program;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;

namespace Renderer
{

    public static class RenderUtils
    {
        
        public static void DrawArea<T, M>(int id, int x0, int y0, int xf, int yf, Raytracer<DefaultRayPayload, T, M> raycaster, Texture2D texture, float4x4 viewMatrix, float4x4 projectionMatrix, Scene<T, M> scene, int step = 1) where T : struct where M : struct
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

                    DefaultRayPayload coloring = new DefaultRayPayload();

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

        public static void Draw<T, M>(Texture2D texture, Raytracer<DefaultRayPayload, T, M> raytracer, Scene<T, M> scene, float4x4 viewMatrix, float4x4 projectionMatrix, int rendStep = 1, int gridXDiv = 8, int gridYDiv = 8) where T : struct where M : struct
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

    public class GuitarDrawer<T> where T : struct, IVertex<T>, INormalVertex<T>, ICoordinatesVertex<T>
                                           , IColorable, ITransformable<T>
    {
        public static int DrawStep { get; set; } = 1;
        public static int YGrid { get; set; } = 8;
        public static int XGrid { get; set; } = 8;

        private static GuitarBuilder<T> CreateCSGGuitar(float4x4 worldTransformation)
        {
            var guitar = new GuitarBuilder<T>();
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

        public static void CreateCSGGuitarScene(Scene<float3, NoMaterial> scene, float4x4 worldTransformation)
        {
            var guitar = CreateCSGGuitar(worldTransformation);
            guitar.Guitar(scene);
        }

        public static Mesh<T> CreateGuitarMesh()
        {
            var box = 1;
            var meshScalar = 1f;

            var model = new GuitarBuilder<T>() { MeshScalar = meshScalar }.GuitarMesh()
                .ApplyTransforms(Transforms.Identity
                                ,Transforms.RotateX(-pi / 2.0f - 11.3f * pi / 180.0f)
                                )
                            .FitIn(box, box, box);

            model = model.ApplyTransforms(Transforms.Identity
                                         ,Transforms.Translate(1, 0, .8f)
                                         )
                            .Weld();

            model.ComputeNormals();
            
            return model;
        }

        public static Mesh<T> CreateWalls()
        {
            var wall = Manifold<T>.Surface(4, 4, (x, y) => float3(2 * x, 2 * y, 0));
            var wall2 = Manifold<T>.Surface(4, 4, (x, y) => float3(2 * x, 2 * y, -0.01f));
            var floor = Manifold<T>.Surface(4, 4, (x, z) => float3(2 * x, 0, 2 * z));
            wall = (wall + wall2).ApplyTransforms(Transforms.Translate(0,0,1.06f));
            
            wall.ComputeNormals();
            floor.ComputeNormals();
            GuitarBuilder<T>.AddColorToMesh(new WallsBuilder().FloorColor, floor);
            GuitarBuilder<T>.AddColorToMesh(new WallsBuilder().WallColor, wall);
            return wall + floor;
        }

        public static void CreateGuitarMeshScene(Scene<T, MyMaterial<T>> scene, float4x4 worldTransformation)
        {
            var model = CreateGuitarMesh();
            scene.Add(model.AsRaycast(), LoadMaterialFromFile("guitar_texture.material", 32, 0.9f), worldTransformation);
            var model2 = CreateWalls();
            scene.Add(model2.AsRaycast(), LoadMaterialFromFile("guitar_texture.material", 32, 0.9f), worldTransformation);
        }

        public static void GuitarCSGRaycast(Texture2D texture, float4x4 worldTransformation)
        {
            Raytracer<DefaultRayPayload, float3, NoMaterial> raycaster = new Raytracer<DefaultRayPayload, float3, NoMaterial>();

            // View and projection matrices
            float4x4 viewMatrix = Transforms.LookAtLH(float3(2, 1f, 4), float3(0, 0, 0), float3(0, 1, 0));
            float4x4 projectionMatrix = Transforms.PerspectiveFovLH(pi_over_4, texture.Height / (float)texture.Width, 0.01f, 20);

            Scene<float3, NoMaterial> scene = new Scene<float3, NoMaterial>();
            CreateCSGGuitarScene(scene, worldTransformation);

            raycaster.OnClosestHit += delegate (IRaycastContext context, float3 attribute, NoMaterial material, ref DefaultRayPayload payload)
            {
                payload.Color = attribute;
            };

            RenderUtils.Draw(texture, raycaster, scene, viewMatrix, projectionMatrix, DrawStep, XGrid, YGrid);
        }

        public static void GuitarRaycast(Texture2D texture, float4x4 worldTransformation)
        {
            //// Scene Setup
            //float3 target = float3(.95f, .55f, .5f);
            //float3 CameraPosition = float3(.95f, .55f, 0f);
            //CameraPosition += .01f * (CameraPosition - target);
            //var lightPositionWorld = mul(worldTransformation, float4x1(1f, .55f, -.8f, 0));
            //float3 LightPosition = float3(lightPositionWorld._m00, lightPositionWorld._m10, lightPositionWorld._m20);

            //// View and projection matrices
            //float4x4 viewMatrix = Transforms.LookAtLH(CameraPosition, target, float3(0, 1, 0));

            // Scene Setup
            float3 CameraPosition = float3(1.1f, 1f, -.75f);
            var lightPositionWorld = mul(worldTransformation, float4x1(2.0f, 1f, -.8f, 0));
            float3 LightPosition = float3(lightPositionWorld._m00, lightPositionWorld._m10, lightPositionWorld._m20);
            float3 LightIntensity = float3(1, 1, 1) * 1;
            // View and projection matrices
            float4x4 viewMatrix = Transforms.LookAtLH(CameraPosition, float3(1.1f, .58f, .5f), float3(0, 1, 0));
            float4x4 projectionMatrix = Transforms.PerspectiveFovLH(pi_over_4, texture.Height / (float)texture.Width, 0.01f, 20);

            Scene<T, MyMaterial<T>> scene = new Scene<T, MyMaterial<T>>();
            CreateGuitarMeshScene(scene, worldTransformation);

            // Raycaster to trace rays and check for shadow rays.
            Raytracer<MyShadowRayPayload, T, MyMaterial<T>> shadower = new Raytracer<MyShadowRayPayload, T, MyMaterial<T>>();
            shadower.OnAnyHit += delegate (IRaycastContext context, T attribute, MyMaterial<T> material, ref MyShadowRayPayload payload)
            {
                // If any object is found in ray-path to the light, the ray is shadowed.
                payload.Shadowed = true;
                // No neccessary to continue checking other objects
                return HitResult.Stop;
            };

            // Raycaster to trace rays and lit closest surfaces
            Raytracer<DefaultRayPayload, T, MyMaterial<T>> raycaster = new Raytracer<DefaultRayPayload, T, MyMaterial<T>>();
            raycaster.OnClosestHit += delegate (IRaycastContext context, T attribute, MyMaterial<T> material, ref DefaultRayPayload payload)
            {
                // Move geometry attribute to world space
                attribute = attribute.Transform(context.FromGeometryToWorld);

                float3 V = normalize(CameraPosition - attribute.Position);
                float3 L = (LightPosition - attribute.Position);
                float d = length(L);
                L /= d; // normalize direction to light reusing distance to light

                attribute.Normal = normalize(attribute.Normal);

                var l = dot(attribute.Normal, L);
                if (l < 0)
                {
                    l = -l;
                    attribute.Normal *= -1;
                }
                float lambertFactor = max(0, l);

                // Check ray to light...
                MyShadowRayPayload shadow = new MyShadowRayPayload();
                shadower.Trace(scene,
                    RayDescription.FromDir(attribute.Position + attribute.Normal * 0.001f, // Move an epsilon away from the surface to avoid self-shadowing 
                    L), ref shadow);

                float3 Intensity = (shadow.Shadowed ? 0.2f : 1.0f) * LightIntensity / (d * d);

                payload.Color = material.EvalBRDF(attribute, V, L) * Intensity * lambertFactor;
            };
            raycaster.OnMiss += delegate (IRaycastContext context, ref DefaultRayPayload payload)
            {
                payload.Color = float3(0, 0, 1); // Blue, as the sky.
            };

            RenderUtils.Draw(texture, raycaster, scene, viewMatrix, projectionMatrix, DrawStep, XGrid, YGrid);
        }

        public static MyMaterial<T> LoadMaterialFromFile(string dir, int size, float glossyness, bool rotate = false)
        {
            string str = File.ReadAllText(dir);
            string[] splitted = str.Split(' ');
            string[] clean = new string[size * size * 3];
            int count = 0;
            foreach (string var in splitted)
                if (var.Length > 0)
                    clean[count++] = var;
            Texture2D item = new Texture2D(size, size);
            count = 0;
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                {
                    float4 temp = new float4();
                    temp.x = (float)int.Parse(clean[count++]);
                    temp.y = (float)int.Parse(clean[count++]);
                    temp.z = (float)int.Parse(clean[count++]);
                    if (!rotate)
                        item.Write(i, j, temp);
                    else
                        item.Write(j, i, temp);
                }
            return new MyMaterial<T> { Diffuse = item, Glossyness = glossyness, TextureSampler = new Sampler { Wrap = WrapMode.Repeat } };
        }

    }
}
