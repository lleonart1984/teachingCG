﻿using GMath;
using Renderer.Modeling;
using Rendering;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using static GMath.Gfx;
using static Rendering.Raycasting;

namespace Renderer
{
    public class Program
    {

        static void CreateScene(Scene<float3> scene)
        {
            //var sphere = new CSG.CSGNode(Raycasting.UnitarySphere);
            var cylinder1 = new CSG.CSGNode(Raycasting.Cylinder(.6f, "xz"));
            scene.Add(cylinder1, Transforms.Translate(0,0, .9f));

            // Adding elements of the scene
            //var sphere = new CSG.CSGNode(Raycasting.UnitarySphere);
            //var cylinder1 = new CSG.CSGNode(Raycasting.Cylinder(.6f, "xy"));
            //var cylinder2 = new CSG.CSGNode(Raycasting.Cylinder(.6f, "xz"));
            //var cylinder3 = new CSG.CSGNode(Raycasting.Cylinder(.6f, "yz"));
            //var bound = float3(.9f, .9f, .9f);
            //var box = new CSG.CSGNode(Raycasting.Box(-bound, bound));
            //scene.Add((box & sphere) / (cylinder1 | cylinder2 | cylinder3), Transforms.Translate(0, .7f, 0));


            //var guitar = new GuitarBuilder();
            //var mesh = guitar.GuitarMesh();
            //float scale = 3;
            //float3 lower = mesh.BoundBox.oppositeCorner, upper = mesh.BoundBox.topCorner;
            //guitar.CSGWorldTransformation = guitar.StackTransformations(
            //    Transforms.RotateZ(pi),
            //    Transforms.RotateX(-pi / 2),
            //    Transforms.FitIn(lower, upper, 1, 1, 1),
            //    Transforms.Scale(scale, scale, scale)
            //    );
            //guitar.Guitar(scene);
        }

        public struct PositionNormal : INormalVertex<PositionNormal>
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

        static void CreateScene(Scene<PositionNormal> scene)
        {
            // Adding elements of the scene
            scene.Add(Raycasting.UnitarySphere.AttributesMap(a => new PositionNormal { Position = a, Normal = normalize(a) }),
                Transforms.Translate(0, 1, 0));
            scene.Add(Raycasting.PlaneXZ.AttributesMap(a => new PositionNormal { Position = a, Normal = float3(0, 1, 0) }),
                Transforms.Identity);

            // Adding elements of the scene
            //scene.Add(Raycasting.PlaneXZ.AttributesMap(a => new PositionNormal { Position = a, Normal = float3(0, -1, 0) }),
            //    Transforms.Translate(0, 1, 0));
            //scene.Add(Raycasting.PlaneXZ.AttributesMap(a => new PositionNormal { Position = a, Normal = float3(0, 1, 0) }),
            //    Transforms.Identity);
            //scene.Add(Raycasting.PlaneYZ.AttributesMap(a => new PositionNormal { Position = a, Normal = float3(-1, 0, 0) }),
            //    Transforms.Translate(1, 0, 0));
            //scene.Add(Raycasting.PlaneYZ.AttributesMap(a => new PositionNormal { Position = a, Normal = float3(1, 0, 0) }),
            //    Transforms.Identity);
            //scene.Add(Raycasting.PlaneXY.AttributesMap(a => new PositionNormal { Position = a, Normal = float3(0, 0, -1) }),
            //    Transforms.Translate(0, 0, 4));
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

        static void SimpleRaycast(Texture2D texture)
        {
            Raytracer<MyRayPayload, float3> raycaster = new Raytracer<MyRayPayload, float3>();

            // View and projection matrices
            float4x4 viewMatrix = Transforms.LookAtLH(float3(2, 1f, 4), float3(0, 0, 0), float3(0, 1, 0));
            float4x4 projectionMatrix = Transforms.PerspectiveFovLH(pi_over_4, texture.Height / (float)texture.Width, 0.01f, 20);

            Scene<float3> scene = new Scene<float3>();
            CreateScene(scene);

            raycaster.OnClosestHit += delegate (IRaycastContext context, float3 attribute, ref MyRayPayload payload)
            {
                payload.Color = attribute;
            };

            var start = new Stopwatch();
            start.Start();
            //for (float px = 0.5f; px < texture.Width; px++)
            //    for (float py = 0.5f; py < texture.Height; py++)
            //    {
            //        RayDescription ray = RayDescription.FromScreen(px, py, texture.Width, texture.Height, inverse(viewMatrix), inverse(projectionMatrix), 0, 1000);

            //        MyRayPayload coloring = new MyRayPayload();

            //        raycaster.Trace(scene, ray, ref coloring);

            //        texture.Write((int)px, (int)py, float4(coloring.Color, 1));
            //    }

            var tasks = new List<Task>();
            int id = 0, xStep = texture.Width / 8, yStep = texture.Height / 8;
            for (int i = 0; i * yStep < texture.Height; i++)
            {
                for (int j = 0; j * xStep < texture.Width; j++)
                {
                    int threadId = id, x0 = j * xStep, y0 = i * yStep, maxX = Math.Min((j + 1) * xStep, texture.Width), maxY = Math.Min((i + 1) * yStep, texture.Height);
                    tasks.Add(Task.Run(() => RenderArea(threadId, x0, y0, maxX, maxY, raycaster, texture, viewMatrix, projectionMatrix, scene)));
                    id++;
                }
            }
            Task.WaitAll(tasks.ToArray());
            start.Stop();
            Console.WriteLine($"Elapsed {start.ElapsedMilliseconds} milliseconds");
        }

        static void LitRaycast(Texture2D texture)
        {
            // Scene Setup
            float3 CameraPosition = float3(3, 2f, 4);
            float3 LightPosition = float3(3, 5, -2);
            // View and projection matrices
            float4x4 viewMatrix = Transforms.LookAtLH(CameraPosition, float3(0, 1, 0), float3(0, 1, 0));
            float4x4 projectionMatrix = Transforms.PerspectiveFovLH(pi_over_4, texture.Height / (float)texture.Width, 0.01f, 20);

            //// Scene Setup
            //float3 CameraPosition = float3(.5f, .5f, 0);
            //float3 LightPosition = float3(.5f,.5f,.5f);
            //// View and projection matrices
            //float4x4 viewMatrix = Transforms.LookAtLH(CameraPosition, CameraPosition + float3(0, 0, 1), float3(0, 1, 0));
            //float4x4 projectionMatrix = Transforms.Identity; Transforms.PerspectiveFovLH(pi_over_4, texture.Height / (float)texture.Width, 0.01f, 20);

            Scene<PositionNormal> scene = new Scene<PositionNormal>();
            CreateScene(scene);

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
                    RayDescription.FromTo(attribute.Position + attribute.Normal*0.001f, // Move an epsilon away from the surface to avoid self-shadowing 
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
                    RayDescription ray = RayDescription.FromScreen(px + 0.5f, py + 0.5f, texture.Width, texture.Height, inverse(viewMatrix), inverse(projectionMatrix), 0, 1000);

                    MyRayPayload coloring = new MyRayPayload();

                    raycaster.Trace(scene, ray, ref coloring);

                    texture.Write(px, py, float4(coloring.Color, 1));
                }
        }

        static Mesh<PositionNormal> CreateGuitarMeshModel()
        {
            var box = 1;
            var meshScalar = 1f;

            var model = new GuitarBuilder() { MeshScalar = meshScalar }.GuitarMesh().ApplyTransforms(Transforms.Identity
                            //MeshShapeGenerator<PositionNormal>.Box(4).ApplyTransforms(
                            //Transforms.Scale(2 * 18, 2 * 2, 2 * 30)
                            
                            , Transforms.RotateX(-pi / 2.0f - 11.3f * pi / 180.0f)
                            ).FitIn(box,box,box).FitIn(box,box,box);

            model = model.ApplyTransforms(Transforms.Identity
                                       , Transforms.Translate(1, 0, .8f)
                                     //, Transforms.RotateY(pi / 5)
                                     //, Transforms.Scale(scale, scale, scale)
                                         )
                         .Weld();

            model.ComputeNormals();

            return model;
        }

        private static void GuitarRaycast(Texture2D texture)
        {
            //// Scene Setup
            //float3 CameraPosition = float3(1.25f, .4f, .47f);
            //float3 LightPosition = float3(2f, 1f, -.5f);
            //// View and projection matrices
            //float4x4 viewMatrix = Transforms.LookAtLH(CameraPosition, float3(1.25f, .4f, .5f), float3(0, 1, 0));

            // Scene Setup
            float3 CameraPosition = float3(1f, 1f, -1f);
            float3 LightPosition = float3(2f, 1f, -.5f);
            // View and projection matrices
            float4x4 viewMatrix = Transforms.LookAtLH(CameraPosition, float3(1f, .5f, .5f), float3(0, 1, 0));
            float4x4 projectionMatrix = Transforms.PerspectiveFovLH(pi_over_4, texture.Height / (float)texture.Width, 0.01f, 20);

            Scene<PositionNormal> scene = new Scene<PositionNormal>();
            CreateGuitarMeshScene(scene);

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


            var start = new Stopwatch();
            
            start.Start();

            var tasks = new List<Task>();
            int id = 0, xStep = texture.Width / 8, yStep = texture.Height / 8;
            int render_step = 6;
            for (int i = 0; i * yStep < texture.Height; i++)
            {
                for (int j = 0; j * xStep < texture.Width; j++)
                {
                    int threadId = id, x0 = j * xStep, y0 = i * yStep, maxX = Math.Min((j + 1) * xStep, texture.Width), maxY = Math.Min((i + 1) * yStep, texture.Height);
                    tasks.Add(Task.Run(() => RenderArea(threadId, x0, y0, maxX, maxY, raycaster, texture, viewMatrix, projectionMatrix, scene, render_step)));
                    id++;
                }
            }
            Task.WaitAll(tasks.ToArray());
            start.Stop();
            Console.WriteLine($"Elapsed {start.ElapsedMilliseconds} milliseconds");
        }

        static float3 EvalBezier(float3[] control, float t)
        {
            // DeCasteljau
            if (control.Length == 1)
                return control[0]; // stop condition
            float3[] nestedPoints = new float3[control.Length - 1];
            for (int i = 0; i < nestedPoints.Length; i++)
                nestedPoints[i] = lerp(control[i], control[i + 1], t);
            return EvalBezier(nestedPoints, t);
        }

        static Mesh<PositionNormal> CreateModel()
        {
            // Revolution Sample with Bezier
            float3[] contourn =
            {
                float3(0, -.5f,0),
                float3(0.8f, -0.5f,0),
                float3(1f, -0.2f,0),
                float3(0.6f,1,0),
                float3(0,1,0)
            };

            Mesh<PositionNormal> model;

            /// Creates the model using a revolution of a bezier.
            /// Only Positions are updated.
            model = Manifold<PositionNormal>.Revolution(30, 20, t => EvalBezier(contourn, t), float3(0, 1, 0)).Weld();
            //model = Manifold<PositionNormal>.MiddleHoleSurface(20,20, 
            //    float4(0,0,0,0));
            ////float4(.1f,.1f,.1f,.1f));
            //model = MeshShapeGenerator<PositionNormal>.Box(10, 10, 10, 
            //    holeXYUp: true, sepXYUp: float4(.1f, .1f, .1f, .1f));
            model.ComputeNormals();

            return model;
        }

        static Mesh<PositionNormal> CreateSea()
        {
            /// Creates the model using a revolution of a bezier.
            /// Only Positions are updated.
            var model = Manifold<PositionNormal>.Surface(30, 20, (u, v) => 2 * float3(2 * u - 1, sin(u * 15) * 0.02f + cos(v * 13 + u * 16) * 0.03f, 2 * v - 1)).Weld();
            model.ComputeNormals();
            return model;
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

        static void CreateGuitarMeshScene(Scene<PositionNormal> scene)
        {
            var model = CreateGuitarMeshModel();
            scene.Add(model.AsRaycast(), Transforms.Identity);
            var wall = Manifold<PositionNormal>.Surface(4, 4, (x, y) => float3(2 * x, 2 * y, 0));
            var floor = Manifold<PositionNormal>.Surface(4, 4, (x, z) => float3(2 * x, 0, 2 * z));
            wall.ComputeNormals();
            floor.ComputeNormals();
            scene.Add(wall.AsRaycast(), Transforms.Translate(0,0,1));
            scene.Add(floor.AsRaycast(), Transforms.Identity);
        }

        static void CreateMeshScene(Scene<PositionNormal> scene)
        {
            var sea = CreateSea();
            scene.Add(sea.AsRaycast(RaycastingMeshMode.Grid), Transforms.Identity);

            var egg = CreateModel();
            scene.Add(egg.AsRaycast(RaycastingMeshMode.Grid), Transforms.Translate(0, 0.5f, 0));
        }

        static void RaycastingMesh (Texture2D texture)
        {
            // Scene Setup
            float3 CameraPosition = float3(3, 2f, 4);
            float3 LightPosition = float3(3, 5, -2);
            float3 LightIntensity = float3(1, 1, 1) * 100;

            // View and projection matrices
            float4x4 viewMatrix = Transforms.LookAtLH(CameraPosition, float3(0, 1, 0), float3(0, 1, 0));
            float4x4 projectionMatrix = Transforms.PerspectiveFovLH(pi_over_4, texture.Height / (float)texture.Width, 0.01f, 20);

            Scene<PositionNormal> scene = new Scene<PositionNormal>();
            CreateMeshScene(scene);

            BRDF[] brdfs =
            {
                LambertBRDF(float3(0.4f,0.5f,1f)),  // see
                Mixture(LambertBRDF(float3(0.7f,0.7f,0.3f)), BlinnBRDF(float3(1,1,1), 70), 0.3f), // egg
            };

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

                payload.Color = brdfs[context.GeometryIndex](N, L, V) * Intensity * lambertFactor;
            };
            raycaster.OnMiss += delegate (IRaycastContext context, ref MyRayPayload payload)
            {
                payload.Color = float3(0, 0, 1); // Blue, as the sky.
            };

            /// Render all points of the screen
            var tasks = new List<Task>();
            int id = 0, xStep = texture.Width / 8, yStep = texture.Height / 8;
            int step = 3;
            for (int i = 0; i * yStep < texture.Height; i++)
            {
                for (int j = 0; j * xStep < texture.Width; j++)
                {
                    int threadId = id, x0 = j * xStep, y0 = i * yStep, maxX = Math.Min((j + 1) * xStep, texture.Width), maxY = Math.Min((i + 1) * yStep, texture.Height);
                    tasks.Add(Task.Run(() => RenderArea(threadId, x0, y0, maxX, maxY, raycaster, texture, viewMatrix, projectionMatrix, scene, step)));
                    id++;
                }
            }
            Task.WaitAll(tasks.ToArray());
        }

        static void Main(string[] args)
        {
            // Texture to output the image.
            Texture2D texture = new Texture2D(512, 512);

            //SimpleRaycast(texture);
            //LitRaycast(texture);
            RaycastingMesh(texture);
            //GuitarRaycast(texture);

            //Raster<PositionNormal, MyProjectedVertex> render = new Raster<PositionNormal, MyProjectedVertex>(texture);
            //GeneratingMeshes(render);

            texture.Save("test.rbm");
            Console.WriteLine("Done.");
            System.Diagnostics.Process.Start("CMD.exe","/C python imageviewer.py test.rbm");
        }

        static void RenderArea<T>(int id, int x0, int y0, int xf, int yf, Raytracer<MyRayPayload, T> raycaster, Texture2D texture, float4x4 viewMatrix, float4x4 projectionMatrix, Scene<T> scene, int step = 1) where T : struct
        {
            for (int px = x0; px < xf; px+=step)
                for (int py = y0; py < yf; py+=step)
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
                            texture.Write(Math.Min(px + i,texture.Width-1), Math.Min(py + j, texture.Height-1), float4(coloring.Color, 1));
                        }
                    }
                }
            Console.WriteLine($"Done {id}");
        }

        public struct MyProjectedVertex : IProjectedVertex<MyProjectedVertex>
        {
            public float4 Homogeneous { get; set; }

            public MyProjectedVertex Add(MyProjectedVertex other)
            {
                return new MyProjectedVertex
                {
                    Homogeneous = this.Homogeneous + other.Homogeneous
                };
            }

            public MyProjectedVertex Mul(float s)
            {
                return new MyProjectedVertex
                {
                    Homogeneous = this.Homogeneous * s
                };
            }
        }

        private static void GeneratingMeshes(Raster<PositionNormal, MyProjectedVertex> render)
        {
            render.ClearRT(float4(0, 0, 0.2f, 1)); // clear with color dark blue.

            //var primitive = CreateModel();
            var primitive = CreateGuitarMeshModel();

            /// Convert to a wireframe to render. Right now only lines can be rasterized.
            primitive = primitive.ConvertTo(Topology.Lines);

            #region viewing and projecting


            // Scene Setup
            float3 CameraPosition = float3(1.25f, .4f, .47f);
            float3 LightPosition = float3(2f, 1f, -.5f);
            // View and projection matrices
            float4x4 viewMatrix = Transforms.LookAtLH(CameraPosition, float3(1.25f, .4f, .5f), float3(0, 1, 0));
            float4x4 projectionMatrix = Transforms.PerspectiveFovLH(pi_over_4, render.RenderTarget.Height / (float)render.RenderTarget.Width, 0.01f, 20);

            //// Scene Setup
            //float3 CameraPosition = float3(3, 2f, 4);
            //float3 LightPosition = float3(3, 2, 4);
            //// View and projection matrices
            //float4x4 viewMatrix = Transforms.LookAtLH(CameraPosition, float3(0, 1, 0), float3(0, 1, 0));
            //float4x4 projectionMatrix = Transforms.PerspectiveFovLH(pi_over_4, render.RenderTarget.Height / (float)render.RenderTarget.Width, 0.01f, 20);

            // Define a vertex shader that projects a vertex into the NDC.
            render.VertexShader = v =>
            {
                float4 hPosition = float4(v.Position, 1);
                hPosition = mul(hPosition, viewMatrix);
                hPosition = mul(hPosition, projectionMatrix);
                return new MyProjectedVertex { Homogeneous = hPosition };
            };

            // Define a pixel shader that colors using a constant value
            render.PixelShader = p =>
            {
                return float4(p.Homogeneous.x / render.RenderTarget.Width, p.Homogeneous.y / render.RenderTarget.Height, 1, 1);
            };

            #endregion

            // Draw the mesh.
            render.DrawMesh(primitive);
        }
    }

}
