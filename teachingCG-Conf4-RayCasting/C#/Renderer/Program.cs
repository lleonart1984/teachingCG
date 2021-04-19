using GMath;
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
            // Adding elements of the scene
            //var sphere = new CSG.CSGNode(Raycasting.UnitarySphere);
            //var cylinder1 = new CSG.CSGNode(Raycasting.Cylinder(.6f, "xy"));
            //var cylinder2 = new CSG.CSGNode(Raycasting.Cylinder(.6f, "xz"));
            //var cylinder3 = new CSG.CSGNode(Raycasting.Cylinder(.6f, "yz"));
            //var bound = float3(.9f, .9f, .9f);
            //var box = new CSG.CSGNode(Raycasting.Box(-bound, bound));
            //scene.Add((box & sphere) / (cylinder1 | cylinder2 | cylinder3), Transforms.Translate(0, .7f, 0));


            var guitar = new GuitarBuilder();
            var mesh = guitar.GuitarMesh();
            float scale = 3;
            float3 lower = mesh.BoundBox.oppositeCorner, upper = mesh.BoundBox.topCorner;
            guitar.CSGWorldTransformation = guitar.StackTransformations(
                Transforms.RotateZ(pi),
                Transforms.RotateX(-pi / 2),
                Transforms.FitIn(lower, upper, 1, 1, 1),
                Transforms.Scale(scale, scale, scale)
                );
            guitar.Guitar(scene);
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
            model = Manifold<PositionNormal>.Revolution(5, 5, t => EvalBezier(contourn, t), float3(0, 1, 0)).Weld();
            
            model = new GuitarBuilder() { MeshScalar = 1f }.GuitarMesh().ApplyTransforms(Transforms.Identity
                            //model = MeshShapeGenerator<PositionNormal>.Box(4).ApplyTransforms(
                            //Transforms.Scale(2 * 18, 2 * 2, 2 * 30)

                            , Transforms.Translate(0, 1, 22)
                            , Transforms.RotateX(pi / 2.0f)
                            , Transforms.RotateZ(pi)
                            );

            var wallScale = model.BoundBox.topCorner - model.BoundBox.oppositeCorner;
            var floatWallScale = Math.Max(wallScale.x, Math.Max(wallScale.y, wallScale.z));
            var wall = new WallsBuilder() { MeshScalar = 1f }.WallMesh();
            wall = wall.ApplyTransforms(Transforms.Identity
                , Transforms.Translate(-wall.BoundBox.oppositeCorner)
                , Transforms.RotateY(pi)
                , Transforms.Scale(floatWallScale, floatWallScale, floatWallScale)
                );
            wall = wall.ApplyTransforms(
                Transforms.Translate(model.BoundBox.oppositeCorner - wall.BoundBox.oppositeCorner + float3(-100,0,0))
                );
            wall = wall.ApplyTransforms(Transforms.ScaleRespectTo(-wall.BoundBox.oppositeCorner, 3, 3, 3));

            model += wall;

            var scale = 3.5f;
            model = model.FitIn(3, 3, 3)
                         .ApplyTransforms( Transforms.Identity
                                         , Transforms.Translate(-1.6f, -.3f, 0)
                                         , Transforms.RotateY(pi/5)
                                         , Transforms.Scale(scale, scale, scale)
                                         )
                         .Weld();

            model.ComputeNormals();

            return model;
        }

        static void CreateMeshScene(Scene<PositionNormal> scene)
        {
            var model = CreateModel();
            scene.Add(model.AsRaycast(), Transforms.Identity);
        }


        static void RaycastingMesh (Texture2D texture)
        {
            // Scene Setup
            float3 CameraPosition = float3(3, 2f, 4);
            float3 LightPosition = float3(3, 2, 4);
            // View and projection matrices
            float4x4 viewMatrix = Transforms.LookAtLH(CameraPosition, float3(0, 1, 0), float3(0, 1, 0));
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
            //for (int px = 0; px < texture.Width; px++)
            //    for (int py = 0; py < texture.Height; py++)
            //    {
            //        int progress = (px * texture.Height + py);
            //        if (progress % 100 == 0)
            //        {
            //            Console.Write("\r" + progress * 100 / (float)(texture.Width * texture.Height) + "%            ");
            //        }

            //        RayDescription ray = RayDescription.FromScreen(px + 0.5f, py + 0.5f, texture.Width, texture.Height, inverse(viewMatrix), inverse(projectionMatrix), 0, 1000);

            //        MyRayPayload coloring = new MyRayPayload();

            //        raycaster.Trace(scene, ray, ref coloring);

            //        texture.Write(px, py, float4(coloring.Color, 1));
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
        }

        static void Main(string[] args)
        {
            // Texture to output the image.
            Texture2D texture = new Texture2D(512, 512);

            SimpleRaycast(texture);
            //LitRaycast(texture);
            //RaycastingMesh(texture);

            //Raster<PositionNormal, MyProjectedVertex> render = new Raster<PositionNormal, MyProjectedVertex>(texture);
            //GeneratingMeshes(render);

            texture.Save("test.rbm");
            Console.WriteLine("Done.");
            System.Diagnostics.Process.Start("CMD.exe","/C python imageviewer.py test.rbm");
        }

        static void RenderArea<T>(int id, int x0, int y0, int xf, int yf, Raytracer<MyRayPayload, T> raycaster, Texture2D texture, float4x4 viewMatrix, float4x4 projectionMatrix, Scene<T> scene) where T : struct
        {
            for (int px = x0; px < xf; px++)
                for (int py = y0; py < yf; py++)
                {
                    int progress = (px * yf + py);
                    //if (progress % 100 == 0)
                    //{
                    //    Console.WriteLine($"{id}: " + progress * 100 / (xf * yf) + "%            ");
                    //}

                    RayDescription ray = RayDescription.FromScreen(px + 0.5f, py + 0.5f, texture.Width, texture.Height, inverse(viewMatrix), inverse(projectionMatrix), 0, 1000);

                    MyRayPayload coloring = new MyRayPayload();

                    raycaster.Trace(scene, ray, ref coloring);

                    texture.Write(px, py, float4(coloring.Color, 1));
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

            var primitive = CreateModel();

            /// Convert to a wireframe to render. Right now only lines can be rasterized.
            primitive = primitive.ConvertTo(Topology.Lines);

            #region viewing and projecting

            // Scene Setup
            float3 CameraPosition = float3(3, 2f, 4);
            float3 LightPosition = float3(3, 2, 4);
            // View and projection matrices
            float4x4 viewMatrix = Transforms.LookAtLH(CameraPosition, float3(0, 1, 0), float3(0, 1, 0));
            float4x4 projectionMatrix = Transforms.PerspectiveFovLH(pi_over_4, render.RenderTarget.Height / (float)render.RenderTarget.Width, 0.01f, 20);

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
