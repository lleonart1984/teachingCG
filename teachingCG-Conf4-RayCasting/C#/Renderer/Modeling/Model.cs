using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using GMath;
using Renderer;
using Rendering;
using static GMath.Gfx;


namespace Renderer.Modeling
{
    public class Model : IEnumerable<(float3, Color)>
    {
        private float3[] _points;
        private Color[] _colors;
        public (float3 topCorner, float3 oppositeCorner) BoundBox;

        public (float3,Color) this[int index]
        {
            get { return (_points[index], _colors[index]); }
        }

        public int Length => _points.Length;

        public Model() : this(new float3[] { }, new Color[] { })
        {
            
        }

        public Model(float3[] points, Color[] colors)
        {
            _points = points;
            _colors = colors;

            if (_points.Any())
                BoundBox = (float3(_points.Max(x => x.x), _points.Max(x => x.y), _points.Max(x => x.z)),
                            float3(_points.Min(x => x.x), _points.Min(x => x.y), _points.Min(x => x.z)));
        }

        public Model ApplyTransforms(params float4x4[] transforms)
        {
            var points = new float3[Length];
            var colors = new Color[Length];

            Array.Copy(_points, points, _points.Length);
            Array.Copy(_colors, colors, _colors.Length);
            
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
            return new Model(points, colors);
        }
    
        public Model ApplyFreeTransform(Func<float3, float3> freeTransform)
        {
            float3[] result = new float3[_points.Length];

            // Transform points with a function
            for (int i = 0; i < _points.Length; i++)
                result[i] = freeTransform(_points[i]);

            var colors = new Color[Length];
            _colors.CopyTo(colors, 0);

            return new Model(result, colors);
        }

        public Model ApplyFilter(Func<float3, bool> selector)
        {
            List<float3> points = new List<float3>();
            List<Color> colors = new List<Color>();

            foreach (var (point , color) in _points.Zip(_colors))
            {
                if (selector(point))
                {
                    points.Add(point);
                    colors.Add(color);
                }
            }
            return new Model(points.ToArray(), colors.ToArray());
        }

        public IEnumerator<(float3, Color)> GetEnumerator()
        {
            return ((IEnumerable<(float3, Color)>)_points.Zip(_colors)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _points.GetEnumerator();
        }

        /// <summary>
        /// Build a texture representing the XY plane with width and height.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="background"></param>
        /// <returns></returns>
        public Texture2D XY(int width, int height, Color background)
        {
            var texture = new Texture2D(width, height);
            var toRender = this.ApplyTransforms(Transforms.Translate(-BoundBox.oppositeCorner.x, -BoundBox.oppositeCorner.y, 0));
            toRender = toRender.ApplyTransforms(Transforms.Scale(width / toRender.BoundBox.topCorner.x, height / toRender.BoundBox.topCorner.y, 1));
            var painted = new bool[width, height];

            foreach (var (point, color) in toRender)
            {
                if ((int)point.x < width && (int)point.y < height)
                {
                    texture[(int)point.x, (int)point.y] = float4(color.R/255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
                    painted[(int)point.x, (int)point.y] = true;
                }
            }
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    if (!painted[i,j])
                        texture[i,j] = float4(background.R / 255.0f, background.G / 255.0f, background.B / 255.0f, background.A / 255.0f);
                }
            }

            return texture;
        }

        /// <summary>
        /// Returs a model with the points between (0,0,0) <= (x,y,z) <= (wwidth, height, deep)
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="deep"></param>
        /// <returns></returns>
        public Model FitIn(int width, int height, int deep)
        {
            var toRender = this.ApplyTransforms(Transforms.Translate(-BoundBox.oppositeCorner));
            toRender = toRender.ApplyTransforms(Transforms.Scale(width / toRender.BoundBox.topCorner.x, height / toRender.BoundBox.topCorner.y, deep / toRender.BoundBox.topCorner.z));
            return toRender;
        }

        public static Model operator + (Model a, Model b)
        {
            var points = new float3[a.Length + b.Length];
            var colors = new Color[a.Length + b.Length];
            Array.Copy(a._points, points, a.Length);
            Array.Copy(b._points, 0, points, a.Length, b.Length);
            Array.Copy(a._colors, colors, a.Length);
            Array.Copy(b._colors, 0, colors, a.Length, b.Length);
            return new Model(points, colors);
        }
        
        public static Model operator - (Model a, Model b)
        {
            List<float3> points = new List<float3>();
            List<Color> colors = new List<Color>();
            foreach (var (point, color) in a._points.Zip(a._colors))
            {
                if (b.BoundBox.oppositeCorner.x >= point.x || point.x >= b.BoundBox.topCorner.x ||
                    b.BoundBox.oppositeCorner.y >= point.y || point.y >= b.BoundBox.topCorner.y ||
                    b.BoundBox.oppositeCorner.z >= point.z || point.z >= b.BoundBox.topCorner.z)
                {
                    points.Add(point);
                    colors.Add(color);
                }
            }
            return new Model(points.ToArray(), colors.ToArray());
        }
    }
}
