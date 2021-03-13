using GMath;
using Renderer.Rendering;
using System;
using System.Collections.Generic;
using System.Text;
using static GMath.Gfx;

namespace Renderer.Modeling
{
    public static class ShapeGenerator
    {
        /// <summary>
        /// Create a model representing a sphere centered in 0,0,0 with radius 1
        /// </summary>
        /// <param name="pointsAmount"></param>
        /// <returns></returns>
        public static Model Sphere(int pointsAmount = 10000)
        {
            float3[] points = new float3[pointsAmount];

            for (int i = 0; i < pointsAmount; i++)
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

            return new Model(points);
        }
    
        /// <summary>
        /// Create a model representing a cubic box centered in 0,0,0 with length 1
        /// </summary>
        /// <param name="pointAmounts"></param>
        /// <returns></returns>
        public static Model Box(int pointAmounts = 10000)
        {
            Func<int, float3, float3, float3[]> GenerateSurface = (amount, vanisher, side) =>
            {
                float3[] points = new float3[amount];
                for (int i = 0; i < amount; i++)
                {
                    points[i] = float3(random(), random(), random()) * vanisher + side;
                }
                return points;
            };

            pointAmounts += pointAmounts % 6;
            float3[] points = new float3[pointAmounts];
            var pointIndex = 0;
            foreach (var (sides, vanisher) in new[] { 
                (float3(1, 0, 0), float3(0, 1, 1)), 
                (float3(0, 0, 0), float3(0, 1, 1)), 
                (float3(0, 1, 0), float3(1, 0, 1)), 
                (float3(0, 0, 0), float3(1, 0, 1)), 
                (float3(0, 0, 1), float3(1, 1, 0)), 
                (float3(0, 0, 0), float3(1, 1, 0)),})
            {
                var amount = pointAmounts / 6;
                Array.Copy(GenerateSurface(amount, vanisher, sides), 0, points, pointIndex, amount);
                pointIndex += amount;
            }
            return new Model(points).ApplyTransforms(Transforms.Translate(-.5f,-.5f,-.5f));
        }
    }
}
