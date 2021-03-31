using Renderer.Modeling;
using Rendering;
using System;
using System.Collections.Generic;
using static GMath.Gfx;
using System.Linq;
using System.Text;
using GMath;

namespace MainForm
{
    class WallsBuilder
    {
        public float Height;

        public float Width;

        public float Depth { get { return 0.5f; } }

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

            var wall = ShapeGenerator.Box(10000).ApplyTransforms(Transforms.Scale(Height, Width, Depth),
                                                                 Transforms.RotateX(pi_over_4 * 2.2f),
                                                                 Transforms.Translate(0,1.6f,1.6f));

            wall += ShapeGenerator.Box(10000).ApplyTransforms(Transforms.Scale(Height, Width, Depth),
                                                              Transforms.Translate(0,-40,0),
                                                              Transforms.RotateX(pi_over_4 * 0.2f),
                                                              Transforms.Translate(0,0,53f));
                                                    
            return wall;
        }
    }
}