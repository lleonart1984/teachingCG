using System;
using System.Collections.Generic;
using System.Text;
using GMath;
using Renderer.Rendering;
using static GMath.Gfx;


namespace Renderer.Modeling
{
    public class Model
    {
        private float3[] _points;

        public float3 this[int index]
        {
            get { return _points[index]; }
        }

        public int Length => _points.Length;

        public Model(float3[] points)
        {
            _points = points;
        }

        public Model ApplyTransforms(params float4x4[] transforms)
        {
            var points = new float3[Length];
            Array.Copy(_points, points, _points.Length);
            var transform = Transforms.Identity;
            foreach (var item in transforms)
            {
                transform = mul(transform, item);
            }

            for (int i = 0; i < _points.Length; i++)
            {
                float4 h = float4(_points[i], 1);
                h = mul(h, transform);
                points[i] = h.xyz / h.w;
            }
            return new Model(points);
        }
    
        public static Model operator + (Model a, Model b)
        {
            var points = new float3[a.Length + b.Length];
            Array.Copy(a._points, points, a.Length);
            Array.Copy(b._points, 0, points, a.Length, b.Length);
            return new Model(points);
        }
    }
}
