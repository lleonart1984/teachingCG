﻿using GMath;
using Renderer.Rendering;
using System;
using static GMath.Gfx;

namespace Renderer
{
    class Program
    {
        static void Main(string[] args)
        {
            Raster render = new Raster(1024, 512);
            //FreeTransformTest(render);
            //DrawRoomTest(render);
            var points = RandomPositionsInEllipse(10000);

            float4x4 viewMatrix = Transforms.LookAtLH(float3(5f, 4.6f, 2), float3(0, 0, 0), float3(0, 1, 0));
            float4x4 projMatrix = Transforms.PerspectiveFovLH(pi_over_4, render.RenderTarget.Height / (float)render.RenderTarget.Width, 0.01f, 10);

            points = ApplyTransform(points, mul(viewMatrix, projMatrix));

            DrawPoints(render, points);
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

        public static float3[] RandomPositionsInEllipse(int N, float xStretch=1, float yStretch=1, float zStretch=1)
        {
            float3[] points = new float3[N];

            for (int i = 0; i < N; i++)
            {
                var ellipse_point = new float3(random(), random(), random());
                ellipse_point = normalize(ellipse_point);
                switch ((int)(random() * 8)) 
                {
                    case 1:
                        ellipse_point = new float3(ellipse_point.x, ellipse_point.y, -ellipse_point.z);
                        break;
                    case 2:
                        ellipse_point = new float3(ellipse_point.x, -ellipse_point.y, ellipse_point.z);
                        break;
                    case 3:
                        ellipse_point = new float3(-ellipse_point.x, ellipse_point.y, ellipse_point.z);
                        break;
                    case 4:
                        ellipse_point = new float3(ellipse_point.x, -ellipse_point.y, -ellipse_point.z);
                        break;
                    case 5:
                        ellipse_point = new float3(-ellipse_point.x, ellipse_point.y, -ellipse_point.z);
                        break;
                    case 6:
                        ellipse_point = new float3(-ellipse_point.x, -ellipse_point.y, ellipse_point.z);
                        break;
                    case 7:
                        ellipse_point = new float3(-ellipse_point.x, -ellipse_point.y, -ellipse_point.z);
                        break;
                    default:
                        break;
                }
                points[i] = ellipse_point;
            }

            points = ApplyTransform(points, Transforms.Scale(xStretch, yStretch, zStretch));

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

        public static void DrawPoints(Raster raster, float3[] points)
        {
            raster.ClearRT(float4(0, 0, 0.2f, 1));
            raster.DrawPoints(points);
        }
    }
}
