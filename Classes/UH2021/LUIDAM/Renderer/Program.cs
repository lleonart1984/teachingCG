using GMath;
using Renderer.Modeling;
using Rendering;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using static GMath.Gfx;

namespace Renderer
{
    public class Program
    {

        static void CreateScene(Scene<float3, Material> scene)
        {
            // Adding elements of the scene
            scene.Add(Raycasting.UnitarySphere, new Material(), Transforms.Translate(0,1,0));
            scene.Add(Raycasting.PlaneXZ, new Material(), Transforms.Identity);
        }

        public struct PositionNormal : INormalVertex<PositionNormal>
        {
            public float3 Position { get; set; }
            public float3 Normal { get; set; }
            public float3 Color { get; set; }

            public PositionNormal Add(PositionNormal other)
            {
                return new PositionNormal
                {
                    Position = this.Position + other.Position,
                    Normal = this.Normal + other.Normal,
                    Color = this.Color
                };
            }

            public PositionNormal Mul(float s)
            {
                return new PositionNormal
                {
                    Position = this.Position * s,
                    Normal = this.Normal * s,
                    Color = this.Color
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
                    Normal = n.xyz,
                    Color = this.Color
                };
            }
        }

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

        struct MyProjectedVertex : IProjectedVertex<MyProjectedVertex>
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

        public struct Impulse
        {
            public float3 Direction;
            public float3 Ratio;
        }

        public struct ScatteredRay
        {
            public float3 Direction;
            public float3 Ratio;
            public float PDF;
        }

        public struct Material : IMaterial
        {
            public float3 Emissive;

            public Texture2D DiffuseMap;
            public Texture2D BumpMap;
            public Sampler TextureSampler;

            public float3 Diffuse;
            public float3 Specular;
            public float SpecularPower;
            public float RefractionIndex;

            // 4 float values with Diffuseness, Glossyness, Mirrorness, Fresnelness
            public float WeightDiffuse { get { return 1 - OneMinusWeightDiffuse; } set { OneMinusWeightDiffuse = 1 - value; } }
            float OneMinusWeightDiffuse; // This is intended for default values of the struct to work as 1, 0, 0, 0 weight initial settings
            public float WeightGlossy;
            public float WeightMirror;
            public float WeightFresnel;

            public float WeightNormalization
            {
                get { return max(0.0001f, WeightDiffuse + WeightGlossy + WeightMirror + WeightFresnel); }
            }

            public float3 EvalBRDF(PositionNormalCoordinate surfel, float3 wout, float3 win)
            {
                float3 diffuse = Diffuse * (DiffuseMap == null ? float3(1, 1, 1) : DiffuseMap.Sample(TextureSampler, surfel.Coordinates).xyz) / pi;
                float3 H = normalize(win + wout);
                float3 specular = Specular * pow(max(0, dot(H, surfel.Normal)), SpecularPower) * (SpecularPower + 2) / two_pi;
                return diffuse * WeightDiffuse / WeightNormalization + specular * WeightGlossy / WeightNormalization;
            }

            // Compute fresnel reflection component given the cosine of input direction and refraction index ratio.
            // Refraction can be obtained subtracting to one.
            // Uses the Schlick's approximation
            float ComputeFresnel(float NdotL, float ratio)
            {
                float f = pow((1 - ratio) / (1 + ratio), 2);
                return (f + (1.0f - f) * pow((1.0f - NdotL), 5));
            }

            public IEnumerable<Impulse> GetBRDFImpulses(PositionNormalCoordinate surfel, float3 wout)
            {
                if (!any(Specular))
                    yield break; // No specular => Ratio == 0

                float NdotL = dot(surfel.Normal, wout);
                // Check if ray is entering the medium or leaving
                bool entering = NdotL > 0;

                // Invert all data if leaving
                NdotL = entering ? NdotL : -NdotL;
                surfel.Normal = entering ? surfel.Normal : -surfel.Normal;
                float ratio = entering ? 1.0f / this.RefractionIndex : this.RefractionIndex / 1.0f; // 1.0f air refraction index approx

                // Reflection vector
                float3 R = reflect(wout, surfel.Normal);

                // Refraction vector
                float3 T = refract(wout, surfel.Normal, ratio);

                // Reflection quantity, (1 - F) will be the refracted quantity.
                float F = ComputeFresnel(NdotL, ratio);

                if (!any(T))
                    F = 1; // total internal reflection (produced with critical angles)

                if (WeightMirror + WeightFresnel * F > 0) // something is reflected
                    yield return new Impulse
                    {
                        Direction = R,
                        Ratio = Specular * (WeightMirror + WeightFresnel * F) / WeightNormalization
                    };

                if (WeightFresnel * (1 - F) > 0) // something to refract
                    yield return new Impulse
                    {
                        Direction = T,
                        Ratio = Specular * WeightFresnel * (1 - F) / WeightNormalization
                    };
            }

            /// <summary>
            /// Scatter a ray using the BRDF and Impulses
            /// </summary>
            public ScatteredRay Scatter(PositionNormalCoordinate surfel, float3 w)
            {
                float selection = random();
                float impulseProb = 0;

                foreach (var impulse in GetBRDFImpulses(surfel, w))
                {
                    float pdf = (impulse.Ratio.x + impulse.Ratio.y + impulse.Ratio.z) / 3;
                    if (selection < pdf) // this impulse is choosen
                        return new ScatteredRay
                        {
                            Ratio = impulse.Ratio / abs(dot(surfel.Normal, impulse.Direction)),
                            Direction = impulse.Direction,
                            PDF = pdf
                        };
                    selection -= pdf;
                    impulseProb += pdf;
                }

                float3 wout = randomHSDirection(surfel.Normal);
                /// BRDF uniform sampling
                return new ScatteredRay
                {
                    Direction = wout,
                    Ratio = EvalBRDF(surfel, wout, w),
                    PDF = (1 - impulseProb) / (2 * pi)
                };
            }

        }

        /// <summary>
        /// Payload used to pick a color from a hit intersection
        /// </summary>
        struct RTRayPayload
        {
            public float3 Color;
            public int Bounces; // Maximum value of allowed bounces
        }

        struct PTRayPayload
        {
            public float3 Color; // Accumulated color to the viewer
            public float3 Importance; // Importance of the ray to the viewer
            public int Bounces; // Maximum value of allowed bounces
        }


        #region Textures

        static void CreateRaycastScene(Scene<PositionNormalCoordinate, Material> scene, Material mat)
        {
            Texture2D ballTexture = new Texture2D(1, 1);
            ballTexture.Write(0, 0, float4(1, 1, 0, 1)); // yellow color

            Texture2D planeTexture = new Texture2D(2, 2);
            planeTexture.Write(0, 0, float4(1, 0, 0, 1)); // red cell
            planeTexture.Write(0, 1, float4(1, 1, 0, 1)); // yellow cell
            planeTexture.Write(1, 0, float4(0, 1, 0, 1)); // green cell
            planeTexture.Write(1, 1, float4(0, 0, 1, 1)); // blue cell

            // Adding elements of the scene
            scene.Add(Raycasting.UnitarySphere.AttributesMap(a => new PositionNormalCoordinate { Position = a, Coordinates = float2(atan2(a.z, a.x) * 0.5f / pi + 0.5f, a.y), Normal = normalize(a) }),
                mat,
                Transforms.Translate(0, 1, 0));
            scene.Add(Raycasting.PlaneXZ.AttributesMap(a => new PositionNormalCoordinate { Position = a, Coordinates = float2(a.x, a.z), Normal = float3(0, 1, 0) }),
                new Material { DiffuseMap = planeTexture, TextureSampler = new Sampler { Wrap = WrapMode.Repeat } },
                Transforms.Identity);
        }

        static Mesh<PositionNormalCoordinate> CreateModelTexture()
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

            /// Creates the model using a revolution of a bezier.
            /// Only Positions are updated.
            var model = Manifold<PositionNormalCoordinate>.Revolution(20, 30, t => EvalBezier(contourn, t), float3(0, 1, 0)).Weld();
            model.ComputeNormals();
            return model;
        }

        static Mesh<PositionNormalCoordinate> CreateSeaTexture()
        {
            /// Creates the model using a revolution of a bezier.
            /// Only Positions are updated.
            //var model = Manifold<PositionNormalCoordinate>.Surface(30, 20, (u, v) => 2 * float3(2 * u - 1, sin(u * 15) * 0.0f + cos(v * 13 + u * 16) * 0.0f, 2 * v - 1));//.Weld();
            var model = Manifold<PositionNormalCoordinate>.Surface(30, 20, (u, v) => 2 * float3(2 * u - 1, 0.0f, 2 * v - 1));//.Weld();
            model.ComputeNormals();
            return model;
        }

        static void CreateMeshScene(Scene<PositionNormalCoordinate, Material> scene)
        {
            Texture2D ballTexture = new Texture2D(1, 1);
            ballTexture.Write(0, 0, float4(1, 1, 0, 1)); // yellow color

            Texture2D planeTexture = new Texture2D(2, 2);
            planeTexture.Write(0, 0, float4(1, 0, 0, 1)); // red cell
            planeTexture.Write(0, 1, float4(1, 1, 0, 1)); // yellow cell
            planeTexture.Write(1, 0, float4(0, 1, 0, 1)); // green cell
            planeTexture.Write(1, 1, float4(0, 0, 1, 1)); // blue cell

            var sea = CreateSeaTexture();
            scene.Add(sea.AsRaycast(RaycastingMeshMode.Grid), new Material
            {
                DiffuseMap = planeTexture,
                Specular = float3(1, 1, 1),
                SpecularPower = 60,
                WeightGlossy = 0.2f,
                TextureSampler = new Sampler
                {
                    Wrap = WrapMode.Repeat,
                    MinMagFilter = Filter.Point
                }
            }, Transforms.Identity);

            var egg = CreateModelTexture();
            scene.Add(egg.AsRaycast(RaycastingMeshMode.Grid), new Material
            {
                DiffuseMap = ballTexture,
                Specular = float3(1, 1, 1),
                SpecularPower = 60,
                WeightGlossy = 0.2f,
                TextureSampler = new Sampler
                {
                    Wrap = WrapMode.Clamp,
                    MipFilter = Filter.Linear
                }
            }, Transforms.Translate(0, 0.5f, 0));
        }

        static void RaycastingMeshTexture(Texture2D texture, Material mat)
        {
            // Scene Setup
            float3 CameraPosition = float3(3, 2f, 4);
            float3 LightPosition = float3(3, 5, -2);
            float3 LightIntensity = float3(1, 1, 1) * 100;

            // View and projection matrices
            float4x4 viewMatrix = Transforms.LookAtLH(CameraPosition, float3(0, 1, 0), float3(0, 1, 0));
            float4x4 projectionMatrix = Transforms.PerspectiveFovLH(pi_over_4, texture.Height / (float)texture.Width, 0.01f, 20);

            Scene<PositionNormalCoordinate, Material> scene = new Scene<PositionNormalCoordinate, Material>();
            CreateMeshScene(scene);
            //CreateRaycastScene(scene, mat);

            // Raycaster to trace rays and check for shadow rays.
            Raytracer<ShadowRayPayload, PositionNormalCoordinate, Material> shadower = new Raytracer<ShadowRayPayload, PositionNormalCoordinate, Material>();
            shadower.OnAnyHit += delegate (IRaycastContext context, PositionNormalCoordinate attribute, Material material, ref ShadowRayPayload payload)
            {
                // If any object is found in ray-path to the light, the ray is shadowed.
                payload.Shadowed = true;
                // No neccessary to continue checking other objects
                return HitResult.Stop;
            };

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

                attribute.Normal = normalize(attribute.Normal);

                float lambertFactor = max(0, dot(attribute.Normal, L));

                // Check ray to light...
                ShadowRayPayload shadow = new ShadowRayPayload();
                shadower.Trace(scene,
                    RayDescription.FromDir(attribute.Position + attribute.Normal * 0.001f, // Move an epsilon away from the surface to avoid self-shadowing 
                    L), ref shadow);

                float3 Intensity = (shadow.Shadowed ? 0.2f : 1.0f) * LightIntensity / (d * d);

                payload.Color = material.EvalBRDF(attribute, V, L) * Intensity * lambertFactor;
            };
            raycaster.OnMiss += delegate (IRaycastContext context, ref MyRayPayload payload)
            {
                payload.Color = float3(0, 0, 0); // Blue, as the sky.
            };

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

                    raycaster.Trace(scene, ray, ref coloring);

                    texture.Write(px, py, float4(coloring.Color, 1));
                }
        }

        #endregion

        #region Meshes

        private static void GeneratingMeshes<V>(Raster<PositionNormal, V> render) where V : struct, IProjectedVertex<V>
        {
            render.ClearRT(float4(0, 0, 0.2f, 1)); // clear with color dark blue.

            var primitive = CreateModel();

            primitive = primitive.Expand();
            //primitive = primitive.Expand();

            /// Convert to a wireframe to render. Right now only lines can be rasterized.
            primitive = primitive.ConvertTo(Topology.Lines);

            #region viewing and projecting

            float4x4 viewMatrix = Transforms.LookAtLH(float3(2, 1f, 2), float3(0, 0, 0), float3(0, 1, 0));
            float4x4 projectionMatrix = Transforms.PerspectiveFovLH(pi_over_4, render.RenderTarget.Height / (float)render.RenderTarget.Width, 0.01f, 20);

            // Define a vertex shader that projects a vertex into the NDC.
            render.VertexShader = v =>
            {
                float4 hPosition = float4(v.Position, 1);
                hPosition = mul(hPosition, viewMatrix);
                hPosition = mul(hPosition, projectionMatrix);
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

        #endregion

        #region LightTransport

        static float3 CameraPosition = float3(2, 4f, 4);
        static float3 LightPosition = float3(3, 5, -2);
        static float3 LightIntensity = float3(1, 1, 1) * 100;

        static void Raytracing(Texture2D texture)
        {
            // View and projection matrices
            float4x4 viewMatrix = Transforms.LookAtLH(CameraPosition, float3(0, 1, 0), float3(0, 1, 0));
            float4x4 projectionMatrix = Transforms.PerspectiveFovLH(pi_over_4, texture.Height / (float)texture.Width, 0.01f, 20);

            Scene<PositionNormalCoordinate, Material> scene = new Scene<PositionNormalCoordinate, Material>();
            //CreateMeshScene(scene);
            CreateRaycastScene(scene);

            // Raycaster to trace rays and check for shadow rays.
            Raytracer<ShadowRayPayload, PositionNormalCoordinate, Material> shadower = new Raytracer<ShadowRayPayload, PositionNormalCoordinate, Material>();
            shadower.OnAnyHit += delegate (IRaycastContext context, PositionNormalCoordinate attribute, Material material, ref ShadowRayPayload payload)
            {
                if (any(material.Emissive))
                    return HitResult.Discard; // Discard light sources during shadow test.

                // If any object is found in ray-path to the light, the ray is shadowed.
                payload.Shadowed = true;
                // No neccessary to continue checking other objects
                return HitResult.Stop;
            };

            // Raycaster to trace rays and lit closest surfaces
            Raytracer<MyRayPayload, PositionNormalCoordinate, Material> raycaster = new Raytracer<MyRayPayload, PositionNormalCoordinate, Material>();
            raycaster.OnClosestHit += delegate (IRaycastContext context, PositionNormalCoordinate attribute, Material material, ref MyRayPayload payload)
            {
                // Move geometry attribute to world space
                attribute = attribute.Transform(context.FromGeometryToWorld);

                float3 V = -normalize(context.GlobalRay.Direction);

                float3 L = (LightPosition - attribute.Position);
                float d = length(L);
                L /= d; // normalize direction to light reusing distance to light

                attribute.Normal = normalize(attribute.Normal);

                if (material.BumpMap != null)
                {
                    float3 T, B;
                    createOrthoBasis(attribute.Normal, out T, out B);
                    float3 tangentBump = material.BumpMap.Sample(material.TextureSampler, attribute.Coordinates).xyz * 2 - 1;
                    float3 globalBump = tangentBump.x * T + tangentBump.y * B + tangentBump.z * attribute.Normal;
                    //attribute.Normal = globalBump;
                    attribute.Normal = normalize(attribute.Normal + globalBump );
                }

                float lambertFactor = max(0, dot(attribute.Normal, L));

                // Check ray to light...
                ShadowRayPayload shadow = new ShadowRayPayload();
                shadower.Trace(scene,
                    RayDescription.FromDir(attribute.Position + attribute.Normal * 0.001f, // Move an epsilon away from the surface to avoid self-shadowing 
                    L), ref shadow);

                float3 Intensity = (shadow.Shadowed ? 0.2f : 1.0f) * LightIntensity / (d * d);

                payload.Color = material.Emissive + material.EvalBRDF(attribute, V, L) * Intensity * lambertFactor; // direct light computation

                // Recursive calls for indirect light due to reflections and refractions
                if (payload.Bounces > 0)
                    foreach (var impulse in material.GetBRDFImpulses(attribute, V))
                    {
                        float3 D = impulse.Direction; // recursive direction to check
                        float3 facedNormal = dot(D, attribute.Normal) > 0 ? attribute.Normal : -attribute.Normal; // normal respect to direction

                        RayDescription ray = new RayDescription { Direction = D, Origin = attribute.Position + facedNormal * 0.001f, MinT = 0.0001f, MaxT = 10000 };

                        MyRayPayload newPayload = new MyRayPayload
                        {
                            Bounces = payload.Bounces - 1
                        };

                        raycaster.Trace(scene, ray, ref newPayload);

                        payload.Color += newPayload.Color * impulse.Ratio;
                    }
            };
            raycaster.OnMiss += delegate (IRaycastContext context, ref MyRayPayload payload)
            {
                payload.Color = float3(0, 0, 0); // Blue, as the sky.
            };

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
                    coloring.Bounces = 3;

                    raycaster.Trace(scene, ray, ref coloring);

                    texture.Write(px, py, float4(coloring.Color, 1));
                }
        }

        static void CreateRaycastScene(Scene<PositionNormalCoordinate, Material> scene)
        {
            Texture2D planeTexture = Texture2D.LoadFromFile("wood.jpeg");

            var sphereModel = Raycasting.UnitarySphere.AttributesMap(a => new PositionNormalCoordinate { Position = a, Coordinates = float2(atan2(a.z, a.x) * 0.5f / pi + 0.5f, a.y), Normal = normalize(a) });

            // Adding elements of the scene
            scene.Add(sphereModel, new Material
            {
                Specular = float3(1, 1, 1),
                SpecularPower = 260,

                WeightDiffuse = 0,
                WeightFresnel = 1.0f, // Glass sphere
                RefractionIndex = 1.6f
            },
                Transforms.Translate(0, 1, -1.5f));

            scene.Add(sphereModel, new Material
            {
                Specular = float3(1, 1, 1),
                SpecularPower = 260,

                WeightDiffuse = 0,
                WeightMirror = 1.0f, // Mirror sphere
            },
                Transforms.Translate(1.5f, 1, 0));


            Texture2D boxDiffuse = Texture2D.LoadFromFile("textures\\wall_texture.bmp");
            Materials<MyPositionNormalCoordinate>.CreateNoisyBumpMap("noisy.bmp", 0.05f, 400, 400);
            Materials<MyPositionNormalCoordinate>.CreateRoughStringBumpMap("rough.bmp", 10, 400, 400);
            var boxBump = Texture2D.LoadFromFile("noisy.bmp");
            boxBump = Texture2D.LoadFromFile("rough.bmp");
            //boxBump = null;

            var material = new Material
            {
                DiffuseMap = boxDiffuse,
                BumpMap = boxBump,
                Diffuse = float3(1, 1, 1),
                Specular = float3(1, 1, 1) * .3f,
                SpecularPower = 60,
                WeightGlossy = .02f,
                WeightFresnel = 0f,
                TextureSampler = new Sampler { Wrap = WrapMode.Repeat, MinMagFilter = Filter.Linear },
            };

            scene.Add(sphereModel, new Material
            {
                Specular = float3(1, 1, 1) * 0.1f,
                SpecularPower = 60,
                Diffuse = float3(1, 1, 1)
            },
                Transforms.Translate(-1.5f, 1, 0));

            var mesh = MeshShapeGenerator<PositionNormalCoordinate>.Box(5,5,5,false,false, false, false,false,allMat:material);
            var normal = float3(1, 0, 0);
            scene.Add(Raycasting.PlaneYZ.AttributesMap(a => new PositionNormalCoordinate 
            {
                Position = a,
                Coordinates = float2(a.y, a.z),
                Normal = normal
            }), 
                      material, 
                      Transforms.Translate(-1.5f, 1, 0));

            scene.Add(Raycasting.PlaneXZ.AttributesMap(a => new PositionNormalCoordinate { Position = a, Coordinates = float2(a.x * 0.2f, a.z * 0.2f), Normal = float3(0, 1, 0) }),
                new Material { DiffuseMap = planeTexture, Diffuse = float3(1, 1, 1), TextureSampler = new Sampler { Wrap = WrapMode.Repeat, MinMagFilter = Filter.Linear } },
                Transforms.Identity);

            // Light source
            scene.Add(sphereModel, new Material
            {
                Emissive = LightIntensity / (4 * pi), // power per unit area
                WeightDiffuse = 0,
                WeightFresnel = 1.0f, // Glass sphere
                RefractionIndex = 1.0f
            },
               mul(Transforms.Scale(2.4f,0.4f,2.4f), Transforms.Translate(LightPosition)));
        }

        #endregion

        #region Pathtracing

        static void Pathtracing(Texture2D texture, int pass)
        {
            // View and projection matrices
            float4x4 viewMatrix = Transforms.LookAtLH(CameraPosition, float3(0, 1, 0), float3(0, 1, 0));
            float4x4 projectionMatrix = Transforms.PerspectiveFovLH(pi_over_4, texture.Height / (float)texture.Width, 0.01f, 20);

            Scene<PositionNormalCoordinate, Material> scene = new Scene<PositionNormalCoordinate, Material>();
            //CreateMeshScene(scene);
            CreateRaycastScene(scene);

            // Raycaster to trace rays and lit closest surfaces
            Raytracer<PTRayPayload, PositionNormalCoordinate, Material> raycaster = new Raytracer<PTRayPayload, PositionNormalCoordinate, Material>();
            raycaster.OnClosestHit += delegate (IRaycastContext context, PositionNormalCoordinate attribute, Material material, ref PTRayPayload payload)
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
                    attribute.Normal = globalBump;// normalize(attribute.Normal + globalBump * 5f);
                }

                ScatteredRay outgoing = material.Scatter(attribute, V);

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
            raycaster.OnMiss += delegate (IRaycastContext context, ref PTRayPayload payload)
            {
                payload.Color = float3(0, 0, 0); // Blue, as the sky.
            };

            /// Render all points of the screen
            for (int px = 0; px < texture.Width; px++)
                for (int py = 0; py < texture.Height; py++)
                {
                    int progress = (px * texture.Height + py);
                    if (progress % 10000 == 0)
                    {
                        Console.Write("\r" + progress * 100 / (float)(texture.Width * texture.Height) + "%            ");
                    }

                    RayDescription ray = RayDescription.FromScreen(px + 0.5f, py + 0.5f, texture.Width, texture.Height, inverse(viewMatrix), inverse(projectionMatrix), 0, 1000);

                    float4 accum = texture.Read(px, py) * pass;
                    PTRayPayload coloring = new PTRayPayload();
                    coloring.Importance = float3(1, 1, 1);
                    coloring.Bounces = 3;

                    raycaster.Trace(scene, ray, ref coloring);

                    texture.Write(px, py, float4((accum.xyz + coloring.Color) / (pass + 1), 1));
                }
        }

        static void PathtracingInit(Texture2D texture, int maxPass)
        {
            int pass = 0;
            while (pass < maxPass)
            {
                Console.WriteLine("Pass: " + pass);
                Pathtracing(texture, pass);
                texture.Save("test.rbm");
                pass++;
            }
        }

        #endregion

        #region Raycasting

        static void CreateScene(Scene<PositionNormal, Material> scene)
        {
            // Adding elements of the scene
            scene.Add(Raycasting.UnitarySphere.AttributesMap(a => new PositionNormal { Position = a, Normal = normalize(a) })
                , new Material(), Transforms.Translate(0, 1, 0));
            scene.Add(Raycasting.PlaneXZ.AttributesMap(a => new PositionNormal { Position = a, Normal = float3(0, 1, 0) })
                , new Material(), Transforms.Identity);
        }

        /// <summary>
        /// Payload used to pick a color from a hit intersection
        /// </summary>
        public struct MyRayPayload
        {
            public float3 Color;
            public int Bounces; // Maximum value of allowed bounces
        }

        /// <summary>
        /// Payload used to flag when a ray was shadowed.
        /// </summary>
        public struct ShadowRayPayload
        {
            public bool Shadowed;
        }

        static void SimpleRaycast(Texture2D texture)
        {
            Raytracer<MyRayPayload, float3, Material> raycaster = new Raytracer<MyRayPayload, float3, Material>();

            // View and projection matrices
            float4x4 viewMatrix = Transforms.LookAtLH(float3(2, 1f, 4), float3(0, 0, 0), float3(0, 1, 0));
            float4x4 projectionMatrix = Transforms.PerspectiveFovLH(pi_over_4, texture.Height / (float)texture.Width, 0.01f, 20);

            Scene<float3, Material> scene = new Scene<float3, Material>();
            CreateScene(scene);

            raycaster.OnClosestHit += delegate (IRaycastContext context, float3 attribute, Material material, ref MyRayPayload payload)
            {
                payload.Color = attribute;
            };

            for (float px = 0.5f; px < texture.Width; px++)
                for (float py = 0.5f; py < texture.Height; py++)
                {
                    RayDescription ray = RayDescription.FromScreen(px, py, texture.Width, texture.Height, inverse(viewMatrix), inverse(projectionMatrix), 0, 1000);

                    MyRayPayload coloring = new MyRayPayload();

                    raycaster.Trace(scene, ray, ref coloring);

                    texture.Write((int)px, (int)py, float4(coloring.Color, 1));
                }
        }

        static void LitRaycast(Texture2D texture)
        {
            // Scene Setup
            float3 CameraPosition = float3(3, 2f, 4);
            float3 LightPosition = float3(3, 5, -2);
            // View and projection matrices
            float4x4 viewMatrix = Transforms.LookAtLH(CameraPosition, float3(0, 1, 0), float3(0, 1, 0));
            float4x4 projectionMatrix = Transforms.PerspectiveFovLH(pi_over_4, texture.Height / (float)texture.Width, 0.01f, 20);

            Scene<PositionNormal, Material> scene = new Scene<PositionNormal, Material>();
            CreateScene(scene);

            // Raycaster to trace rays and check for shadow rays.
            Raytracer<ShadowRayPayload, PositionNormal, Material> shadower = new Raytracer<ShadowRayPayload, PositionNormal, Material>();
            shadower.OnAnyHit += delegate (IRaycastContext context, PositionNormal attribute, Material material, ref ShadowRayPayload payload)
            {
                // If any object is found in ray-path to the light, the ray is shadowed.
                payload.Shadowed = true;
                // No neccessary to continue checking other objects
                return HitResult.Stop;
            };

            // Raycaster to trace rays and lit closest surfaces
            Raytracer<MyRayPayload, PositionNormal, Material> raycaster = new Raytracer<MyRayPayload, PositionNormal, Material>();
            raycaster.OnClosestHit += delegate (IRaycastContext context, PositionNormal attribute, Material material, ref MyRayPayload payload)
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

            /// Creates the model using a revolution of a bezier.
            /// Only Positions are updated.
            var model = Manifold<PositionNormal>.Revolution(30, 20, t => EvalBezier(contourn, t), float3(0, 1, 0)).Weld();
            model.ComputeNormals();
            return model;
        }

        static Mesh<PositionNormal> CreateSea()
        {
            /// Creates the model using a revolution of a bezier.
            /// Only Positions are updated.
            var model = Manifold<PositionNormal>.Surface(30, 20, (u, v) => 2*float3(2 * u - 1, sin(u*15)*0.02f+cos(v*13 + u*16)*0.03f, 2 * v - 1)).Weld();
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

        static void CreateMeshScene(Scene<PositionNormal, Material> scene)
        {
            var sea = CreateSea();
            scene.Add(sea.AsRaycast(RaycastingMeshMode.Grid), new Material(), Transforms.Identity);

            var egg = CreateModel();
            scene.Add(egg.AsRaycast(RaycastingMeshMode.Grid), new Material(), Transforms.Translate(0, 0.5f, 0));
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

            Scene<PositionNormal, Material> scene = new Scene<PositionNormal, Material>();
            CreateMeshScene(scene);

            BRDF[] brdfs =
            {
                LambertBRDF(float3(0.4f,0.5f,1f)),  // see
                Mixture(LambertBRDF(float3(0.7f,0.7f,0.3f)), BlinnBRDF(float3(1,1,1), 70), 0.3f), // egg
            };

            // Raycaster to trace rays and check for shadow rays.
            Raytracer<ShadowRayPayload, PositionNormal, Material> shadower = new Raytracer<ShadowRayPayload, PositionNormal, Material>();
            shadower.OnAnyHit += delegate (IRaycastContext context, PositionNormal attribute, Material material, ref ShadowRayPayload payload)
            {
                // If any object is found in ray-path to the light, the ray is shadowed.
                payload.Shadowed = true;
                // No neccessary to continue checking other objects
                return HitResult.Stop;
            };

            // Raycaster to trace rays and lit closest surfaces
            Raytracer<MyRayPayload, PositionNormal, Material> raycaster = new Raytracer<MyRayPayload, PositionNormal, Material>();
            raycaster.OnClosestHit += delegate (IRaycastContext context, PositionNormal attribute, Material material, ref MyRayPayload payload)
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

                    raycaster.Trace(scene, ray, ref coloring);

                    texture.Write(px, py, float4(coloring.Color, 1));
                }
        }


        #endregion

        static void Main(string[] args)
        {
            Stopwatch stopwatch = new Stopwatch();

            stopwatch.Start();

            // Texture to output the image.
            var baseWidth = 300;
            var baseHeight = 500;
            var ratio = (float)baseWidth / (float)baseHeight;

            var height = 850;
            Texture2D texture = new Texture2D((int)(height * ratio), height);

            var pathMax = 10000;
            var drawGuitar = true;
            if (drawGuitar)
            {
                Console.WriteLine($"Started at {DateTime.Now}");
                var csg = false;
                var mesh = false;
                var pathtracing = true;
                // Default raytracing

                if (csg)
                {
                    GuitarDrawer<MyPositionNormalCoordinate>.GuitarCSGRaycast(texture, Transforms.Identity);
                }
                else if (mesh)
                {
                    GuitarDrawer<MyPositionNormalCoordinate>.GuitarMesh(new Raster<MyPositionNormalCoordinate, MyProjectedVertex>(texture));
                }
                else if (pathtracing)
                {
                    /// Pathtracing can't be done concurrently
                    GuitarDrawer<MyPositionNormalCoordinate>.DrawStep = 1;
                    GuitarDrawer<MyPositionNormalCoordinate>.XGrid = 1;
                    GuitarDrawer<MyPositionNormalCoordinate>.YGrid = 1;
                    /// Pathtracing can't be done concurrently
                    
                    var continuePath = false;
                    var pass = 0;
                    if (continuePath)
                    {
                        Console.WriteLine("Write file path:");
                        var file = Console.ReadLine();
                        Console.WriteLine("Write pass number:");
                        pass = int.Parse(Console.ReadLine());
                        texture = Texture2D.LoadRbmFromFile(file);
                    }
                    GuitarDrawer<MyPositionNormalCoordinate>.GuitarPathtracing(texture, Transforms.Identity, pathMax, pass);
                }
                else
                {
                    GuitarDrawer<MyPositionNormalCoordinate>.DrawStep = 1;
                    GuitarDrawer<MyPositionNormalCoordinate>.LocalLightIntensity = float3(1, 1, 1) * 20;
                    GuitarDrawer<MyPositionNormalCoordinate>.GuitarRaytracing(texture, Transforms.Identity);
                    //GuitarDrawer.GuitarCSGRaycast(texture, Transforms.Identity);
                }
            }
            else
            {
                //Raytracing(texture);
                PathtracingInit(texture, pathMax);
                //SimpleRaycast(texture);
                //LitRaycast(texture);
                //RaycastingMesh(texture);
                //RaycastingMeshTexture(texture, mat);
                //Pathtracing(texture);
            }


            texture.Save("test.rbm");
            texture.SaveToBmp("test.bmp");

            //Raster<PositionNormal, MyProjectedVertex> render = new Raster<PositionNormal, MyProjectedVertex>(1024, 512);
            //GeneratingMeshes(render);
            //render.RenderTarget.Save("test.rbm");

            
            stopwatch.Stop();
            Console.WriteLine("Done. Rendered in " + stopwatch.ElapsedMilliseconds + " ms");
            Console.Beep(440, 1000);

            System.Diagnostics.Process.Start("CMD.exe","/C python imageviewer.py test.rbm");
        }
    }
}
