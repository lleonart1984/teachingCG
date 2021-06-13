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
    class WallsBuilder<T> where T : struct, IVertex<T>, INormalVertex<T>, ICoordinatesVertex<T>, IColorable, ITransformable<T>
    {
        #region Scalars

        public float MeshScalar = 1f;

        public float Height;

        public float Width;

        public float Depth { get { return 0.5f; } }

        #endregion

        #region Colors

        public Color WallColor { get; set; } = Color.FromArgb(180, 90, 125);

        public Color FloorColor { get; set; } = Color.FromArgb(113, 54, 82);
        
        #endregion

        #region Materials

        public static MyMaterial<T> WallMaterial = Materials<T>.WallMaterial; //TODO

        public static MyMaterial<T> FloorMaterial = Materials<T>.FloorMaterial; // TODO

        #endregion

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
    
        public Mesh<T> WallMesh()
        {
            var initWall = MeshShapeGenerator<T>.Box(2, 2, 2);
            initWall = initWall.FitIn(1, 1, 1).ApplyTransforms(Transforms.Scale(2,2,.01f));
            var wall = initWall.ApplyTransforms(Transforms.Translate(0, 0, 1.05f));
            wall += initWall.ApplyTransforms(Transforms.RotateYGrad(90), Transforms.Translate(0, 0, 1.05f));
            wall += initWall.ApplyTransforms(Transforms.RotateYGrad(90), Transforms.Translate(2,0,1.05f));
            wall.SetMaterial(WallMaterial);
            return wall;;
        }

        public Mesh<T> FloorMesh()
        {
            var floor = MeshShapeGenerator<T>.Box(4, 2, 4);
            floor = floor.FitIn(1, 1, 1).ApplyTransforms(Transforms.Scale(2,.01f,2f), Transforms.Translate(0,0,-.8f));
            floor.SetMaterial(FloorMaterial);
            return floor;
        }
    
    }
}
