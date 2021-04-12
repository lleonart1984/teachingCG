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
            GeneratingMeshes(render);
            render.RenderTarget.Save("test.rbm");
            Console.WriteLine("Done.");
        }

        private static void GeneratingMeshes(Raster<MyVertex, MyProjectedVertex> render)
        {
            render.ClearRT(float4(0, 0, 0.2f, 1)); // clear with color dark blue.

            CoffeeMakerModel<MyVertex> CoffeeMaker = new CoffeeMakerModel<MyVertex>();
            Mesh<MyVertex> models = CoffeeMaker.GetMesh();

            /// Convert to a wireframe to render. Right now only lines can be rasterized.
            Mesh<MyVertex> primitive = models.ConvertTo(Topology.Lines);

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
}
