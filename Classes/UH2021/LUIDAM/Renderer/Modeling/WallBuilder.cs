using Renderer.Modeling;
using Rendering;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using static GMath.Gfx;
using static Renderer.Program;

namespace Renderer
{
    class WallsBuilder
    {
        public float MeshScalar = 1f;

        public float Height;

        public float Width;

        public float Depth { get { return 0.5f; } }

        public Color WallColor { get; set; } = Color.FromArgb(180, 90, 125);
        public Color FloorColor { get; set; } = Color.FromArgb(113, 54, 82);

        public WallsBuilder()
        {
            Height = 100.0f;
            Width = 100.0f;
        }

        public Model Wall()
        {
            // var wall = ShapeGenerator.Box(10000).ApplyTransforms(Transforms.Translate(0,0,25.0f),
            //                                                      Transforms.Scale(Height, Width, Depth),
            //                                                      Transforms.RotateX(pi_over_4 * 0.6f));

            // wall += ShapeGenerator.Box(10000).ApplyTransforms(Transforms.Scale(Height, Width, Depth));

            var wall = ShapeGenerator.Box(FloorColor, 100000).ApplyTransforms(Transforms.Scale(Height, Width, Depth),
                                                                 Transforms.RotateX(pi_over_4 * 2.2f),
                                                                 Transforms.Translate(0, 1.6f, 1.6f));

            wall += ShapeGenerator.Box(WallColor, 100000).ApplyTransforms(Transforms.Scale(Height, Width, Depth),
                                                              Transforms.Translate(0, -40, 0),
                                                              Transforms.RotateX(pi_over_4 * 0.2f),
                                                              Transforms.Translate(0, 0, 53f));

            return wall;
        }
    
        public Mesh<MyPositionNormalCoordinate> WallMesh()
        {
            var wall = MeshShapeGenerator<MyPositionNormalCoordinate>.Box(30, 30, 2, true, false, false, false, false, false);
            var floor = MeshShapeGenerator<MyPositionNormalCoordinate>.Box(30, 2, 30, false, false, false, true, false, false);

            return (wall + floor);
        }
    }
}
