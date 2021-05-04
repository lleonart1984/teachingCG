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
        /// <summary>
        /// Maps the Mesh vertexes representing a truncated plane in its original axis without transformations to the material
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="face"></param>
        void MapPlane<T>(Mesh<T> face) where T : struct, IVertex<T>, ICoordinatesVertex<T>;

        /// <summary>
        /// Maps the Mesh vertexes representing a truncated cylinder to the material
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="face"></param>
        void MapCylinder<T>(Mesh<T> baseCyl) where T : struct, IVertex<T>, ICoordinatesVertex<T>;
    }
}
