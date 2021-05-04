using GMath;
using Rendering;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Renderer
{
    public interface IColorable
    {
        float3 Color { get; set; }
    
        public void SetColor(Color color)
        {
            Color = new float3(color.R / 255.0f, color.B / 255.0f, color.G / 255.0f);
        }
    }

    public interface ITransformable<T> where T : struct
    {
        public T Transform(float4x4 matrix);
    }

    public interface IMaterial
    {

    }
}
