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
using System.Drawing;
using System.Linq;
using Renderer.Modeling;

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

        public static void RayTrace<T>(Texture2D texture, Scene<T, MyMaterial<T>> scene, Raytracer<MyRTRayPayload, T, MyMaterial<T>> raycaster, float4x4 viewMatrix, float4x4 projectionMatrix, int rendStep = 1, int gridXDiv = 8, int gridYDiv = 8) where T : struct, IVertex<T>, INormalVertex<T>, ICoordinatesVertex<T>, IColorable, ITransformable<T>
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
                    tasks.Add(Task.Run(() => RenderUtils.RaytracePassDrawArea(threadId, x0, y0, maxX, maxY, raycaster, texture, viewMatrix, projectionMatrix, scene, rendStep)));
                    id++;
                }
            }
            Task.WaitAll(tasks.ToArray());
            start.Stop();
            Console.WriteLine($"Elapsed {start.ElapsedMilliseconds} milliseconds");
        }

        public static void RaytracePassDrawArea<T>(int id, int x0, int y0, int xf, int yf, Raytracer<MyRTRayPayload, T, MyMaterial<T>> raycaster, Texture2D texture, float4x4 viewMatrix, float4x4 projectionMatrix, Scene<T, MyMaterial<T>> scene, int step = 1) where T : struct, IVertex<T>, INormalVertex<T>, ICoordinatesVertex<T>, IColorable, ITransformable<T>
        {
            for (int px = x0; px < xf; px += step)
                for (int py = y0; py < yf; py += step)
                {

                    RayDescription ray = RayDescription.FromScreen(px + 0.5f, py + 0.5f, texture.Width, texture.Height, inverse(viewMatrix), inverse(projectionMatrix), 0, 1000);

                    MyRTRayPayload coloring = new MyRTRayPayload();
                    coloring.Bounces = 3;

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

        public static void PathTrace<T>(Texture2D texture, Scene<T, MyMaterial<T>> scene, Raytracer<MyPTRayPayload, T, MyMaterial<T>> raycaster, float4x4 viewMatrix, float4x4 projectionMatrix, int maxPass, int rendStep = 1, int gridXDiv = 8, int gridYDiv = 8, int initPass = 0) where T : struct, IVertex<T>, INormalVertex<T>, ICoordinatesVertex<T>, IColorable, ITransformable<T>
        {
            var pathTimer = new Stopwatch();
            var passTimer = new Stopwatch();

            pathTimer.Start();

            int id = 0, xStep = texture.Width / gridXDiv, yStep = texture.Height / gridYDiv;
            int pass = initPass;
            while (pass < maxPass)
            {
                var tasks = new List<Task>();
                passTimer.Restart();
                for (int i = 0; i * yStep < texture.Height; i++)
                {
                    for (int j = 0; j * xStep < texture.Width; j++)
                    {
                        int threadId = id, x0 = j * xStep, y0 = i * yStep, maxX = Math.Min((j + 1) * xStep, texture.Width), maxY = Math.Min((i + 1) * yStep, texture.Height);
                        tasks.Add(Task.Run(() => RenderUtils.PathtracePassDrawArea(threadId, x0, y0, maxX, maxY, raycaster, texture, viewMatrix, projectionMatrix, scene, pass, rendStep)));
                        id++;
                    }
                }
                Task.WaitAll(tasks.ToArray());
                texture.Save("test.rbm");
                texture.SaveToBmp("test.bmp");
                Console.WriteLine($"Pass {pass} completed in {passTimer.ElapsedMilliseconds / 1000} seconds. {DateTime.Now}");
                pass++;
            }
            pathTimer.Stop();
            Console.WriteLine($"Elapsed {pathTimer.ElapsedMilliseconds / 1000} seconds. {DateTime.Now}");
        }

        public static void PathtracePassDrawArea<T>(int id, int x0, int y0, int xf, int yf, Raytracer<MyPTRayPayload, T, MyMaterial<T>> raycaster, Texture2D texture, float4x4 viewMatrix, float4x4 projectionMatrix, Scene<T, MyMaterial<T>> scene, int pass, int step = 1) where T : struct, IVertex<T>, INormalVertex<T>, ICoordinatesVertex<T>, IColorable, ITransformable<T>
        {
            for (int px = x0; px < xf; px += step)
                for (int py = y0; py < yf; py += step)
                {

                    RayDescription ray = RayDescription.FromScreen(px + 0.5f, py + 0.5f, texture.Width, texture.Height, inverse(viewMatrix), inverse(projectionMatrix), 0, 1000);

                    float4 accum = texture.Read(px, py) * pass;
                    MyPTRayPayload coloring = new MyPTRayPayload();
                    coloring.Importance = float3(1, 1, 1);
                    coloring.Bounces = 3;

                    raycaster.Trace(scene, ray, ref coloring);

                    for (int i = 0; i < step; i++)
                    {
                        for (int j = 0; j < step; j++)
                        {
                            texture.Write(Math.Min(px + i, texture.Width - 1), Math.Min(py + j, texture.Height - 1), float4((accum.xyz + coloring.Color) / (pass + 1), 1));
                        }
                    }
                }
            Console.WriteLine($"Done {id}");
        }
    }

    public class GuitarDrawer<T> where T : struct, IVertex<T>, INormalVertex<T>, ICoordinatesVertex<T>
                                           , IColorable, ITransformable<T>
    {
        public static int DrawStep { get; set; } = 1;
        public static int YGrid { get; set; } = 8;
        public static int XGrid { get; set; } = 8;

        public static float3 CameraPosition { get; set; } = float3(1.25f, 1f, -.4f);
        //public static float3 CameraPosition { get; set; } = float3(1.1f, 1f, -.75f); // Forms camera
        //public static float3 CameraPosition { get; set; } = float3(1.1f, 3f, -1.0f); // From top
        //public static float3 CameraPosition { get; set; } = float3(1.1f, .1f, -2.0f); // From below
        //public static float3 CameraPosition { get; set; } = float3(1.23f, .4f, .83f); // Close
        //public static float3 CameraPosition { get; set; } = float3(1f, 1f, 1f);  // Headstock Wall camera
        //public static float3 CameraPosition { get; set; } = float3(1f, 0.05f, .85f);  // Base camera

        public static float3 Target { get; set; } = float3(1.25f, .7f, .5f);
        //public static float3 Target { get; set; } = float3(1.23f, .4f, 2f); // Strings
        //public static float3 Target { get; set; } = LightSources[0].position;
        //public static float3 Target { get; set; } = float3(1.1f, .58f, .5f); // Forms target
        //public static float3 Target { get; set; } = float3(1.4f, 1f, 1f); // Headstock Wall target
        //public static float3 Target { get; set; } = float3(1.4f, 0.05f, .85f); // Base target

        public static float4x4 ViewMatrix { get; set; } = Transforms.LookAtLH(CameraPosition, Target, float3(0, 1, 0));

        public static float4x4 ProjectionMatrix(int height, int width) => Transforms.PerspectiveFovLH(pi_over_4, height / (float)width, 0.01f, 20);

        public static float3 GlobalLightIntensity { get; set; } = float3(1, 1, 1) * 120;
        public static float3 LocalLightIntensity { get; set; } = float3(1, 1, 1) * 800; // Old Pathtracing
        //public static float3 LocalLightIntensity { get; set; } = float3(1, 1, 1) * 20; // Raytracing

        public static (float3 position, float3 intensity, float3 scale)[] LightSources => new (float3 position, float3 intensity, float3 scale)[]
        {
            (float3(1.9f, 2f, 0.3f), LocalLightIntensity, float3(.4f,.1f,.4f)),
            //(Target, GlobalLightIntensity, 5f), 
            //(float3(1f, 1.5f, .5f), GlobalLightIntensity, float3(1f,.1f,1f)), // Light above guitar
            //(float3(1.9f, 1.9f, -1f), .5f*GlobalLightIntensity, float3(1f,.1f,1f)),
        };
        private static GuitarBuilder<T> CreateCSGGuitar(float4x4 worldTransformation)
        {
            var guitar = new GuitarBuilder<T>();
            var mesh = guitar.GuitarMesh();
            float3 lower = mesh.BoundBox.oppositeCorner, upper = mesh.BoundBox.topCorner;
            guitar.CSGWorldTransformation = guitar.StackTransformations(
                Transforms.RotateZ(10 * pi / 180.0f),
                Transforms.RotateX(-pi / 2.0f - 13.5f * pi / 180.0f),
                MyTransforms.FitIn(lower, upper, 1, 1, 1),
                Transforms.Translate(1, .9f, .8f),
                worldTransformation
                );
            return guitar;
        }

        public static void CreateCSGGuitarScene(Scene<T, NoMaterial> scene, float4x4 worldTransformation)
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
                                ,Transforms.RotateZ(0 * pi / 180.0f)
                                ,Transforms.RotateX(-pi / 2.0f - 13.5f * pi / 180.0f)
                                )
                            .FitIn(box, box, box);

            model = model.ApplyTransforms(Transforms.Identity
                                         ,Transforms.Translate(1, .02f, .8f)
                                         //,Transforms.Translate(0, 0, -.1f)
                                         )
                            ;
            model.ComputeNormals();
            
            return model;
        }

        public static Mesh<T> CreateWalls()
        {
            //var wall = Manifold<T>.Surface(4, 4, (x, y) => float3(2 * x, 2 * y, 0));
            //var wall2 = Manifold<T>.Surface(4, 4, (x, y) => float3(2 * x, 2 * y, -0.01f));
            //var floor = Manifold<T>.Surface(4, 4, (x, z) => float3(2 * x, 0, 2 * z));
            //wall = (wall + wall2).ApplyTransforms(Transforms.Translate(0,0,1.06f));

            var wallBuilder = new WallsBuilder<T>();
            //GuitarBuilder<T>.AddColorToMesh(wallBuilder.FloorColor, floor);
            //GuitarBuilder<T>.AddColorToMesh(wallBuilder.WallColor, wall);

            var wall = wallBuilder.WallMesh();
            var floor = wallBuilder.FloorMesh();

            wall.ComputeNormals();
            floor.ComputeNormals();
            
            return wall + floor;
        }

        public static void AddLightSource(Scene<T, MyMaterial<T>> scene)
        {
            var sphereModel = Raycasting.UnitarySphere.AttributesMap(a => new T { Position = a, Coordinates = float2(atan2(a.z, a.x) * 0.5f / pi + 0.5f, a.y), Normal = normalize(a), Color = float3(1, 1, 1) });
            //scene.Add(sphereModel,
            //    GetFrontMainGuitarBodyMaterial(),
            //    //LoadMaterialFromFile("textures\\pin_head_texture.bmp", 0f, 200, 0f, 1, diffuseWeight: 0.4f),
            //    //LoadMaterialFromFile("textures\\circle_texture.bmp", 0.01f, 60, 0.01f, 0),
            //    mul(Transforms.Scale(.3f), Transforms.Translate(Target)));

            //scene.Add(MeshShapeGenerator<T>.Box(30).AsRaycast(), GetFrontMainGuitarBodyMaterial(),
            //var c = new GuitarBuilder<T>().BridgeStringMesh();
            ////MeshShapeGenerator<T>.Cylinder(200, cylinderMat: Materials<T>.MetalStringMaterial,
            ////                                            upFaceMat: Materials<T>.GuitarBodyHoleMaterial,
            ////                                            downFaceMat: Materials<T>.WallMaterial);
            //c = c.ApplyTransforms(Transforms.Scale(.3f, .3f, .3f),
            //                      Transforms.RotateXGrad(90), Transforms.Translate(Target + float3(.1f,-.3f,-.1f)));
            //c.ComputeNormals();
            //foreach (var item in c.MaterialDecompose())
            //{
            //    scene.Add(item.AsRaycast(), (MyMaterial<T>)item.Materials[0], Transforms.Identity);
            //}

            foreach ((var position, var intensity, var scale) in LightSources)
            {
                scene.Add(sphereModel, new MyMaterial<T>
                {
                    Emissive = intensity / (4 * pi), // power per unit area
                    WeightDiffuse = 0,
                    WeightFresnel = 1.0f, // Glass sphere
                    RefractionIndex = 1.0f
                },
                   mul(Transforms.Scale(scale), Transforms.Translate(position)));
            }
        }

        public static void CreateGuitarMeshScene(Scene<T, MyMaterial<T>> scene, float4x4 worldTransformation)
        {
            var model = CreateGuitarMesh();
            var meshes = MyMeshTools.MaterialDecompose(model);
            foreach (var mesh in meshes)
            {
                scene.Add(mesh.AsRaycast(), (MyMaterial<T>)mesh.Materials[0], worldTransformation);
            }

            var model2 = CreateWalls();
            var meshes2 = MyMeshTools.MaterialDecompose(model2);
            //foreach (var mesh in meshes2.Skip(1).Take(1))
            foreach (var mesh in meshes2)
            {
                scene.Add(mesh.AsRaycast(), (MyMaterial<T>)mesh.Materials[0], worldTransformation);
            }

            AddLightSource(scene);
        }

        public static void GuitarMesh<V>(Raster<T,V> render) where V : struct, IProjectedVertex<V>
        {
            render.ClearRT(float4(0, 0, 0.2f, 1)); // clear with color dark blue.

            var primitive = CreateGuitarMesh() + CreateWalls();

            /// Convert to a wireframe to render. Right now only lines can be rasterized.
            primitive = primitive.ConvertTo(Topology.Lines);

            #region viewing and projecting

            // Define a vertex shader that projects a vertex into the NDC.
            render.VertexShader = v =>
            {
                float4 hPosition = float4(v.Position, 1);
                hPosition = mul(hPosition, ViewMatrix);
                hPosition = mul(hPosition, ProjectionMatrix(render.RenderTarget.Height, render.RenderTarget.Width));
                return new V { Homogeneous = hPosition };
            };

            // Define a pixel shader that colors using a constant value
            render.PixelShader = p =>
            {
                return float4(p.Homogeneous.x / 1024.0f, p.Homogeneous.y / 512.0f, 1, 1);
            };

            #endregion

            // Draw the mesh.
            render.DrawMesh(primitive);
        }

        public static void GuitarCSGRaycast(Texture2D texture, float4x4 worldTransformation)
        {
            Raytracer<DefaultRayPayload, T, NoMaterial> raycaster = new Raytracer<DefaultRayPayload, T, NoMaterial>();

            // View and projection matrices
            float4x4 viewMatrix = ViewMatrix;
            float4x4 projectionMatrix = ProjectionMatrix(texture.Height, texture.Width);

            Scene<T, NoMaterial> scene = new Scene<T, NoMaterial>();
            CreateCSGGuitarScene(scene, worldTransformation);

            raycaster.OnClosestHit += delegate (IRaycastContext context, T attribute, NoMaterial material, ref DefaultRayPayload payload)
            {
                payload.Color = material.Color;
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
            //float3 CameraPosition = float3(1.1f, 1f, -.75f);
            //var lightPositionWorld = mul(worldTransformation, float4x1(1.9f, 1.9f, -1f, 0));
            //float3 LightPosition = float3(lightPositionWorld._m00, lightPositionWorld._m10, lightPositionWorld._m20);
            //float3 LightIntensity = float3(1, 1, 1) * 3;
            
            // View and projection matrices
            float4x4 projectionMatrix = ProjectionMatrix(texture.Height, texture.Width);

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
                foreach (var (lightPosition, lightIntentisty, _) in LightSources)
                {
                    float3 L = (lightPosition - attribute.Position);
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

                    float3 Intensity = (shadow.Shadowed ? 0.2f : 1.0f) * lightIntentisty / (d * d);

                    payload.Color += material.EvalBRDF(attribute, V, L) * Intensity * lambertFactor;

                }
            };
            raycaster.OnMiss += delegate (IRaycastContext context, ref DefaultRayPayload payload)
            {
                payload.Color = float3(0, 0, 1); // Blue, as the sky.
            };

            RenderUtils.Draw(texture, raycaster, scene, ViewMatrix, projectionMatrix, DrawStep, XGrid, YGrid);
        }

        public static void GuitarPathtracing(Texture2D texture, float4x4 worldTransformation, int maxPass, int initPass=0)
        {
            // View and projection matrices
            float4x4 projectionMatrix = ProjectionMatrix(texture.Height, texture.Width);

            Scene<T, MyMaterial<T>> scene = new Scene<T, MyMaterial<T>>();
            //CreateMeshScene(scene);
            CreateGuitarMeshScene(scene, worldTransformation);

            Raytracer<MyPTRayPayload, T, MyMaterial<T>> raycaster = new Raytracer<MyPTRayPayload, T, MyMaterial<T>>();
            raycaster.OnClosestHit += delegate (IRaycastContext context, T attribute, MyMaterial<T> material, ref MyPTRayPayload payload)
            {
                // Move geometry attribute to world space
                attribute = attribute.Transform(context.FromGeometryToWorld);

                float3 V = -normalize(context.GlobalRay.Direction);

                attribute.Normal = normalize(attribute.Normal);

                if (material.BumpMap != null)
                {
                    float3 T, B;
                    createOrthoBasis(attribute.Normal, out T, out B);
                    float3 tangentBump = material.BumpMap.Sample(material.TextureSampler, attribute.Coordinates).xyz * 2 - 1;
                    float3 globalBump = tangentBump.x * T + tangentBump.y * B + tangentBump.z * attribute.Normal;
                    //attribute.Normal = globalBump;
                    attribute.Normal = normalize(attribute.Normal + globalBump);
                }

                MyScatteredRay outgoing = material.Scatter(attribute, V);

                float lambertFactor = abs(dot(attribute.Normal, outgoing.Direction));

                payload.Color += payload.Importance * material.Emissive;

                // Recursive calls for indirect light due to reflections and refractions
                if (payload.Bounces > 0)
                {
                    float3 D = outgoing.Direction; // recursive direction to check
                    float3 facedNormal = dot(D, attribute.Normal) > 0 ? attribute.Normal : -attribute.Normal; // normal respect to direction

                    RayDescription ray = new RayDescription { Direction = D, Origin = attribute.Position + facedNormal * 0.001f, MinT = 0.0001f, MaxT = 10000 };

                    payload.Importance *= outgoing.Ratio * lambertFactor / outgoing.PDF;
                    payload.Bounces--;

                    raycaster.Trace(scene, ray, ref payload);
                }
            };
            raycaster.OnMiss += delegate (IRaycastContext context, ref MyPTRayPayload payload)
            {
                payload.Color = float3(0, 0, 0); // Black, as the night.
            };

            RenderUtils.PathTrace(texture, scene, raycaster, ViewMatrix, projectionMatrix, maxPass, DrawStep, XGrid, YGrid, initPass);
        }

        public static void GuitarRaytracing(Texture2D texture, float4x4 worldTransformation)
        {
            // View and projection matrices
            float4x4 projectionMatrix = ProjectionMatrix(texture.Height, texture.Width);

            Scene<T, MyMaterial<T>> scene = new Scene<T, MyMaterial<T>>();
            CreateGuitarMeshScene(scene, worldTransformation);

            // Raycaster to trace rays and check for shadow rays.
            Raytracer<MyShadowRayPayload, T, MyMaterial<T>> shadower = new Raytracer<MyShadowRayPayload, T, MyMaterial<T>>();
            shadower.OnAnyHit += delegate (IRaycastContext context, T attribute, MyMaterial<T> material, ref MyShadowRayPayload payload)
            {
                if (any(material.Emissive))
                    return HitResult.Discard; // Discard light sources during shadow test.

                // If any object is found in ray-path to the light, the ray is shadowed.
                payload.Shadowed = true;
                // No neccessary to continue checking other objects
                return HitResult.Stop;
            };

            // Raycaster to trace rays and lit closest surfaces
            Raytracer<MyRTRayPayload, T, MyMaterial<T>> raycaster = new Raytracer<MyRTRayPayload, T, MyMaterial<T>>();
            raycaster.OnClosestHit += delegate (IRaycastContext context, T attribute, MyMaterial<T> material, ref MyRTRayPayload payload)
            {
                // Move geometry attribute to world space
                attribute = attribute.Transform(context.FromGeometryToWorld);

                float3 V = -normalize(context.GlobalRay.Direction);
                foreach (var (lightPosition, lightIntentisty, _) in LightSources)
                {
                    float3 L = (lightPosition - attribute.Position);
                    float d = length(L);
                    L /= d; // normalize direction to light reusing distance to light

                    attribute.Normal = normalize(attribute.Normal);

                    if (material.BumpMap != null)
                    {
                        float3 T, B;
                        createOrthoBasis(attribute.Normal, out T, out B);
                        float3 tangentBump = material.BumpMap.Sample(material.TextureSampler, attribute.Coordinates).xyz * 2 - 1;
                        float3 globalBump = tangentBump.x * T + tangentBump.y * B + tangentBump.z * attribute.Normal;
                        attribute.Normal = normalize(attribute.Normal + globalBump);
                        //attribute.Normal = globalBump;
                    }

                    var l = dot(attribute.Normal, L);
                    if (l < 0)
                    {
                        l = -l;
                        attribute.Normal *= -1;
                    }
                    float lambertFactor = abs(l);

                    // Check ray to light...
                    MyShadowRayPayload shadow = new MyShadowRayPayload();
                    shadower.Trace(scene,
                        RayDescription.FromDir(attribute.Position + attribute.Normal * 0.001f, // Move an epsilon away from the surface to avoid self-shadowing 
                        L), ref shadow);

                    float3 Intensity = (shadow.Shadowed ? 0.2f : 1.0f) * lightIntentisty / (d * d);

                    //payload.Color = float3((attribute.Normal.x + 1) / 2, (attribute.Normal.y + 1) / 2, (attribute.Normal.z + 1) / 2); // Normal
                    payload.Color += material.Emissive + material.EvalBRDF(attribute, V, L) * Intensity * lambertFactor; // direct light computation

                    // Recursive calls for indirect light due to reflections and refractions
                    if (payload.Bounces > 0)
                        foreach (var impulse in material.GetBRDFImpulses(attribute, V))
                        {
                            float3 D = impulse.Direction; // recursive direction to check
                            float3 facedNormal = dot(D, attribute.Normal) > 0 ? attribute.Normal : -attribute.Normal; // normal respect to direction

                            RayDescription ray = new RayDescription { Direction = D, Origin = attribute.Position + facedNormal * 0.001f, MinT = 0.0001f, MaxT = 10000 };

                            MyRTRayPayload newPayload = new MyRTRayPayload
                            {
                                Bounces = payload.Bounces - 1
                            };

                            raycaster.Trace(scene, ray, ref newPayload);

                            payload.Color += newPayload.Color * impulse.Ratio;
                        }
                }
            };
            raycaster.OnMiss += delegate (IRaycastContext context, ref MyRTRayPayload payload)
            {
                payload.Color = float3(0, 0, 0); // Black, as the night.
            };

            RenderUtils.RayTrace<T>(texture, scene, raycaster, ViewMatrix, projectionMatrix, DrawStep, XGrid, YGrid);
        }

    }
}
