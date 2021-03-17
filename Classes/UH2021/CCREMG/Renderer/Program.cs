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
            // FreeTransformTest(render);
            //DrawRoomTest(render);
            CofeeMakerTest(render);
            render.RenderTarget.Save("test.rbm");
            Console.WriteLine("Done.");
        }

        public static float3[] RandomPositionsInBoxSurface(int N)
        {
            float3[] points = new float3[N];

            for (int i = 0; i < N; i++)
                points[i] = randomInBox();

            return points;
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

        private static void FreeTransformTest(Raster render)
        {
            render.ClearRT(float4(0, 0, 0.2f, 1)); // clear with color dark blue.

            int N = 100000;
            // Create buffer with points to render
            float3[] points = RandomPositionsInBoxSurface(N);

            // Creating boxy...
            points = ApplyTransform(points, float4x4(
                1f, 0, 0, 0,
                0, 1.57f, 0, 0,
                0, 0, 1f, 0,
                0, 0, 0, 1
                ));

            // Apply a free transform
            points = ApplyTransform(points, p => float3(p.x * cos(p.y) + p.z * sin(p.y), p.y, p.x * sin(p.y) - p.z * cos(p.y)));

            #region viewing and projecting

            points = ApplyTransform(points, Transforms.LookAtLH(float3(5f, 2.6f, 4), float3(0, 0, 0), float3(0, 1, 0)));
            points = ApplyTransform(points, Transforms.PerspectiveFovLH(pi_over_4, render.RenderTarget.Height / (float)render.RenderTarget.Width, 0.01f, 10));

            #endregion

            render.DrawPoints(points);
        }

        public static void DrawRoomTest(Raster raster)
        {
            raster.ClearRT(float4(0, 0, 0.2f, 1)); // clear with color dark blue.

            int N = 100000;
            // Create buffer with points to render
            float3[] points = RandomPositionsInBoxSurface(N);

            float4x4 viewMatrix = Transforms.LookAtLH(float3(5f, 4.6f, 2), float3(0, 0, 0), float3(0, 1, 0));
            float4x4 projMatrix = Transforms.PerspectiveFovLH(pi_over_4, raster.RenderTarget.Height / (float)raster.RenderTarget.Width, 0.01f, 10);

            DrawRoom(raster, points, mul(viewMatrix, projMatrix));
        }

        private static void DrawRoom(Raster raster, float3[] boxPoints, float4x4 transform)
        {
            DrawTable(raster, boxPoints, mul(Transforms.Translate(0, 0, 0), transform));
            DrawTable(raster, boxPoints, mul(Transforms.RotateRespectTo(float3(1,0,0), float3(0,1,0), pi/2), transform));
        }

        private static void DrawTable (Raster raster, float3[] boxPoints, float4x4 transform)
        {
            DrawTableLeg(raster, boxPoints, mul(Transforms.Translate(0.2f,0,0.2f), transform));
            DrawTableLeg(raster, boxPoints, mul(Transforms.Translate(1.6f,0,0.2f), transform));
            DrawTableLeg(raster, boxPoints, mul(Transforms.Translate(1.6f,0,1.6f), transform));
            DrawTableLeg(raster, boxPoints, mul(Transforms.Translate(0.2f,0,1.6f), transform));
            DrawTableTop(raster, boxPoints, mul(Transforms.Translate(0, 2, 0), transform));
        }
        
        private static void DrawTableTop(Raster raster, float3[] boxPoints, float4x4 transform)
        {
            float4x4 transformingIntoLeg = mul(Transforms.Scale(2.2f, 0.2f, 2.2f), transform);
            DrawBox(raster, boxPoints, transformingIntoLeg);
        }

        private static void DrawTableLeg(Raster raster, float3[] boxPoints, float4x4 transform)
        {
            float4x4 transformingIntoLeg = mul(Transforms.Scale(0.2f, 2, 0.2f), transform);
            DrawBox(raster, boxPoints, transformingIntoLeg);
        }

        private static void DrawBox(Raster raster, float3[] boxPoints, float4x4 transform)
        {
            float4x4 transformingIntoBox = mul(float4x4(
                0.5f, 0, 0, 0,
                0, 0.5f, 0, 0,
                0, 0, 0.5f, 0,
                0.5f, 0.5f, 0.5f, 1
                ), transform);

            float3[] pointsToDraw = ApplyTransform(boxPoints, transformingIntoBox);
            raster.DrawPoints(pointsToDraw);
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

            List<float3> buttonUnionPoints = PoliedroXZ(sides * 10, float3(0, altura_union, 0), 1.35f);
            List<float3> topUnionPoints = PoliedroXZ(sides * 10, float3(0, altura_union + h_union, 0), 1.35f);

            List<float3> buttonTopPoints = PoliedroXZ(sides, float3(0, altura_tope, 0), 1.4f);
            List<float3> topTopPoints = PoliedroXZ(sides, float3(0, altura_tope + h_tope, 0), 2.1f);

            List<float3> buttonTapaPoints = PoliedroXZ(sides, float3(0, altura_tapa, 0), 2.1f);
            List<float3> topTapaPoints = PoliedroXZ(sides, float3(0, altura_tapa + h_tapa, 0), 0.3f);

            List<float3> buttonCositaPoints = PoliedroXZ(sides, float3(0, altura_cosita, 0), 0.3f);
            List<float3> topCositaPoints = PoliedroXZ(sides, float3(0, altura_cosita + h_cosita, 0), 0.4f);

            List<float3> DownPointsList = new List<float3>();
            List<float3> UpPointsList = new List<float3>();


            DownPointsList.AddRange(UnirPoliedros(buttonBasePoints, topBasePoints, cloud, random));

            UpPointsList.AddRange(UnirPoliedros(buttonUnionPoints, topUnionPoints, 5, random));
            UpPointsList.AddRange(UnirPoliedros(buttonTopPoints, topTopPoints, cloud, random));
            UpPointsList.AddRange(UnirPoliedros(buttonTapaPoints, topTapaPoints, cloud, random));
            UpPointsList.AddRange(UnirPoliedros(buttonCositaPoints, topCositaPoints, cloud, random));

            float4x4 rosca_transform = Transforms.RotateRespectTo(float3(0,0,0), float3(0,1,0), pi/3);
            UpPointsList = new List<float3>(ApplyTransform(UpPointsList.ToArray(), rosca_transform));

            // float3[] base_points = pointsList.ToArray();

            // Apply a free transform
            // points = ApplyTransform(points, p => float3(p.x * cos(p.y) + p.z * sin(p.y), p.y, p.x * sin(p.y) - p.z * cos(p.y)));
            // float3[] top_points = ApplyTransform(base_points, mul(Transforms.RotateRespectTo(float3(0,0,0), float3(0,0,1), pi), Transforms.Translate(0,7,0)));


            DownPointsList.AddRange(UpPointsList);
            float3[] points = DownPointsList.ToArray();


            #region viewing and projecting

            points = ApplyTransform(points, Transforms.LookAtLH(float3(11f, 6.6f, 9), float3(0, 4, 0), float3(0, 1, 0)));
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

        private static List<float3> PoliedroXZ(int sides, float3 centre, float radio)
        {
            List<float3> points = new List<float3>();

            for(float i = 0; i < pi * 2; i += (pi * 2 / sides))
            {
                points.Add(centre + float3(radio * (float)Math.Cos(i), 0, radio * (float)Math.Sin(i)));
            }

            return points;
        }
    }
}
