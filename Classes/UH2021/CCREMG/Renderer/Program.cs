using GMath;
using Rendering;
using System;
using System.Collections.Generic;
using static GMath.Gfx;

namespace Renderer
{
    class Program
    {
        struct MyVertex : IVertex<MyVertex>
        {
            public float3 Position { get; set; }

            public MyVertex Add(MyVertex other)
            {
                return new MyVertex
                {
                    Position = this.Position + other.Position,
                };
            }

            public MyVertex Mul(float s)
            {
                return new MyVertex
                {
                    Position = this.Position * s,
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

        static void Main(string[] args)
        {
            Raster<MyVertex, MyProjectedVertex> render = new Raster<MyVertex, MyProjectedVertex>(1024, 512);
            // CofeeMakerTest(render);
            GeneratingMeshes(render);
            render.RenderTarget.Save("test.rbm");
            Console.WriteLine("Done.");
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

        static float3 EvalMyControl(float3[] control, float t)
        {
            float dist = 0f;
            float[] dist_tramos = new float[control.Length - 1];

            for(int i = 0; i < control.Length - 1; i++)
            {
                dist_tramos[i] = Gfx.length(control[i+1] - control[i]);
                dist += dist_tramos[i];
            }

            float dist_t = dist * t;
            float3 r = control[control.Length - 1];
            for(int i = 0; i < dist_tramos.Length; i++)
            {
                if(dist_t > dist_tramos[i])
                {
                    dist_t -= dist_tramos[i];
                    continue;
                }
                else
                {
                    r = lerp(control[i], control[i + 1], dist_t/dist_tramos[i]);
                    break;
                }
            }
            return r;
        }


        static Mesh<MyVertex>[] CreateModel()
        {
            // Parametric representation of a sphere.
            //return Manifold<MyVertex>.Surface(30, 30, (u, v) =>
            //{
            //    float alpha = u * 2 * pi;
            //    float beta = pi / 2 - v * pi;
            //    return float3(cos(alpha) * cos(beta), sin(beta), sin(alpha) * cos(beta));
            //});

            // Generative model
            //return Manifold<MyVertex>.Generative(30, 30,
            //    // g function
            //    u => float3(cos(2 * pi * u), 0, sin(2 * pi * u)),
            //    // f function
            //    (p, v) => p + float3(cos(v * pi), 2*v-1, 0)
            //);

            int sides = 10;

            float h_base = 3;
            float altura_base = 0;

            float h_union = 0.5f;
            float altura_union = altura_base + h_base;

            float h_tope = 3;
            float altura_tope = altura_union + h_union;

            float h_tapa = 0.3f;
            float altura_tapa = altura_tope + h_tope;

            float h_cosita = 1f;
            float altura_cosita = altura_tapa + h_tapa;

            // Revolution Sample with Bezier
            float3[] contourn_base =
            {
                float3(0, 0, 0),
                float3(2, 0, 0),
                float3(1.3f, h_base,0),
                float3(1.3f, altura_union + h_union, 0)
            };

            float3[] countourn_union =
            {
                float3(1.35f, altura_union, 0),
                float3(1.35f, altura_union + h_union, 0)
            };

            float3[] contourn_top = 
            {
                float3(1.35f, altura_tope, 0),
                float3(1.4f, altura_tope, 0),
                float3(2.1f, altura_tope + h_tope, 0),
                float3(0, altura_tope + h_tope, 0),
                float3(2.1f, altura_tope + h_tope, 0),
                float3(0.3f, altura_cosita, 0),
                float3(0.4f, altura_cosita + h_cosita, 0),
                float3(0, altura_cosita + h_cosita, 0)
            };
            // return Manifold<MyVertex>.Revolution(2, 10, t => EvalBezier(contourn, t), float3(0, 1, 0));
            Mesh<MyVertex> button_mesh = Manifold<MyVertex>.Revolution(20, 10, t => EvalMyControl(contourn_base, t), float3(0, 1, 0));
            Mesh<MyVertex> union_mesh = Manifold<MyVertex>.Revolution(10, 50, t => EvalMyControl(countourn_union, t), float3(0, 1, 0));
            Mesh<MyVertex> top_mesh = Manifold<MyVertex>.Revolution(40, 10, t => EvalMyControl(contourn_top, t), float3(0, 1, 0));

            List<float3> buttonTopPoints = PoliedroXZ(sides, float3(0, altura_tope, 0), 1.4f);
            List<float3> topTopPoints = PoliedroXZ(sides, float3(0, altura_tope + h_tope, 0), 2.1f);
            Mesh<MyVertex> mesh_piquito = Mesh_Piquito(buttonTopPoints, topTopPoints, 2.1f, 0.7f);
            mesh_piquito = mesh_piquito.Bigger_Mesh();


            List<float3> handlePoints = AsaXZ(float3(0, altura_tapa, 1.7f), h_tope, h_tope/2, h_union);
            List<float3> handlePoints1 = new List<float3>();
            List<float3> handlePoints2 = new List<float3>();
            for(int i = 0; i < handlePoints.Count; i++)
            {
                if(i < handlePoints.Count/2)
                    handlePoints1.Add(handlePoints[i]);
                else
                    handlePoints2.Add(handlePoints[i]);
            }
            Mesh<MyVertex> handle_mesh = CoffeMakerSection_Mesh(handlePoints1, handlePoints2);

            handle_mesh = handle_mesh.Add_Mesh(AsaLateralMesh(handlePoints1));
            handle_mesh = handle_mesh.Add_Mesh(AsaLateralMesh(handlePoints2));
            handle_mesh = handle_mesh.Bigger_Mesh();

            Mesh<MyVertex> up_mesh = top_mesh.Add_Mesh(mesh_piquito).Add_Mesh(handle_mesh);
            up_mesh = up_mesh.Transform(Transforms.RotateRespectTo(float3(0,0,0), float3(0,1,0), pi/3));

            handle_mesh = handle_mesh.Transform(Transforms.RotateRespectTo(float3(0,0,0), float3(0,1,0), pi/3));


            Mesh<MyVertex> r_model = button_mesh.Add_Mesh(union_mesh).Add_Mesh(up_mesh);

            Mesh<MyVertex>[] models = {r_model};
            return models;
        }

        private static void GeneratingMeshes(Raster<MyVertex, MyProjectedVertex> render)
        {
            render.ClearRT(float4(0, 0, 0.2f, 1)); // clear with color dark blue.

            Mesh<MyVertex>[] models = CreateModel();

            for(int i = 0; i < models.Length; i++)
            {
                /// Convert to a wireframe to render. Right now only lines can be rasterized.
                Mesh<MyVertex> primitive = models[i].ConvertTo(Topology.Lines);

                #region viewing and projecting

                float4x4 viewMatrix = Transforms.LookAtLH(float3(-12f, 6.6f, 0), float3(0, 4, 0), float3(0, 1, 0));
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
                    return float4(p.Homogeneous.x / 1024.0f, p.Homogeneous.y / 512.0f, 1, 1);
                };

                #endregion

                // Draw the mesh.
                render.DrawMesh(primitive);
            }
        }

        public static float3[] ApplyTransform(float3[] points, float4x4 matrix)
        {
            float3[] result = new float3[points.Length];

            // Transform points with a matrix
            // Linear transform in homogeneous coordinates
            for (int i = 0; i < points.Length; i++)
            {
                float4 h = float4(points[i], 1);
                h = mul(h, matrix);
                result[i] = h.xyz / h.w;
            }

            return result;
        }

        public static float3[] ApplyTransform(float3[] points, Func<float3, float3> freeTransform)
        {
            float3[] result = new float3[points.Length];

            // Transform points with a function
            for (int i = 0; i < points.Length; i++)
                result[i] = freeTransform(points[i]);

            return result;
        }

        private static void CofeeMakerTest(Raster<MyVertex, MyProjectedVertex>  render)
        {
            render.ClearRT(float4(0, 0, 0.2f, 1)); // clear with color dark blue.

            int sides = 10;
            int cloud = 500;
            GRandom random = new GRandom();

            float h_base = 3;
            float altura_base = 0;

            float h_union = 0.5f;
            float altura_union = altura_base + h_base;

            float h_tope = 3;
            float altura_tope = altura_union + h_union;

            float h_tapa = 0.3f;
            float altura_tapa = altura_tope + h_tope;

            float h_cosita = 1f;
            float altura_cosita = altura_tapa + h_tapa;

            List<float3> buttonBasePoints = PoliedroXZ(sides, float3(0, 0, 0), 2);
            List<float3> topBasePoints = PoliedroXZ(sides, float3(0, altura_base + h_base, 0), 1.3f);

            List<float3> basePoints = FillPoliedroXZ(buttonBasePoints, float3(0, 0, 0), cloud);
            List<float3> coffeeBasePoints = DrawCoffeeMakerSection(buttonBasePoints, topBasePoints, cloud);

            List<float3> unionPoints = DrawCylinder(float3(0, 0, 0), altura_union, h_union, 1.35f, cloud, random);
            // List<float3> buttonUnionPoints = PoliedroXZ(sides * 10, float3(0, altura_union, 0), 1.35f);
            // List<float3> topUnionPoints = PoliedroXZ(sides * 10, float3(0, altura_union + h_union, 0), 1.35f);

            List<float3> buttonTopPoints = PoliedroXZ(sides, float3(0, altura_tope, 0), 1.4f);
            List<float3> topTopPoints = PoliedroXZ(sides, float3(0, altura_tope + h_tope, 0), 2.1f);
            List<float3> topPoints = DrawCoffeeMakerTopSection(buttonTopPoints, topTopPoints, cloud, 2.1f, 0.7f, random);

            List<float3> buttonTapaPoints = PoliedroXZ(sides, float3(0, altura_tapa, 0), 2.1f);
            List<float3> topTapaPoints = PoliedroXZ(sides, float3(0, altura_tapa + h_tapa, 0), 0.3f);
            List<float3> tapaPoints = DrawCoffeeMakerSection(buttonTapaPoints, topTapaPoints, cloud / 10);

            List<float3> buttonCositaPoints = PoliedroXZ(sides, float3(0, altura_cosita, 0), 0.3f);
            List<float3> topCositaPoints = PoliedroXZ(sides, float3(0, altura_cosita + h_cosita, 0), 0.4f);
            List<float3> cositaPoints = DrawCoffeeMakerSection(buttonCositaPoints, topCositaPoints, cloud / 10);


            List<float3> handlePoints = AsaXZ(float3(0, altura_tapa, 1.7f), h_tope, h_tope/2, h_union);
            List<float3> handlePoints1 = new List<float3>();
            List<float3> handlePoints2 = new List<float3>();
            for(int i = 0; i < handlePoints.Count; i++)
            {
                if(i < handlePoints.Count/2)
                    handlePoints1.Add(handlePoints[i]);
                else
                    handlePoints2.Add(handlePoints[i]);
            }
            List<float3> listHandlePoints = UnirPoliedros(handlePoints1, handlePoints2, cloud, random);

            listHandlePoints.AddRange(DrawCoffeeMakerSection(handlePoints1, handlePoints2, cloud/50));
            listHandlePoints.AddRange(FillAsaLateral(handlePoints1, cloud/50, random));
            listHandlePoints.AddRange(FillAsaLateral(handlePoints2, cloud/50, random));


            List<float3> DownPointsList = new List<float3>();
            List<float3> UpPointsList = new List<float3>();


            DownPointsList.AddRange(UnirPoliedros(buttonBasePoints, topBasePoints, cloud, random));

            // UpPointsList.AddRange(UnirPoliedros(buttonUnionPoints, topUnionPoints, 5, random));
            UpPointsList.AddRange(UnirPoliedros(buttonTopPoints, topTopPoints, cloud, random));
            UpPointsList.AddRange(UnirPoliedros(buttonTapaPoints, topTapaPoints, cloud, random));
            UpPointsList.AddRange(UnirPoliedros(buttonCositaPoints, topCositaPoints, cloud, random));
            UpPointsList.AddRange(topPoints);
            UpPointsList.AddRange(tapaPoints);
            UpPointsList.AddRange(cositaPoints);
            UpPointsList.AddRange(listHandlePoints);

            float4x4 rosca_transform = Transforms.RotateRespectTo(float3(0,0,0), float3(0,1,0), pi/3);
            UpPointsList = new List<float3>(ApplyTransform(UpPointsList.ToArray(), rosca_transform));

            // float3[] base_points = pointsList.ToArray();

            // Apply a free transform
            // points = ApplyTransform(points, p => float3(p.x * cos(p.y) + p.z * sin(p.y), p.y, p.x * sin(p.y) - p.z * cos(p.y)));
            // float3[] top_points = ApplyTransform(base_points, mul(Transforms.RotateRespectTo(float3(0,0,0), float3(0,0,1), pi), Transforms.Translate(0,7,0)));


            DownPointsList.AddRange(UpPointsList);
            DownPointsList.AddRange(basePoints);
            DownPointsList.AddRange(coffeeBasePoints);
            DownPointsList.AddRange(unionPoints);

            float3[] points = DownPointsList.ToArray();

            #region viewing and projecting

            points = ApplyTransform(points, Transforms.LookAtLH(float3(-12f, 6.6f, 0), float3(0, 4, 0), float3(0, 1, 0)));
            points = ApplyTransform(points, Transforms.PerspectiveFovLH(pi_over_4, render.RenderTarget.Height / (float)render.RenderTarget.Width, 0.01f, 20));

            #endregion

            render.DrawPoints(points);
        }

        private static List<float3> UnirPoliedros(List<float3> poli1, List<float3> poli2, int cloud, GRandom random)
        {
            List<float3> points = new List<float3>();
            int sides = poli1.Count;

            for(int i = 0; i < sides; i++)
            {
                points.AddRange(new Segment3D(poli1[i], poli1[(i + 1) % sides]).RandomPoints(cloud, random));
                points.AddRange(new Segment3D(poli2[i], poli2[(i + 1) % sides]).RandomPoints(cloud, random));
                points.AddRange(new Segment3D(poli1[i], poli2[i]).RandomPoints(cloud, random));
            }
            return points;
        }

        private static List<float3> FillPoliedroXZ(List<float3> poli, float3 center, int cloud)
        {
            List<float3> points = new List<float3>();
            for(int i = 1; i < poli.Count; ++i){
                points.AddRange(FillTriangleXZ(center, poli[i], poli[i - 1], cloud));
            }
            points.AddRange(FillTriangleXZ(center, poli[0], poli[poli.Count - 1], cloud));
            return points;
        }


        private static Mesh<MyVertex> Mesh_Piquito(List<float3> baseF, List<float3> topF, float r, float d)
        {
            int n = baseF.Count;

            float3 a = (topF[n - 1] + baseF[n - 1]) / 2;
            float3 b = (topF[0] + baseF[0]) / 2;
            float3 q = (b + a) / 2;
            float3 p = Find(topF[n - 1], topF[0], r, d);

            MyVertex[] vertices = new MyVertex[4];
            int[] indices = new int[6];

            vertices[0] = new MyVertex{Position = topF[n-1]};
            vertices[1] = new MyVertex{Position = p};
            vertices[2] = new MyVertex{Position = topF[0]};
            vertices[3] = new MyVertex{Position = q};

            indices[0] = 0;
            indices[1] = 1;
            indices[2] = 3;
            indices[3] = 1;
            indices[4] = 2;
            indices[5] = 3;

            return new Mesh<MyVertex>(vertices, indices);
        }


        private static Mesh<MyVertex> CoffeMakerSection_Mesh(List<float3> baseF, List<float3> topF)
        {
            MyVertex[] vertices = new MyVertex[baseF.Count + topF.Count];
            int[] indices = new int[baseF.Count * 3 * 2];

            int j = 0;
            int k = 0;
            for(int i = 0; i < baseF.Count - 1; i++)
            {
                vertices[j] = new MyVertex{Position = baseF[i]};
                vertices[j+1] = new MyVertex{Position = topF[i]};

                indices[k] = j;
                indices[k+1] = j + 1;
                indices[k+2] = j + 2;
                indices[k+3] = j + 1;
                indices[k+4] = j + 3;
                indices[k+5] = j + 2;

                j += 2;
                k += 6;
            }

            vertices[j] = new MyVertex{Position = baseF[baseF.Count - 1]};
            vertices[j+1] = new MyVertex{Position = topF[topF.Count - 1]};

            indices[k] = j;
            indices[k+1] = j + 1;
            indices[k+2] = 0;
            indices[k+3] = j + 1;
            indices[k+4] = 0;
            indices[k+5] = 1;

            return new Mesh<MyVertex>(vertices, indices);
        }

        private static List<float3> DrawCoffeeMakerSection(List<float3> baseF, List<float3> topF, int cloud){
            var points = new List<float3>();
            for(int i = 1; i < baseF.Count; ++i){
                points.AddRange(FillTriangleXZ(baseF[i - 1], baseF[i], topF[i - 1], cloud));
                points.AddRange(FillTriangleXZ(baseF[i], topF[i], topF[i - 1], cloud));
            }

            points.AddRange(FillTriangleXZ(baseF[baseF.Count - 1], baseF[0], topF[topF.Count - 1], cloud));
            points.AddRange(FillTriangleXZ(baseF[0], topF[0], topF[topF.Count - 1], cloud));
            return points;
        }

        private static List<float3> DrawCoffeeMakerTopSection(List<float3> baseF, List<float3> topF, int cloud, float r, float d, GRandom rnd){
            var points = new List<float3>();
            int n = baseF.Count;

            for(int i = 1; i < n; ++i){
                points.AddRange(FillTriangleXZ(baseF[i - 1], baseF[i], topF[i - 1], cloud));
                points.AddRange(FillTriangleXZ(baseF[i], topF[i], topF[i - 1], cloud));
            }

            float3 a = (topF[n - 1] + baseF[n - 1]) / 2;
            float3 b = (topF[0] + baseF[0]) / 2;
            float3 q = (b + a) / 2;
            float3 p = Find(topF[n - 1], topF[0], r, d);

            points.AddRange(new Segment3D(topF[n - 1], p).RandomPoints(cloud, rnd));
            points.AddRange(new Segment3D(topF[0], p).RandomPoints(cloud, rnd));
            points.AddRange(new Segment3D(q, p).RandomPoints(cloud, rnd));
            points.AddRange(new Segment3D(topF[n - 1], q).RandomPoints(cloud, rnd));
            points.AddRange(new Segment3D(topF[0], q).RandomPoints(cloud, rnd));
            points.AddRange(FillTriangleXZ(baseF[n - 1], baseF[0], a, cloud / 5));
            points.AddRange(FillTriangleXZ(a, baseF[0], b, cloud / 5));
            points.AddRange(FillTriangleXZ(topF[n - 1], a, q, cloud/ 10));
            points.AddRange(FillTriangleXZ(q, b, topF[0], cloud / 10));
            points.AddRange(FillTriangleXZ(q, p, topF[n - 1], cloud / 5));
            points.AddRange(FillTriangleXZ(q, p, topF[0], cloud / 5));

            return points;
        }

        private static float3 Find(float3 a, float3 b, float r, float d){
            float3 m = (a + b) / 2;
            float k = m[2] / m[0];
            float xp = (float)Math.Sqrt(((d + r) * (d + r)) / (1 + k * k));
            return float3(xp, m[1], k * xp);
        }

        private static List<float3> FillTriangleXZ(float3 a, float3 b, float3 c, int cloud)
        {
            Random rnd = new Random();
            
            GRandom random = new GRandom();

            List<float3> points = new List<float3>();

            if(cloud < 0)
            {
                cloud = cloud * -1;
                points.AddRange(new Segment3D(a, b).RandomPoints(cloud, random));
                points.AddRange(new Segment3D(b, c).RandomPoints(cloud, random));
                points.AddRange(new Segment3D(c, a).RandomPoints(cloud, random));
            }
            else
            {
                for(int i = 0; i < cloud; ++i)
                {
                    float u = (float)rnd.NextDouble();
                    float v = (float)rnd.NextDouble();
                    float3 item = (1 - sqrt(u)) * a;
                    item += (sqrt(u) * (1 - v)) * b;
                    item += v * sqrt(u) * c;
                    points.Add(item);
                }
            }
            return points;
        }

        private static List<float3> DrawCylinder(float3 center, float baseHeight, float h, float r, int cloud, GRandom rnd){
            var points = new List<float3>();
            
            while(cloud-- > 0){
                float x = rnd.random();
                x *= r;
                
                float y = rnd.random();
                y *= h;
                y += baseHeight;

                float z = (float)Math.Sqrt(r * r - x * x);

                points.Add(float3(x, y, z));
                points.Add(float3(x, y, -z));
                points.Add(float3(-x, y, z));
                points.Add(float3(-x, y, -z));
            }
            return points;
        }

        private static List<float3> PoliedroXZ(int sides, float3 centre, float radio)
        {
            List<float3> points = new List<float3>();

            for(float i = 0; i < pi * 2; i += (pi * 2 / sides))
            {
                points.Add(centre + float3(radio * (float)Math.Cos(i), 0, radio * (float)Math.Sin(i)));
            }

            return points;
        }

        private static List<float3> AsaXZ(float3 site, float length, float width, float height)
        {
            List<float3> points = new List<float3>();

            points.Add(float3(0, 0, 0)); //0
            points.Add(float3(0, 0, width/3)); //1
            points.Add(float3(0, 0, 2 * width / 3)); //2
            points.Add(float3(length/4, 0, width)); //3
            points.Add(float3(length, 0, width/3)); //4
            points.Add(float3(5 * length / 6, 0, width / 6)); //5
            points.Add(float3(4 * length / 6, 0,  width/3)); //6
            points.Add(float3(length/2, 0, width/2)); //7
            points.Add(float3(length/4, 0, 2 * width/3)); //8
            points.Add(float3(length/5, 0, 3 * width/5)); //9
            points.Add(float3(length/4, 0, 2 * width/5)); //10
            points.Add(float3(length/5, 0, width/5)); //11
            points.Add(float3(length/4, 0, width/6)); //12
            points.Add(float3(length/4, 0, -1 * width/8)); //13

            int l = points.Count;
            for(int i = 0; i < l; i++)
            {
                points.Add(points[i] + float3(0, height, 0));
            }


            float4x4 transform = mul(mul(mul(Transforms.Translate(0, -1 * height/2, 0), Transforms.RotateRespectTo(float3(0,0,0), float3(0,0,1), -1 * pi / 2)), Transforms.Translate(site)), Transforms.RotateRespectTo(float3(0,0,0), float3(0,1,0), 8 * pi/5));

            float3[] points_r = ApplyTransform(points.ToArray(), transform);

            return new List<float3>(points_r);
        }
    

        private static Mesh<MyVertex> AsaLateralMesh(List<float3>asa)
        {
            int n = asa.Count;
            MyVertex[] vertices = new MyVertex[n];
            int[] indices = new int[36];

            int j = 0;
            for(int i = 0; i < n; i++)
            {
                vertices[i] = new MyVertex{Position = asa[i]};

            }
            indices[j] = 0;
            indices[++j] = 13;
            indices[++j] = 12;

            indices[++j] = 0;
            indices[++j] = 12;
            indices[++j] = 11;

            indices[++j] = 0;
            indices[++j] = 1;
            indices[++j] = 11;

            indices[++j] = 1;
            indices[++j] = 11;
            indices[++j] = 10;

            indices[++j] = 1;
            indices[++j] = n - 4;
            indices[++j] = n - 5;

            indices[++j] = 1;
            indices[++j] = n - 5;
            indices[++j] = n - 6;

            indices[++j] = 1;
            indices[++j] = 2;
            indices[++j] = n - 6;

            indices[++j] = 2;
            indices[++j] = 3;
            indices[++j] = n - 6;

            indices[++j] = 3;
            indices[++j] = n - 6;
            indices[++j] = n - 7;

            indices[++j] = 3;
            indices[++j] = 7;
            indices[++j] = 6;

            indices[++j] = 3;
            indices[++j] = 6;
            indices[++j] = 4;

            indices[++j] = 4;
            indices[++j] = 5;
            indices[++j] = 6;

            return new Mesh<MyVertex>(vertices, indices);
        }
        private static List<float3> FillAsaLateral(List<float3> asa, int cloud, GRandom rnd)
        {
            List<float3> points = new List<float3>();

            int n = asa.Count;
            for(int i = 0; i < n; i++)
            {
                points.AddRange(FillTriangleXZ(asa[0], asa[n - 1], asa[n - 2], cloud));
                points.AddRange(FillTriangleXZ(asa[0], asa[n - 2], asa[n - 3], cloud));
                points.AddRange(FillTriangleXZ(asa[0], asa[1], asa[n - 3], cloud));
                points.AddRange(FillTriangleXZ(asa[1], asa[n - 3], asa[n - 4], cloud));
                points.AddRange(FillTriangleXZ(asa[1], asa[n - 4], asa[n - 5], cloud));
                points.AddRange(FillTriangleXZ(asa[1], asa[n - 5], asa[n - 6], cloud));
                points.AddRange(FillTriangleXZ(asa[1], asa[2], asa[n - 6], cloud));
                points.AddRange(FillTriangleXZ(asa[2], asa[3], asa[n - 6], cloud));
                points.AddRange(FillTriangleXZ(asa[3], asa[n - 6], asa[n - 7], cloud));
                points.AddRange(FillTriangleXZ(asa[3], asa[n - 7], asa[n - 8], cloud));
                points.AddRange(FillTriangleXZ(asa[3], asa[n - 8], asa[n - 9], cloud));
                points.AddRange(FillTriangleXZ(asa[3], asa[4], asa[n - 9], cloud));
                points.AddRange(FillTriangleXZ(asa[4], asa[5], asa[6], cloud));
            }
            return points;
        }
    }
}
