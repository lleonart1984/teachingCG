using Renderer.Modeling;
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
using Renderer.CSG;

namespace Renderer
{
    public class GuitarBuilder
    {
        public float MeshScalar = 1f;

        public float StringLength => (BridgeLength - BridgeBodyDif)*2;

        public float StringBridgeSeparation => BridgeHeight / 2;

        public float BridgeLength => 28;

        public float BridgeBodyDif => BridgeLength / 4.0f; // How much the bridge is into the body

        public Color BridgeUpperColor => Color.FromArgb(65, 54, 52);

        public Color BridgeLowerColor => Color.FromArgb(65, 54, 52);

        public Color BodyColor => Color.FromArgb(202,110,1);

        public Color StringHubColor => Color.FromArgb(42, 31, 27);

        public Color PinColor => Color.LightYellow;

        public Color HeadPinColor => Color.WhiteSmoke;

        public Color FretColor => Color.SlateGray;

        public Color HoleColor => Color.FromArgb(94, 62, 23);

        public Color HeadstockColor => Color.FromArgb(85,43,21);

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
                                                                  Transforms.Scale(BridgeWidth, BridgeHeight/2, 1))
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
                                                                   Transforms.Translate(0, -.5f * BridgeHeight/2, -baseBridge + step * i)); // This is not the correct fret spacing
                frets += fret;
            }

            return (bridge + bridge2).ApplyTransforms(Transforms.Scale(1, 1, BridgeLength)) + frets;
        }

        public Model Headstock()
        {
            var width = BridgeWidth * 1.3f;
            var height = BridgeHeight / 2.0f;
            var length = BridgeWidth * 2;

            var basePiece = ShapeGenerator.Box(HeadstockColor).ApplyTransforms(Transforms.Translate(0,-.5f,-.5f),
                                                                 Transforms.Scale(width, height, length));
            var xHoleScale = 3 / 16.0f;
            var yHoleScale = 1;
            var zHoleScale = 3 / 4.0f;
            var holeDZ = -length * (1 - zHoleScale) / 2;
            var hole1 = basePiece.ApplyTransforms(Transforms.Scale(xHoleScale, yHoleScale*2f, zHoleScale),
                                                  Transforms.Translate(width*xHoleScale,height/2, holeDZ));
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

                var zTranslate = ((i%3)) * step - length - holeDZ + step/2;
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
                                                                               Transforms.Translate((i < 3 ? 1 : -1) * xHolderScale, -yPinScale, -xHolderScale*2));

                var head = ShapeGenerator.Cylinder(HeadPinColor, 1000).ApplyTransforms(Transforms.RotateX(pi_over_4 * 2),
                                                                         Transforms.Scale(1.1f*xHolderScale, 2*yHolderScale/6, 2*yHolderScale/3),
                                                                         Transforms.RotateY((two_pi/12*i)),
                                                                         Transforms.Translate((i < 3 ? 1 : -1) * xHolderScale, -yPinScale + yHolderScale, -xHolderScale * 2));


                var pin = basePin + headHolder + head;

                pin = pin.ApplyTransforms(Transforms.Translate((i < 3 ? 1 : -1) * (width/2 + xPinScale/2), yTranslate, zTranslate));
                
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
            float transform(float x, float val) => x * (bazier(val*3f)).y;

            var body = ShapeGenerator.Box(BodyColor, 40000).ApplyTransforms(Transforms.Translate(0,0,.7f))
                                                .ApplyFreeTransform(p => float3( transform(p.x, p.z), p.y, p.z))
                                                .ApplyTransforms(Transforms.Translate(0,0,-.7f));
            
            body = body.ApplyTransforms(Transforms.Translate(0,0,.5f),
                                        Transforms.Scale(BodyWidth, BridgeHeight*2, bodyLength),
                                        Transforms.Translate(0, BridgeHeight, BridgeLength - BridgeBodyDif));

            var radius = BridgeWidth * 1.5f / 2;
            var dz = BridgeLength + radius - (BridgeLength / 4 * .2f);
            var c = ShapeGenerator.Cylinder(Color.Red).ApplyTransforms(Transforms.RotateX(pi_over_4 * 2),
                                                              Transforms.Scale(radius, 2, radius),
                                                              Transforms.Translate(0, -.25f, dz));

            var hole = ShapeGenerator.Cylinder(HoleColor, thickness: BridgeWidth/8).ApplyTransforms(Transforms.RotateX(pi_over_4 * 2),
                                                              Transforms.Scale(radius, .01f, radius),
                                                              Transforms.Translate(0, 0, dz));
            
            dz = (c.Max(x => x.Item1.z) + c.Min(x => x.Item1.z)) / 2;
            var dx = (c.Max(x => x.Item1.x) + c.Min(x => x.Item1.x)) / 2;
            var minY = c.Min(x => x.Item1.y);
            var maxY = c.Max(x => x.Item1.y);

            body = body.ApplyFilter(x => !(Math.Pow(x.x - dx, 2) + Math.Pow(x.z - dz, 2) <= Math.Pow(radius, 2) && 
                                         minY <= x.y && x.y <= maxY));

            var stringHub = ShapeGenerator.Box(StringHubColor, 3000).ApplyTransforms(Transforms.Translate(0, -.5f,.5f),
                                                                     Transforms.Scale(BridgeWidth, 1, 1.5f),
                                                                     Transforms.Translate(0,0,StringLength));
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

        public Mesh<PositionNormal> BridgeStringMesh()
        {
            var strings = new Mesh<PositionNormal>();
            var step = BridgeWidth / (StringWidths.Length + 1);

            for (int i = 0; i < StringWidths.Length; i++)
            {
                var cylinder = MeshShapeGenerator<PositionNormal>.Cylinder((int)(10 * MeshScalar)).ApplyTransforms(Transforms.Translate(0, 0, .5f),
                                                                                           Transforms.Scale(StringWidths[i], StringWidths[i], 1),
                                                                                           Transforms.Translate(-BridgeWidth / 2 + step * (i + 1), -StringBridgeSeparation, 0));
                strings += cylinder;
            }

            return strings.ApplyTransforms(Transforms.Scale(1, 1, StringLength));
        }
    
        public Mesh<PositionNormal> BridgeMesh()
        {
            var bridge = MeshShapeGenerator<PositionNormal>.Box((int)(MeshScalar * 2), (int)(MeshScalar * 2), (int)(MeshScalar * 5),faceYZDown:false)
                .ApplyTransforms(Transforms.Translate(0, 0, .5f),
                                 Transforms.Scale(BridgeWidth, BridgeHeight / 2, 1)); // Remove face facing the cylinder
            var bridge2 = MeshShapeGenerator<PositionNormal>.Cylinder((int)(50 * MeshScalar), angle:pi).ApplyTransforms(Transforms.Translate(0, 0, .5f),
                                                                                                Transforms.Scale(BridgeWidth / 2, BridgeHeight / 2, 1),
                                                                                                Transforms.Translate(0, .5f * BridgeHeight / 2, 0));

            var baseBridge = bridge.Min(x => x.Position.z);
            var fretsAmount = 20;
            var step = BridgeLength / fretsAmount;
            var frets = new Mesh<PositionNormal>();
            for (int i = 0; i < fretsAmount; i++) // Frets
            {
                var fret = MeshShapeGenerator<PositionNormal>.Box((int)(3 * MeshScalar)).ApplyTransforms(Transforms.Translate(0, 0, .5f),
                                                                                 Transforms.Scale(BridgeWidth, 1, BridgeWidth / 30),
                                                                                 Transforms.Scale(1, i == 0 ? StringBridgeSeparation : BridgeWidth / 30, 1),
                                                                                 Transforms.Translate(0, -.5f * BridgeHeight / 2, -baseBridge + step * i)); // This is not the correct fret spacing
                frets += fret;
            }

            return (bridge + bridge2).ApplyTransforms(Transforms.Scale(1, 1, BridgeLength)) + frets;
        }
    
        public Mesh<PositionNormal> HeadstockMesh()
        {
            var width = BridgeWidth * 1.3f;
            var height = BridgeHeight / 2.0f;
            var length = BridgeWidth * 2;

            var basePiece = MeshShapeGenerator<PositionNormal>.Box((int)(4 * MeshScalar), (int)(2 * MeshScalar), (int)(6 * MeshScalar))
                                                                .ApplyTransforms(Transforms.Translate(0, -.5f, -.5f),
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

            var stringRollCylinders = new Mesh<PositionNormal>();
            var stringPins = new Mesh<PositionNormal>();
            var step = length * zHoleScale / 3.0f;
            for (int i = 0; i < 6; i++)
            {
                var xBaseCylinderScale = width * xHoleScale;

                var baseCylinder = MeshShapeGenerator<PositionNormal>.Cylinder((int)(5 * MeshScalar)).ApplyTransforms(Transforms.RotateY(pi_over_4 * 2),
                                                                                 Transforms.Scale(xBaseCylinderScale, height * yHoleScale * .25f, height * yHoleScale * .25f));

                var zTranslate = ((i % 3)) * step - length - holeDZ + step / 2;
                var yTranslate = -height / 2.0f;

                baseCylinder = baseCylinder.ApplyTransforms(Transforms.Translate((i < 3 ? 1 : -1) * 1f * (width * xHoleScale), yTranslate, zTranslate));

                stringRollCylinders += baseCylinder;

                var xPinScale = xBaseCylinderScale / 3;
                var yPinScale = height * .2f;

                var basePin = MeshShapeGenerator<PositionNormal>.Cylinder((int)(5 * MeshScalar)).ApplyTransforms(Transforms.RotateY(pi_over_4 * 2),
                                                                                          Transforms.Scale(xPinScale, yPinScale, yPinScale));

                var yHolderScale = height * 1.5f;
                var xHolderScale = xPinScale / 4;
                var headHolder = MeshShapeGenerator<PositionNormal>.Cylinder((int)(5 * MeshScalar)).ApplyTransforms(Transforms.RotateX(pi_over_4 * 2),
                                                                               Transforms.Translate(0, .5f, 0),
                                                                               Transforms.Scale(xHolderScale, yHolderScale, xHolderScale),
                                                                               Transforms.Translate((i < 3 ? 1 : -1) * xHolderScale, -yPinScale, -xHolderScale * 2));

                var head = MeshShapeGenerator<PositionNormal>.Cylinder((int)(5 * MeshScalar)).ApplyTransforms(Transforms.RotateX(pi_over_4 * 2),
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
    
        public Mesh<PositionNormal> MainBodyMesh()
        {
            var bodyLength = BridgeLength * 1.1f;

            Func<float, float2> bazier = BodyFunction();
            float transform(float x, float val) 
            {
                if (0f <= abs(x) && abs(x) <= .25f && .1f <= abs(val) && abs(val) <= .5f)
                    return x;
                return x * (bazier(val * 2.99f)).y;
            }; // Multiply val for the max value that bazier is defined, the amount of parts
            
            var body = MeshShapeGenerator<PositionNormal>.Box((int)(10 * MeshScalar),(int)(3 * MeshScalar), (int)(15 * MeshScalar) 
                                                              ,holeXZDown:true, sepXZDown:float4(.37f,.61f,.37f,.15f)
                                                              )
                                                .ApplyTransforms(Transforms.Translate(0, 0, .5f))
                                                .Transform(p => new PositionNormal { Position = float3(transform(p.Position.x, p.Position.z), p.Position.y, p.Position.z) })
                                                .ApplyTransforms(Transforms.Translate(0, 0, -.5f));

            body = body.ApplyTransforms(Transforms.Translate(0, 0, .5f),
                                        Transforms.Scale(BodyWidth, BridgeHeight * 2, bodyLength),
                                        Transforms.Translate(0, BridgeHeight, BridgeLength - BridgeBodyDif));

            var radius = BridgeWidth * 1.5f / 2;
            var dz = BridgeLength + radius - (BridgeLength / 4 * .2f);
            var c = MeshShapeGenerator<PositionNormal>.Cylinder(100).ApplyTransforms(Transforms.RotateX(pi_over_4 * 2),
                                                                                 Transforms.Scale(radius, 2, radius),
                                                                                 Transforms.Translate(0, -.25f, dz));

            var hole = MeshShapeGenerator<PositionNormal>.Cylinder((int)(100 * MeshScalar), thickness: BridgeWidth / 8, surface:true)
                                                   .ApplyTransforms(Transforms.RotateX(pi_over_4 * 2),
                                                                    Transforms.Scale(radius, 1, radius),
                                                                    Transforms.Translate(0, 0, dz));

            dz = (c.Max(x => x.Position.z) + c.Min(x => x.Position.z)) / 2;
            var dx = (c.Max(x => x.Position.x) + c.Min(x => x.Position.x)) / 2;
            var minY = c.Min(x => x.Position.y);
            var maxY = c.Max(x => x.Position.y);

            var stringHub = MeshShapeGenerator<PositionNormal>.Box((int)(6 * MeshScalar)).ApplyTransforms(Transforms.Translate(0, -.5f, .5f),
                                                                                   Transforms.Scale(BridgeWidth, 1, 1.5f),
                                                                                   Transforms.Translate(0, 0, StringLength));
            stringHub +=    MeshShapeGenerator<PositionNormal>.Box((int)(6 * MeshScalar)).ApplyTransforms(Transforms.Translate(0, -.5f, .5f),
                                                                                   Transforms.Scale(BridgeWidth * 3f, .5f, 1.5f),
                                                                                   Transforms.Translate(0, 0, StringLength));
            return hole + body + stringHub;
        }
    
        public Mesh<PositionNormal> GuitarMesh()
        {
            var body = MainBodyMesh() + BridgeMesh();
            return body + BridgeStringMesh() + HeadstockMesh();
        }

        #endregion

        #region CSG

        public float4x4 CSGWorldTransformation { get; set; } = Transforms.Identity;
        public float3 boxLower = float3(-.5f, -.5f, -.5f);
        public float3 boxUpper = float3(.5f, .5f, .5f);
        public float cylinderRadius = .5f;

        public void BridgeStrings(Scene<float3> scene)
        {
            var strings = new List<(IRaycastGeometry<float3>, float4x4)>();
            var step = BridgeWidth / (StringWidths.Length + 1);

            for (int i = 0; i < StringWidths.Length; i++)
            {
                var cylinder = Raycasting.Cylinder(.5f
                    , lowerBound: float3(-.5f, -.5f, -.5f)
                    , upperBound: float3(.5f, .5f, .5f));
                var transform = StackTransformations(
                                        Transforms.Translate(0, 0, .5f),
                                        Transforms.Scale(StringWidths[i], StringWidths[i], 1),
                                        Transforms.Translate(-BridgeWidth / 2 + step * (i + 1), -StringBridgeSeparation, 0),
                                        Transforms.Scale(1, 1, StringLength));
                strings.Add((cylinder, transform));
            }
            AddToScene(scene, strings);
        }

        public void Bridge(Scene<float3> scene)
        {
            var parts = new List<(IRaycastGeometry<float3>, float4x4)>();

            var bridge = Raycasting.Box(boxLower, boxUpper);
            var bridgeTransf = StackTransformations(Transforms.Translate(0, 0, .5f),
                                                    Transforms.Scale(BridgeWidth, BridgeHeight / 2, 1));

            var bridge2 = Raycasting.Cylinder(cylinderRadius
                    , lowerBound: float3(-.5f, -.5f, -.5f)
                    , upperBound: float3(.5f, .5f, .5f));
            var bridge2Transf = StackTransformations(Transforms.Translate(0, 0, .5f),
                                                     Transforms.Scale(BridgeWidth / 2, BridgeHeight / 2, 1),
                                                     Transforms.Translate(0, .5f * BridgeHeight / 2, 0));

            var baseBridge = min(mul(bridgeTransf, float4x1(boxLower.x, boxLower.y, boxLower.z, 1))._m20, 
                                 mul(bridgeTransf, float4x1(boxUpper.x, boxUpper.y, boxUpper.z, 1))._m20);
            var fretsAmount = 20;
            var step = BridgeLength / fretsAmount;
            var frets = new List<(IRaycastGeometry<float3>, float4x4)>();
            for (int i = 0; i < fretsAmount; i++) // Frets
            {
                var fret = Raycasting.Box(boxLower, boxUpper);
                var transform = StackTransformations(Transforms.Translate(0, 0, .5f),
                                                     Transforms.Scale(BridgeWidth, 1, BridgeWidth / 30),
                                                     Transforms.Scale(1, i == 0 ? StringBridgeSeparation : BridgeWidth / 30, 1),
                                                     Transforms.Translate(0, -.5f * BridgeHeight / 2, -baseBridge + step * i)); // This is not the correct fret spacing

                frets.Add((fret, transform));
            }
            bridgeTransf  = StackTransformations(bridgeTransf , Transforms.Scale(1, 1, BridgeLength));
            bridge2Transf = StackTransformations(bridge2Transf, Transforms.Scale(1, 1, BridgeLength));
            
            parts.Add((bridge, bridgeTransf));
            parts.Add((bridge2, bridge2Transf));
            parts.AddRange(frets);
            
            AddToScene(scene, parts);
        }

        public void Headstock(Scene<float3> scene)
        {
            var width = BridgeWidth * 1.3f;
            var height = BridgeHeight / 2.0f;
            var length = BridgeWidth * 2;
            var parts = new List<(IRaycastGeometry<float3>, float4x4)>();

            var basePieceTransform = StackTransformations(Transforms.Translate(0, -.5f, -.5f),
                                                          Transforms.Scale(width, height, length));
            var basePiece = new CSGNode(Raycasting.Box(boxLower, boxUpper), basePieceTransform); 
            var xHoleScale = 3 / 16.0f;
            var yHoleScale = 1;
            var zHoleScale = 3 / 4.0f;
            var holeDZ = -length * (1 - zHoleScale) / 2;

            var hole1Transf = StackTransformations(basePieceTransform,
                                                  Transforms.Scale(xHoleScale, yHoleScale * 2f, zHoleScale),
                                                  Transforms.Translate(width * xHoleScale, height / 2, holeDZ));
            var hole1 = new CSGNode(Raycasting.Box(boxLower, boxUpper), hole1Transf);
            
            var hole2Transf = StackTransformations(hole1Transf,
                                                   Transforms.Translate(-2 * width * xHoleScale, 0, 0));
            var hole2 = new CSGNode(Raycasting.Box(boxLower, boxUpper), hole2Transf);

            basePiece = basePiece / (hole1 | hole2);

            parts.Add((basePiece, Transforms.Identity)); // Identity because the transformation is already in CSGNode

            var stringRollCylinders = new List<(IRaycastGeometry<float3>, float4x4)>();
            var stringPins = new List<(IRaycastGeometry<float3>, float4x4)>();
            var step = length * zHoleScale / 3.0f;
            for (int i = 0; i < 6; i++)
            {
                var xBaseCylinderScale = width * xHoleScale;

                var zTranslate = ((i % 3)) * step - length - holeDZ + step / 2;
                var yTranslate = -height / 2.0f;
                var baseCylinder = Raycasting.Cylinder(cylinderRadius
                    , lowerBound: float3(-.5f, -.5f, -.5f)
                    , upperBound: float3(.5f, .5f, .5f));
                var baseCylinderTransf = StackTransformations(Transforms.RotateY(pi_over_4 * 2),
                                                              Transforms.Scale(xBaseCylinderScale, height * yHoleScale * .25f, height * yHoleScale * .25f),
                                                              Transforms.Translate((i < 3 ? 1 : -1) * 1f * (width * xHoleScale), yTranslate, zTranslate));
                stringRollCylinders.Add((baseCylinder, baseCylinderTransf));


                var xPinScale = xBaseCylinderScale / 3;
                var yPinScale = height * .2f;
                var finalPosTransf = Transforms.Translate((i < 3 ? 1 : -1) * (width / 2 + xPinScale / 2), yTranslate, zTranslate);


                var basePin = Raycasting.Cylinder(cylinderRadius
                    , lowerBound: float3(-.5f, -.5f, -.5f)
                    , upperBound: float3(.5f, .5f, .5f));
                var basePinTrans = StackTransformations(Transforms.RotateY(pi_over_4 * 2),
                                                        Transforms.Scale(xPinScale, yPinScale, yPinScale),
                                                        finalPosTransf);

                var yHolderScale = height * 1.5f;
                var xHolderScale = xPinScale / 4;
                var headHolder = Raycasting.Cylinder(cylinderRadius
                    , lowerBound: float3(-.5f, -.5f, -.5f)
                    , upperBound: float3(.5f, .5f, .5f));
                var headHolderTransf = StackTransformations(Transforms.RotateX(pi_over_4 * 2),
                                                            Transforms.Translate(0, .5f, 0),
                                                            Transforms.Scale(xHolderScale, yHolderScale, xHolderScale),
                                                            Transforms.Translate((i < 3 ? 1 : -1) * xHolderScale, -yPinScale, -xHolderScale * 2),
                                                            finalPosTransf);

                var head = Raycasting.Cylinder(cylinderRadius
                    , lowerBound: float3(-.5f, -.5f, -.5f)
                    , upperBound: float3(.5f, .5f, .5f));
                var headTransf = StackTransformations(Transforms.RotateX(pi_over_4 * 2),
                                                      Transforms.Scale(1.1f * xHolderScale, 2 * yHolderScale / 6, 2 * yHolderScale / 3),
                                                      Transforms.RotateY((two_pi / 12 * i)),
                                                      Transforms.Translate((i < 3 ? 1 : -1) * xHolderScale, -yPinScale + yHolderScale, -xHolderScale * 2),
                                                      finalPosTransf);

                stringPins.Add((basePin, basePinTrans));
                stringPins.Add((headHolder, headHolderTransf));
                stringPins.Add((head, headTransf));
            }
            parts.AddRange(stringRollCylinders);
            parts.AddRange(stringPins);

            AddToScene(scene, parts);
        }

        public void MainBody(Scene<float3> scene)
        {
            var parts = new List<(IRaycastGeometry<float3>, float4x4)>();
            
            var bodyLength = BridgeLength * 1.1f;

            var bodyTransf = StackTransformations(Transforms.Translate(0, 0, .5f),
                                                  Transforms.Scale(BodyWidth, BridgeHeight * 2, bodyLength),
                                                  Transforms.Translate(0, BridgeHeight, BridgeLength - BridgeBodyDif));
            var body = new CSGNode(Raycasting.Box(boxLower, boxUpper), bodyTransf);
            var innerBody = new CSGNode(Raycasting.Box(boxLower + .1f, boxUpper - .1f), bodyTransf);


            var radius = BridgeWidth * 1.5f ;
            var dz = BridgeLength + radius - (BridgeLength / 4 * .2f);
            var cTransf = StackTransformations(Transforms.RotateX(pi_over_4 * 2),
                                               Transforms.Scale(radius, 2, radius),
                                               Transforms.Translate(0, -.25f, dz));
            var c = new CSGNode(Raycasting.Cylinder(cylinderRadius
                    , lowerBound: float3(-.5f, -.5f, -.5f)
                    , upperBound: float3(.5f, .5f, .5f)), cTransf);

            var holeTransf = StackTransformations(Transforms.RotateX(pi_over_4 * 2),
                                                  Transforms.Scale(radius, .01f, radius),
                                                  Transforms.Translate(0, 0, dz));
            var hole = new CSGNode(Raycasting.Cylinder(cylinderRadius
                    , lowerBound: float3(-.5f, -.5f, -.5f)
                    , upperBound: float3(.5f, .5f, .5f)), holeTransf);

            body = (body | hole) / c;
            body /= innerBody;

            parts.Add((body, Transforms.Identity));

            var stringHub = Raycasting.Box(boxLower, boxUpper);
            var stringHubTransf = StackTransformations(Transforms.Translate(0, -.5f, .5f),
                                                       Transforms.Scale(BridgeWidth, 1, 1.5f),
                                                       Transforms.Translate(0, 0, StringLength));
            var stringHub2 = Raycasting.Box(boxLower, boxUpper);
            var stringHub2Transf = StackTransformations(Transforms.Translate(0, -.5f, .5f),
                                                        Transforms.Scale(BridgeWidth * 3f, .5f, 1.5f),
                                                        Transforms.Translate(0, 0, StringLength));
            parts.Add((stringHub, stringHubTransf));
            parts.Add((stringHub2, stringHub2Transf));

            AddToScene(scene, parts);
        }

        public void Guitar(Scene<float3> scene)
        {
            BridgeStrings(scene);
            Bridge(scene);
            Headstock(scene);
            MainBody(scene);
        }
        #endregion

        /// <summary>
        /// Guitar parametric shape function, defined between 0 and 3, 
        /// initial: (0,0) 
        /// max y: (2,1) 
        /// max x: (2.77,0) 
        /// y >= 0 
        /// x >=0
        /// </summary>
        /// <returns></returns>
        public Func<float, float2> BodyFunction()
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
        public Func<float, float2> PartFunction(params Func<float, float2>[] funcs)
        {
            return t =>
            {
                var index = (int)Math.Floor(t);
                if (funcs.Length <= index || index < 0)
                    throw new ArgumentException("Function is not defined");
                return funcs[index](t - index);
            };
        }

        public Func<float, float2> BazierCurve(params float2[] points)
        {
            return BazierCurve(points[0], points[1], points[2], points[3]);
        }

        public Func<float, float2> BazierCurve(float2 p1, float2 p2, float2 p3, float2 p4)
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
    
        public float4x4 StackTransformations(params float4x4[] transformations)
        {
            var transform = Transforms.Identity;
            foreach (var item in transformations)
            {
                transform = mul(transform, item);
            }
            return transform;
        }

        public void AddToScene(Scene<float3> scene, IEnumerable<(IRaycastGeometry<float3>, float4x4)> geometries)
        {
            foreach (var (geo, trans) in geometries)
            {
                scene.Add(geo, mul(trans, CSGWorldTransformation));
            }
        }
    }
}
