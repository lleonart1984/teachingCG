using GMath;
using Rendering;
using System;
using System.Collections.Generic;
using static GMath.Gfx;

namespace Renderer
{
    class Program
    {
        static void Main(string[] args)
        {
            Raster render = new Raster(1024, 512);
            CofeeMakerTest(render);
            render.RenderTarget.Save("test.rbm");
            Console.WriteLine("Done.");
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

        private static void DrawCoffeeMaker(Raster render){

        }

        private static void CofeeMakerTest(Raster render)
        {
            render.ClearRT(float4(0, 0, 0.2f, 1)); // clear with color dark blue.

            int sides = 10;
            int cloud = 1000;
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

            float4x4 rosca_transform = Transforms.RotateRespectTo(float3(0,0,0), float3(0,1,0), pi/3);
            UpPointsList = new List<float3>(ApplyTransform(UpPointsList.ToArray(), rosca_transform));

            // float3[] base_points = pointsList.ToArray();

            // Apply a free transform
            // points = ApplyTransform(points, p => float3(p.x * cos(p.y) + p.z * sin(p.y), p.y, p.x * sin(p.y) - p.z * cos(p.y)));
            // float3[] top_points = ApplyTransform(base_points, mul(Transforms.RotateRespectTo(float3(0,0,0), float3(0,0,1), pi), Transforms.Translate(0,7,0)));

            List<float3> handlePoints = AsaXZ(float3(0, altura_tapa, 2.1f), h_tope, h_tope/2, h_union);
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




            DownPointsList.AddRange(UpPointsList);
            DownPointsList.AddRange(basePoints);
            DownPointsList.AddRange(coffeeBasePoints);
            DownPointsList.AddRange(unionPoints);
            DownPointsList.AddRange(listHandlePoints);

            float3[] points = DownPointsList.ToArray();

            #region viewing and projecting

            points = ApplyTransform(points, Transforms.LookAtLH(float3(11f, 6.6f, 0), float3(0, 4, 0), float3(0, 1, 0)));
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
            

            List<float3> points = new List<float3>();
            for(int i = 0; i < cloud; ++i)
            {
                float u = (float)rnd.NextDouble();
                float v = (float)rnd.NextDouble();
                float3 item = (1 - sqrt(u)) * a;
                item += (sqrt(u) * (1 - v)) * b;
                item += v * sqrt(u) * c;
                points.Add(item);
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

            points.Add(float3(0, 0, 0));
            points.Add(float3(0, 0, width/3));
            points.Add(float3(0, 0, 2 * width / 3));
            points.Add(float3(length/4, 0, width));
            points.Add(float3(length, 0, width/3));
            points.Add(float3(5 * length / 6, 0, width / 6));
            points.Add(float3(4 * length / 6, 0, width/6));
            points.Add(float3(length/2, 0, width/2));
            points.Add(float3(length/4, 0, 2 * width/3));
            points.Add(float3(length/5, 0, 3 * width/5));
            points.Add(float3(length/4, 0, 2 * width/5));
            points.Add(float3(9 * length/40, 0, width/3));
            points.Add(float3(length/5, 0, width/5));
            points.Add(float3(length/4, 0, width/8));
            points.Add(float3(length/4, 0, -1 * width/8));

            int l = points.Count;
            for(int i = 0; i < l; i++)
            {
                points.Add(points[i] + float3(0, height, 0));
            }


            float4x4 transform = mul(mul(Transforms.Translate(0, -1 * height/2, 0), Transforms.RotateZ(pi/2)), Transforms.Translate(site));

            float3[] points_r = ApplyTransform(points.ToArray(), transform);

            return new List<float3>(points_r);
        }
    }
}
