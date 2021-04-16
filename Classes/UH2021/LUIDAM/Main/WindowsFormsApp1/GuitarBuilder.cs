﻿using Renderer.Modeling;
using Rendering;
using System;
using System.Collections.Generic;
using static GMath.Gfx;
using System.Linq;
using System.Text;
using GMath;
using System.Drawing;
using static Renderer.Program;
using Renderer;

namespace Renderer
{
    public class GuitarBuilder
    {
        public float MeshScalar = 1f;

        public float StringLength => (BridgeLength - BridgeBodyDif) * 2;

        public float StringBridgeSeparation => BridgeHeight / 2;

        public float BridgeLength => 28;

        public float BridgeBodyDif => BridgeLength / 4.0f; // How much the bridge is into the body

        public Color BridgeUpperColor => Color.FromArgb(65, 54, 52);

        public Color BridgeLowerColor => Color.FromArgb(65, 54, 52);

        public Color BodyColor => Color.FromArgb(202, 110, 1);

        public Color StringHubColor => Color.FromArgb(42, 31, 27);

        public Color PinColor => Color.LightYellow;

        public Color HeadPinColor => Color.WhiteSmoke;

        public Color FretColor => Color.SlateGray;

        public Color HoleColor => Color.FromArgb(94, 62, 23);

        public Color HeadstockColor => Color.FromArgb(85, 43, 21);

        public float[] StringWidths => new float[] { .06f, .05f, .04f, .04f, .03f, .02f };

        public Color[] StringColors => new Color[] { Color.DarkSlateGray, Color.DarkSlateGray, Color.DarkSlateGray, Color.DarkSlateGray, Color.DarkSlateGray, Color.DarkSlateGray };

        public float BridgeWidth => 4;

        public float BridgeHeight => 1.5f;

        public float BodyWidth => BridgeWidth * 7;

        #region Dots

        public Model BridgeStrings()
        {
            var strings = new Model();
            var step = BridgeWidth / (StringWidths.Length + 1);

            for (int i = 0; i < StringWidths.Length; i++)
            {
                var cylinder = ShapeGenerator.Cylinder(StringColors[i], 6000).ApplyTransforms(Transforms.Translate(0, 0, .5f),
                                                                             Transforms.Scale(StringWidths[i], StringWidths[i], 1),
                                                                             Transforms.Translate(-BridgeWidth / 2 + step * (i + 1), -StringBridgeSeparation, 0));
                strings += cylinder;
            }

            return strings.ApplyTransforms(Transforms.Scale(1, 1, StringLength));
        }

        public Model Bridge()
        {

            var bridge = ShapeGenerator.Box(BridgeUpperColor, 10000).ApplyTransforms(Transforms.Translate(0, 0, .5f),
                                                                  Transforms.Scale(BridgeWidth, BridgeHeight / 2, 1))
                                                 .ApplyFilter(x => x.y != .5f); // Remove face facing the cylinder
            var bridge2 = ShapeGenerator.Cylinder(BridgeLowerColor, 10000).ApplyFilter(x => x.y > 0) // Remove top half of the cylinder
                                                       .ApplyTransforms(Transforms.Translate(0, 0, .5f),
                                                                        Transforms.Scale(BridgeWidth / 2, BridgeHeight / 2, 1),
                                                                        Transforms.Translate(0, .5f * BridgeHeight / 2, 0));

            var baseBridge = bridge.Min(x => x.Item1.z);
            var fretsAmount = 20;
            var step = BridgeLength / fretsAmount;
            var frets = new Model();
            for (int i = 0; i < fretsAmount; i++) // Frets
            {
                var fret = ShapeGenerator.Box(FretColor, 500).ApplyTransforms(Transforms.Translate(0, 0, .5f),
                                                                   Transforms.Scale(BridgeWidth, 1, BridgeWidth / 30),
                                                                   Transforms.Scale(1, i == 0 ? StringBridgeSeparation : BridgeWidth / 30, 1),
                                                                   Transforms.Translate(0, -.5f * BridgeHeight / 2, -baseBridge + step * i)); // This is not the correct fret spacing
                frets += fret;
            }

            return (bridge + bridge2).ApplyTransforms(Transforms.Scale(1, 1, BridgeLength)) + frets;
        }

        public Model Headstock()
        {
            var width = BridgeWidth * 1.3f;
            var height = BridgeHeight / 2.0f;
            var length = BridgeWidth * 2;

            var basePiece = ShapeGenerator.Box(HeadstockColor).ApplyTransforms(Transforms.Translate(0, -.5f, -.5f),
                                                                 Transforms.Scale(width, height, length));
            var xHoleScale = 3 / 16.0f;
            var yHoleScale = 1;
            var zHoleScale = 3 / 4.0f;
            var holeDZ = -length * (1 - zHoleScale) / 2;
            var hole1 = basePiece.ApplyTransforms(Transforms.Scale(xHoleScale, yHoleScale * 2f, zHoleScale),
                                                  Transforms.Translate(width * xHoleScale, height / 2, holeDZ));
            var hole2 = hole1.ApplyTransforms(Transforms.Translate(-2 * width * xHoleScale, 0, 0));

            basePiece -= hole1;
            basePiece -= hole2;

            var stringRollCylinders = new Model();
            var stringPins = new Model();
            var step = length * zHoleScale / 3.0f;
            for (int i = 0; i < 6; i++)
            {
                var xBaseCylinderScale = width * xHoleScale;

                var baseCylinder = ShapeGenerator.Cylinder(PinColor, 1000).ApplyTransforms(Transforms.RotateY(pi_over_4 * 2),
                                                                                 Transforms.Scale(xBaseCylinderScale, height * yHoleScale * .25f, height * yHoleScale * .25f));

                var zTranslate = ((i % 3)) * step - length - holeDZ + step / 2;
                var yTranslate = -height / 2.0f;

                baseCylinder = baseCylinder.ApplyTransforms(Transforms.Translate((i < 3 ? 1 : -1) * 1f * (width * xHoleScale), yTranslate, zTranslate));

                stringRollCylinders += baseCylinder;

                var xPinScale = xBaseCylinderScale / 3;
                var yPinScale = height * .2f;

                var basePin = ShapeGenerator.Cylinder(PinColor, 1000).ApplyTransforms(Transforms.RotateY(pi_over_4 * 2),
                                                                            Transforms.Scale(xPinScale, yPinScale, yPinScale));

                var yHolderScale = height * 1.5f;
                var xHolderScale = xPinScale / 4;
                var headHolder = ShapeGenerator.Cylinder(PinColor, 1000).ApplyTransforms(Transforms.RotateX(pi_over_4 * 2),
                                                                               Transforms.Translate(0, .5f, 0),
                                                                               Transforms.Scale(xHolderScale, yHolderScale, xHolderScale),
                                                                               Transforms.Translate((i < 3 ? 1 : -1) * xHolderScale, -yPinScale, -xHolderScale * 2));

                var head = ShapeGenerator.Cylinder(HeadPinColor, 1000).ApplyTransforms(Transforms.RotateX(pi_over_4 * 2),
                                                                         Transforms.Scale(1.1f * xHolderScale, 2 * yHolderScale / 6, 2 * yHolderScale / 3),
                                                                         Transforms.RotateY((two_pi / 12 * i)),
                                                                         Transforms.Translate((i < 3 ? 1 : -1) * xHolderScale, -yPinScale + yHolderScale, -xHolderScale * 2));


                var pin = basePin + headHolder + head;

                pin = pin.ApplyTransforms(Transforms.Translate((i < 3 ? 1 : -1) * (width / 2 + xPinScale / 2), yTranslate, zTranslate));

                stringPins += pin;
            }

            basePiece += stringRollCylinders + stringPins;
            return basePiece;
        }

        public Model MainBody()
        {
            // IMPLEMENTATION OF THE GUITAR MAIN BODY

            var bodyLength = BridgeLength * 1.1f;

            float bound = 2.697f;
            //float transform(float x, float val) => (x)*(-((val* bound) -1)*((val * bound) - 1)*((val * bound) - 2.3f)*((val * bound) - 3)+1); // -0.5 <= x <= 0.5   &&   0 <= z <= 1

            Func<float, float2> bazier = BodyFunction();
            float transform(float x, float val) => x * (bazier(val * 2.99f)).y;

            var body = ShapeGenerator.Box(BodyColor, 40000).ApplyTransforms(Transforms.Translate(0, 0, .5f))
                                                .ApplyFreeTransform(p => float3(transform(p.x, p.z), p.y, p.z))
                                                .ApplyTransforms(Transforms.Translate(0, 0, -.5f));

            body = body.ApplyTransforms(Transforms.Translate(0, 0, .5f),
                                        Transforms.Scale(BodyWidth, BridgeHeight * 2, bodyLength),
                                        Transforms.Translate(0, BridgeHeight, BridgeLength - BridgeBodyDif));

            var radius = BridgeWidth * 1.5f / 2;
            var dz = BridgeLength + radius - (BridgeLength / 4 * .2f);
            var c = ShapeGenerator.Cylinder(Color.Red).ApplyTransforms(Transforms.RotateX(pi_over_4 * 2),
                                                              Transforms.Scale(radius, 2, radius),
                                                              Transforms.Translate(0, -.25f, dz));

            var hole = ShapeGenerator.Cylinder(HoleColor, thickness: BridgeWidth / 8).ApplyTransforms(Transforms.RotateX(pi_over_4 * 2),
                                                              Transforms.Scale(radius, .01f, radius),
                                                              Transforms.Translate(0, 0, dz));

            dz = (c.Max(x => x.Item1.z) + c.Min(x => x.Item1.z)) / 2;
            var dx = (c.Max(x => x.Item1.x) + c.Min(x => x.Item1.x)) / 2;
            var minY = c.Min(x => x.Item1.y);
            var maxY = c.Max(x => x.Item1.y);

            body = body.ApplyFilter(x => !(Math.Pow(x.x - dx, 2) + Math.Pow(x.z - dz, 2) <= Math.Pow(radius, 2) &&
                                         minY <= x.y && x.y <= maxY));

            var stringHub = ShapeGenerator.Box(StringHubColor, 3000).ApplyTransforms(Transforms.Translate(0, -.5f, .5f),
                                                                     Transforms.Scale(BridgeWidth, 1, 1.5f),
                                                                     Transforms.Translate(0, 0, StringLength));
            stringHub += ShapeGenerator.Box(StringHubColor, 6000).ApplyTransforms(Transforms.Translate(0, -.5f, .5f),
                                                                  Transforms.Scale(BridgeWidth * 3f, .5f, 1.5f),
                                                                  Transforms.Translate(0, 0, StringLength));

            return hole + body + stringHub;
        }

        public Model Guitar()
        {
            var body = MainBody() + Bridge();
            return body + BridgeStrings() + Headstock();
            //return MainBody();
        }

        #endregion

        #region Mesh

        public Mesh<MyVertex> BridgeStringMesh()
        {
            var strings = new Mesh<MyVertex>();
            var step = BridgeWidth / (StringWidths.Length + 1);

            for (int i = 0; i < StringWidths.Length; i++)
            {
                var cylinder = MeshShapeGenerator<MyVertex>.Cylinder((int)(10 * MeshScalar)).ApplyTransforms(Transforms.Translate(0, 0, .5f),
                                                                                           Transforms.Scale(StringWidths[i], StringWidths[i], 1),
                                                                                           Transforms.Translate(-BridgeWidth / 2 + step * (i + 1), -StringBridgeSeparation, 0));
                strings += cylinder;
            }

            return strings.ApplyTransforms(Transforms.Scale(1, 1, StringLength));
        }

        public Mesh<MyVertex> BridgeMesh()
        {
            var bridge = MeshShapeGenerator<MyVertex>.Box((int)(100 * MeshScalar), false).ApplyTransforms(Transforms.Translate(0, 0, .5f),
                                                                                        Transforms.Scale(BridgeWidth, BridgeHeight / 2, 1)); // Remove face facing the cylinder
            var bridge2 = MeshShapeGenerator<MyVertex>.Cylinder((int)(50 * MeshScalar), angle: pi).ApplyTransforms(Transforms.Translate(0, 0, .5f),
                                                                                                Transforms.Scale(BridgeWidth / 2, BridgeHeight / 2, 1),
                                                                                                Transforms.Translate(0, .5f * BridgeHeight / 2, 0));

            var baseBridge = bridge.Min(x => x.Position.z);
            var fretsAmount = 20;
            var step = BridgeLength / fretsAmount;
            var frets = new Mesh<MyVertex>();
            for (int i = 0; i < fretsAmount; i++) // Frets
            {
                var fret = MeshShapeGenerator<MyVertex>.Box((int)(5 * MeshScalar)).ApplyTransforms(Transforms.Translate(0, 0, .5f),
                                                                                 Transforms.Scale(BridgeWidth, 1, BridgeWidth / 30),
                                                                                 Transforms.Scale(1, i == 0 ? StringBridgeSeparation : BridgeWidth / 30, 1),
                                                                                 Transforms.Translate(0, -.5f * BridgeHeight / 2, -baseBridge + step * i)); // This is not the correct fret spacing
                frets += fret;
            }

            return (bridge + bridge2).ApplyTransforms(Transforms.Scale(1, 1, BridgeLength)) + frets;
        }

        public Mesh<MyVertex> HeadstockMesh()
        {
            var width = BridgeWidth * 1.3f;
            var height = BridgeHeight / 2.0f;
            var length = BridgeWidth * 2;

            var basePiece = MeshShapeGenerator<MyVertex>.Box((int)(60 * MeshScalar)).ApplyTransforms(Transforms.Translate(0, -.5f, -.5f),
                                                                                    Transforms.Scale(width, height, length));
            var xHoleScale = 3 / 16.0f;
            var yHoleScale = 1;
            var zHoleScale = 3 / 4.0f;
            var holeDZ = -length * (1 - zHoleScale) / 2;
            var hole1 = basePiece.ApplyTransforms(Transforms.Scale(xHoleScale, yHoleScale * 2f, zHoleScale),
                                                  Transforms.Translate(width * xHoleScale, height / 2, holeDZ));
            var hole2 = hole1.ApplyTransforms(Transforms.Translate(-2 * width * xHoleScale, 0, 0));

            //basePiece -= hole1; //TODO VER ESTO LUEGO
            //basePiece -= hole2;

            var stringRollCylinders = new Mesh<MyVertex>();
            var stringPins = new Mesh<MyVertex>();
            var step = length * zHoleScale / 3.0f;
            for (int i = 0; i < 6; i++)
            {
                var xBaseCylinderScale = width * xHoleScale;

                var baseCylinder = MeshShapeGenerator<MyVertex>.Cylinder((int)(10 * MeshScalar)).ApplyTransforms(Transforms.RotateY(pi_over_4 * 2),
                                                                                 Transforms.Scale(xBaseCylinderScale, height * yHoleScale * .25f, height * yHoleScale * .25f));

                var zTranslate = ((i % 3)) * step - length - holeDZ + step / 2;
                var yTranslate = -height / 2.0f;

                baseCylinder = baseCylinder.ApplyTransforms(Transforms.Translate((i < 3 ? 1 : -1) * 1f * (width * xHoleScale), yTranslate, zTranslate));

                stringRollCylinders += baseCylinder;

                var xPinScale = xBaseCylinderScale / 3;
                var yPinScale = height * .2f;

                var basePin = MeshShapeGenerator<MyVertex>.Cylinder((int)(10 * MeshScalar)).ApplyTransforms(Transforms.RotateY(pi_over_4 * 2),
                                                                                          Transforms.Scale(xPinScale, yPinScale, yPinScale));

                var yHolderScale = height * 1.5f;
                var xHolderScale = xPinScale / 4;
                var headHolder = MeshShapeGenerator<MyVertex>.Cylinder((int)(10 * MeshScalar)).ApplyTransforms(Transforms.RotateX(pi_over_4 * 2),
                                                                               Transforms.Translate(0, .5f, 0),
                                                                               Transforms.Scale(xHolderScale, yHolderScale, xHolderScale),
                                                                               Transforms.Translate((i < 3 ? 1 : -1) * xHolderScale, -yPinScale, -xHolderScale * 2));

                var head = MeshShapeGenerator<MyVertex>.Cylinder((int)(10 * MeshScalar)).ApplyTransforms(Transforms.RotateX(pi_over_4 * 2),
                                                                         Transforms.Scale(1.1f * xHolderScale, 2 * yHolderScale / 6, 2 * yHolderScale / 3),
                                                                         Transforms.RotateY((two_pi / 12 * i)),
                                                                         Transforms.Translate((i < 3 ? 1 : -1) * xHolderScale, -yPinScale + yHolderScale, -xHolderScale * 2));


                var pin = basePin + headHolder + head;

                pin = pin.ApplyTransforms(Transforms.Translate((i < 3 ? 1 : -1) * (width / 2 + xPinScale / 2), yTranslate, zTranslate));

                stringPins += pin;
            }

            basePiece += stringRollCylinders + stringPins;
            return basePiece;
        }

        public Mesh<MyVertex> MainBodyMesh()
        {
            var bodyLength = BridgeLength * 1.1f;

            //float bound = 2.697f;
            //float transform(float x, float val) => (x) * (-((val * bound) - 1) * ((val * bound) - 1) * ((val * bound) - 2.3f) * ((val * bound) - 3) + 1); // -0.5 <= x <= 0.5   &&   0 <= z <= 1

            Func<float, float2> bazier = BodyFunction();
            float transform(float x, float val) => x * (bazier(val * 2.99f)).y; // Multiply val for the max value that bazier is defined, the amount of parts

            var body = MeshShapeGenerator<MyVertex>.Box((int)(400 * MeshScalar)).ApplyTransforms(Transforms.Translate(0, 0, .5f))
                                                .Transform(p => new MyVertex { Position = float3(transform(p.Position.x, p.Position.z), p.Position.y, p.Position.z) })
                                                .ApplyTransforms(Transforms.Translate(0, 0, -.5f));

            body = body.ApplyTransforms(Transforms.Translate(0, 0, .5f),
                                        Transforms.Scale(BodyWidth, BridgeHeight * 2, bodyLength),
                                        Transforms.Translate(0, BridgeHeight, BridgeLength - BridgeBodyDif));

            var radius = BridgeWidth * 1.5f / 2;
            var dz = BridgeLength + radius - (BridgeLength / 4 * .2f);
            var c = MeshShapeGenerator<MyVertex>.Cylinder(100).ApplyTransforms(Transforms.RotateX(pi_over_4 * 2),
                                                                                 Transforms.Scale(radius, 2, radius),
                                                                                 Transforms.Translate(0, -.25f, dz));

            var hole = MeshShapeGenerator<MyVertex>.Cylinder((int)(100 * MeshScalar), thickness: BridgeWidth / 8)
                                                   .ApplyTransforms(Transforms.RotateX(pi_over_4 * 2),
                                                                    Transforms.Scale(radius, .01f, radius),
                                                                    Transforms.Translate(0, 0, dz));

            dz = (c.Max(x => x.Position.z) + c.Min(x => x.Position.z)) / 2;
            var dx = (c.Max(x => x.Position.x) + c.Min(x => x.Position.x)) / 2;
            var minY = c.Min(x => x.Position.y);
            var maxY = c.Max(x => x.Position.y);

            //body = body.ApplyFilter(x => !(Math.Pow(x.x - dx, 2) + Math.Pow(x.z - dz, 2) <= Math.Pow(radius, 2) &&
            //                             minY <= x.y && x.y <= maxY)); // TODO VER ESTO LUEGO

            var stringHub = MeshShapeGenerator<MyVertex>.Box((int)(20 * MeshScalar)).ApplyTransforms(Transforms.Translate(0, -.5f, .5f),
                                                                                   Transforms.Scale(BridgeWidth, 1, 1.5f),
                                                                                   Transforms.Translate(0, 0, StringLength));
            stringHub += MeshShapeGenerator<MyVertex>.Box((int)(20 * MeshScalar)).ApplyTransforms(Transforms.Translate(0, -.5f, .5f),
                                                                                Transforms.Scale(BridgeWidth * 3f, .5f, 1.5f),
                                                                                Transforms.Translate(0, 0, StringLength));

            return hole + body + stringHub;
        }

        public Mesh<MyVertex> GuitarMesh()
        {
            var body = MainBodyMesh() + BridgeMesh();
            return body + BridgeStringMesh() + HeadstockMesh();
        }

        #endregion

        #region PureMesh

        public Mesh<MyVertex> BridgeStringPureMesh()
        {
            var strings = new Mesh<MyVertex>();
            var step = BridgeWidth / (StringWidths.Length + 1);

            for (int i = 0; i < StringWidths.Length; i++)
            {
                var cylinder = MeshShapeGenerator<MyVertex>.Cylinder((int)(10 * MeshScalar)).ApplyTransforms(Transforms.Translate(0, 0, .5f),
                                                                                           Transforms.Scale(StringWidths[i], StringWidths[i], 1),
                                                                                           Transforms.Translate(-BridgeWidth / 2 + step * (i + 1), -StringBridgeSeparation, 0));
                strings += cylinder;
            }

            return strings.ApplyTransforms(Transforms.Scale(1, 1, StringLength));
        }

        public Mesh<MyVertex> BridgePureMesh()
        {
            var bridge = MeshShapeGenerator<MyVertex>.Box((int)(100 * MeshScalar), false).ApplyTransforms(Transforms.Translate(0, 0, .5f),
                                                                                        Transforms.Scale(BridgeWidth, BridgeHeight / 2, 1)); // Remove face facing the cylinder
            var bridge2 = MeshShapeGenerator<MyVertex>.Cylinder((int)(50 * MeshScalar), angle: pi).ApplyTransforms(Transforms.Translate(0, 0, .5f),
                                                                                                Transforms.Scale(BridgeWidth / 2, BridgeHeight / 2, 1),
                                                                                                Transforms.Translate(0, .5f * BridgeHeight / 2, 0));

            var baseBridge = bridge.Min(x => x.Position.z);
            var fretsAmount = 20;
            var step = BridgeLength / fretsAmount;
            var frets = new Mesh<MyVertex>();
            for (int i = 0; i < fretsAmount; i++) // Frets
            {
                var fret = MeshShapeGenerator<MyVertex>.Box((int)(5 * MeshScalar)).ApplyTransforms(Transforms.Translate(0, 0, .5f),
                                                                                 Transforms.Scale(BridgeWidth, 1, BridgeWidth / 30),
                                                                                 Transforms.Scale(1, i == 0 ? StringBridgeSeparation : BridgeWidth / 30, 1),
                                                                                 Transforms.Translate(0, -.5f * BridgeHeight / 2, -baseBridge + step * i)); // This is not the correct fret spacing
                frets += fret;
            }

            return (bridge + bridge2).ApplyTransforms(Transforms.Scale(1, 1, BridgeLength)) + frets;
        }

        public Mesh<MyVertex> HeadstockPureMesh()
        {
            var width = BridgeWidth * 1.3f;
            var height = BridgeHeight / 2.0f;
            var length = BridgeWidth * 2;

            var basePiece = MeshShapeGenerator<MyVertex>.Box((int)(60 * MeshScalar)).ApplyTransforms(Transforms.Translate(0, -.5f, -.5f),
                                                                                    Transforms.Scale(width, height, length));
            var xHoleScale = 3 / 16.0f;
            var yHoleScale = 1;
            var zHoleScale = 3 / 4.0f;
            var holeDZ = -length * (1 - zHoleScale) / 2;
            var hole1 = basePiece.ApplyTransforms(Transforms.Scale(xHoleScale, yHoleScale * 2f, zHoleScale),
                                                  Transforms.Translate(width * xHoleScale, height / 2, holeDZ));
            var hole2 = hole1.ApplyTransforms(Transforms.Translate(-2 * width * xHoleScale, 0, 0));

            //basePiece -= hole1; //TODO VER ESTO LUEGO
            //basePiece -= hole2;

            var stringRollCylinders = new Mesh<MyVertex>();
            var stringPins = new Mesh<MyVertex>();
            var step = length * zHoleScale / 3.0f;
            for (int i = 0; i < 6; i++)
            {
                var xBaseCylinderScale = width * xHoleScale;

                var baseCylinder = MeshShapeGenerator<MyVertex>.Cylinder((int)(10 * MeshScalar)).ApplyTransforms(Transforms.RotateY(pi_over_4 * 2),
                                                                                 Transforms.Scale(xBaseCylinderScale, height * yHoleScale * .25f, height * yHoleScale * .25f));

                var zTranslate = ((i % 3)) * step - length - holeDZ + step / 2;
                var yTranslate = -height / 2.0f;

                baseCylinder = baseCylinder.ApplyTransforms(Transforms.Translate((i < 3 ? 1 : -1) * 1f * (width * xHoleScale), yTranslate, zTranslate));

                stringRollCylinders += baseCylinder;

                var xPinScale = xBaseCylinderScale / 3;
                var yPinScale = height * .2f;

                var basePin = MeshShapeGenerator<MyVertex>.Cylinder((int)(10 * MeshScalar)).ApplyTransforms(Transforms.RotateY(pi_over_4 * 2),
                                                                                          Transforms.Scale(xPinScale, yPinScale, yPinScale));

                var yHolderScale = height * 1.5f;
                var xHolderScale = xPinScale / 4;
                var headHolder = MeshShapeGenerator<MyVertex>.Cylinder((int)(10 * MeshScalar)).ApplyTransforms(Transforms.RotateX(pi_over_4 * 2),
                                                                               Transforms.Translate(0, .5f, 0),
                                                                               Transforms.Scale(xHolderScale, yHolderScale, xHolderScale),
                                                                               Transforms.Translate((i < 3 ? 1 : -1) * xHolderScale, -yPinScale, -xHolderScale * 2));

                var head = MeshShapeGenerator<MyVertex>.Cylinder((int)(10 * MeshScalar)).ApplyTransforms(Transforms.RotateX(pi_over_4 * 2),
                                                                         Transforms.Scale(1.1f * xHolderScale, 2 * yHolderScale / 6, 2 * yHolderScale / 3),
                                                                         Transforms.RotateY((two_pi / 12 * i)),
                                                                         Transforms.Translate((i < 3 ? 1 : -1) * xHolderScale, -yPinScale + yHolderScale, -xHolderScale * 2));


                var pin = basePin + headHolder + head;

                pin = pin.ApplyTransforms(Transforms.Translate((i < 3 ? 1 : -1) * (width / 2 + xPinScale / 2), yTranslate, zTranslate));

                stringPins += pin;
            }

            basePiece += stringRollCylinders + stringPins;
            return basePiece;
        }

        public Mesh<MyVertex> MainBodyPureMesh()
        {
            var bodyLength = BridgeLength * 1.1f;

            float bound = 2.697f;
            float transform(float x, float val) => (x) * (-((val * bound) - 1) * ((val * bound) - 1) * ((val * bound) - 2.3f) * ((val * bound) - 3) + 1); // -0.5 <= x <= 0.5   &&   0 <= z <= 1

            var body = MeshShapeGenerator<MyVertex>.Box((int)(400 * MeshScalar)).ApplyTransforms(Transforms.Translate(0, 0, .7f))
                                                .Transform(p => new MyVertex { Position = float3(transform(p.Position.x, p.Position.z), p.Position.y, p.Position.z) })
                                                .ApplyTransforms(Transforms.Translate(0, 0, -.7f));

            body = body.ApplyTransforms(Transforms.Translate(0, 0, .5f),
                                        Transforms.Scale(BodyWidth, BridgeHeight * 2, bodyLength),
                                        Transforms.Translate(0, BridgeHeight, BridgeLength - BridgeBodyDif));

            var radius = BridgeWidth * 1.5f / 2;
            var dz = BridgeLength + radius - (BridgeLength / 4 * .2f);
            var c = MeshShapeGenerator<MyVertex>.Cylinder(100).ApplyTransforms(Transforms.RotateX(pi_over_4 * 2),
                                                                                 Transforms.Scale(radius, 2, radius),
                                                                                 Transforms.Translate(0, -.25f, dz));

            var hole = MeshShapeGenerator<MyVertex>.Cylinder((int)(100 * MeshScalar), thickness: BridgeWidth / 8)
                                                   .ApplyTransforms(Transforms.RotateX(pi_over_4 * 2),
                                                                    Transforms.Scale(radius, .01f, radius),
                                                                    Transforms.Translate(0, 0, dz));

            dz = (c.Max(x => x.Position.z) + c.Min(x => x.Position.z)) / 2;
            var dx = (c.Max(x => x.Position.x) + c.Min(x => x.Position.x)) / 2;
            var minY = c.Min(x => x.Position.y);
            var maxY = c.Max(x => x.Position.y);

            //body = body.ApplyFilter(x => !(Math.Pow(x.x - dx, 2) + Math.Pow(x.z - dz, 2) <= Math.Pow(radius, 2) &&
            //                             minY <= x.y && x.y <= maxY)); // TODO VER ESTO LUEGO

            var stringHub = MeshShapeGenerator<MyVertex>.Box((int)(20 * MeshScalar)).ApplyTransforms(Transforms.Translate(0, -.5f, .5f),
                                                                                   Transforms.Scale(BridgeWidth, 1, 1.5f),
                                                                                   Transforms.Translate(0, 0, StringLength));
            stringHub += MeshShapeGenerator<MyVertex>.Box((int)(20 * MeshScalar)).ApplyTransforms(Transforms.Translate(0, -.5f, .5f),
                                                                                Transforms.Scale(BridgeWidth * 3f, .5f, 1.5f),
                                                                                Transforms.Translate(0, 0, StringLength));

            return hole + body + stringHub;
        }

        public Mesh<MyVertex> GuitarPureMesh()
        {
            var body = MainBodyMesh() + BridgeMesh();
            return body + BridgeStringMesh() + HeadstockMesh();
        }

        #endregion

        /// <summary>
        /// Guitar parametric shape function, defined between 0 and 3, 
        /// initial: (0,0) 
        /// max y: (3,1) 
        /// max x: (3.5,0) 
        /// y >= 0 
        /// x >=0
        /// </summary>
        /// <returns></returns>
        Func<float, float2> BodyFunction()
        {
            var bazierPoints = new List<float2>
            {
                float2(0,0),
                float2(0,1.05f),
                float2(0.8498f,0.7696f),

                float2(0.85f,0.762f),

                float2(0.972f,0.642f),
                float2(1.205f,0.66f),

                float2(1.315f,0.786f),

                //float2(1.39f,0.896f),
                //float2(2.06f,1.496f),
                //float2(2.3f,0f),

                float2(1.39f,0.88f),
                float2(2.77f,1.52f),
                float2(2.77f,0f),

                //float2(2.5f,1.51f),
                //float2(2.5f,0f),
            };
            return PartFunction(BazierCurve(bazierPoints.GetRange(0, 4).ToArray()),
                                BazierCurve(bazierPoints.GetRange(3, 4).ToArray()),
                                BazierCurve(bazierPoints.GetRange(6, 4).ToArray()));
        }

        // Chops the funcs evaluation in intervals of 1
        Func<float, float2> PartFunction(params Func<float, float2>[] funcs)
        {
            return t =>
            {
                var index = (int)Math.Floor(t);
                if (funcs.Length <= index || index < 0)
                    throw new ArgumentException("Function is not defined");
                return funcs[index](t - index);
            };
        }


        Func<float, float2> BazierCurve(params float2[] points)
        {
            return BazierCurve(points[0], points[1], points[2], points[3]);
        }

        Func<float, float2> BazierCurve(float2 p1, float2 p2, float2 p3, float2 p4)
        {
            return t => float2(
            (1 - t) * ((1 - t) * ((1 - t) * p1.x
            + t * p2.x) + t * ((1 - t) * p2.x
            + t * p3.x)) + t * ((1 - t) * ((1 - t) * p2.x
            + t * p3.x) + t * ((1 - t) * p3.x
            + t * p4.x)),
            (1 - t) * ((1 - t) * ((1 - t) * p1.y
            + t * p2.y) + t * ((1 - t) * p2.y
            + t * p3.y)) + t * ((1 - t) * ((1 - t) * p2.y
            + t * p3.y) + t * ((1 - t) * p3.y
            + t * p4.y)));
        }
    }
}
