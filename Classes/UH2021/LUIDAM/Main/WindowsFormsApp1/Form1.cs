using Renderer.Modeling;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Rendering;
using static GMath.Gfx;
using GMath;
using System.IO;
using System.Drawing.Imaging;
using Renderer;
using static Renderer.Program;

namespace MainForm
{
    public partial class Form1 : Form
    {
        private Model _baseModel;
        private Model _model;
        private Mesh<MyVertex> _baseModelMesh;
        private Mesh<MyVertex> _modelMesh;
        private float3 _baseTranslation;
        private float3 _baseZoom;
        private float _maxScale;
        private float _minScale;
        private Model _wall;
        private Raster<MyVertex, MyProjectedVertex> _raster;

        public Form1()
        {
            InitializeComponent();

            imageFile.FileOk += ImageFile_FileOk;

            //_baseModel = ShapeGenerator.Sphere(5000);
            //_baseModel = ShapeGenerator.Box(5000);
            _baseModel = new Model();
            _baseModelMesh = new Mesh<MyVertex>();
            SetGuitar();
            AddWalls();
            SetBaseTranslation();
            _baseZoom = float3(10, 10, 10);

            _model = _baseModel.ApplyTransforms(Transforms.Scale(_baseZoom), Transforms.Translate(_baseTranslation));
            _modelMesh = _baseModelMesh.ApplyTransforms(Transforms.Scale(_baseZoom), Transforms.Translate(_baseTranslation));

            _maxScale = 4.0f;
            _minScale = .5f;
            var stepSize = (_maxScale - _minScale) / (zoomBar.Maximum - zoomBar.Minimum);
            zoomBar.Value = (int)((1 - _minScale) / stepSize); // Setting zoomBar value to the value that represent scaling by 1
            DrawModel();
        }

        private void SetGuitar()
        {

            var generator = new GuitarBuilder();
            //_baseModel = generator.Guitar().ApplyTransforms(Transforms.Translate(-40,0,0));

            _baseModelMesh = generator.GuitarMesh();
        }

        private void AddWalls()
        {
            //_wall = new WallsBuilder().Wall();
            //var wallWidth = _wall.BoundBox.topCorner.x - _wall.BoundBox.oppositeCorner.x;
            //_baseModel += _wall.ApplyTransforms(Transforms.Scale(2 / 3.0f, 1, 1), Transforms.Translate(-1 / 3.0f * wallWidth, 0, 0));
        }

        public void ClearImagePbx()
        {
            imagePbx.BackColor = Color.DarkBlue;
        }

        public void DrawModel()
        {
            ClearImagePbx();
            
            if (imagePbx.Width == 0 || imagePbx.Height == 0)
                return;
            Bitmap image = new Bitmap(imagePbx.Width, imagePbx.Height);
            
            // Drawing Model
            for (int i = 0; i < _model.Length; i++)
            {
                var (point, color)= _model[i];
                (int x, int y) = ((int)point.x, (int)point.y);
                if (x < 0 || y < 0 || x >= image.Width || y >= image.Height)
                    continue;
                image.SetPixel(x, y, color);
            }

            // Drawing Mesh
            var lineMesh = _modelMesh.ConvertTo(Topology.Lines);
            _raster.DrawMesh(lineMesh);
            var target = _raster.RenderTarget;
            for (int i = 0; i < target.Width; i++)
            {
                for (int j = 0; j < target.Height; j++)
                {
                    var colorVector = target.Read(i, j);
                    image.SetPixel(i, j, Color.FromArgb(ColorComponent(colorVector.w), ColorComponent(colorVector.x), ColorComponent(colorVector.y), ColorComponent(colorVector.z)));
                    if (length(colorVector) != 0)
                    {
                        int a = 1;
                    }
                }
            }
            
            imagePbx.Image = image;
            imagePbx.Invalidate();
        }

        public void SaveModel()
        {
            var model = _model.ApplyFilter(x => x.y >= 0 && x.y <= imagePbx.Height);
            var top = model.BoundBox.topCorner;
            var low = model.BoundBox.oppositeCorner;
            var height = (int)(top.y - low.y);
            var width = (int)(top.x - low.x);
            var texture = model.XY(width, height, new WallsBuilder().FloorColor);
            
            // Save Mesh
            texture = _raster.RenderTarget;
            height = texture.Height;
            width = texture.Width;
            
            texture.Save("guitar.rbm");
            var btimap = new Bitmap(width, height);
            for (int i = 0; i < btimap.Width; i++)
            {
                for (int j = 0; j < btimap.Height; j++)
                {
                    btimap.SetPixel(i, j, Color.FromArgb(ColorComponent(texture[i, j].w), ColorComponent(texture[i, j].x), ColorComponent(texture[i, j].y), ColorComponent(texture[i, j].z)));
                }
            }
            btimap.Save("guitar.bmp", ImageFormat.Bmp);
        }

        private void imagePbx_Click(object sender, EventArgs e)
        {
            imageFile.ShowDialog();
        }

        private void ImageFile_FileOk(object sender, CancelEventArgs e)
        {
            if (!e.Cancel)
            {
                BinaryReader reader = new BinaryReader(imageFile.OpenFile());

                int width = reader.ReadInt32(); //read width value
                int height = reader.ReadInt32(); // read height value

                Bitmap bmp = new Bitmap(width, height);

                for (int py = 0; py < height; py++)
                    for (int px = 0; px < width; px++)
                    {
                        float r = reader.ReadSingle();
                        float g = reader.ReadSingle();
                        float b = reader.ReadSingle();
                        float a = reader.ReadSingle();
                        a = 1;// force no transparent

                        bmp.SetPixel(px, py, Color.FromArgb(ColorComponent(a), ColorComponent(r), ColorComponent(g), ColorComponent(b)));
                    }
                reader.Close();
                imagePbx.Image = bmp;
                imagePbx.Invalidate();
            }
        }

        static int ColorComponent(float x)
        {
            return (int)Math.Max(0, Math.Min(255, 256 * x));
        }

        private void UpdateModel(object sender, EventArgs e)
        {
            var stepSize = (_maxScale - _minScale) / (zoomBar.Maximum - zoomBar.Minimum);
            var scalar = _minScale + stepSize * zoomBar.Value;
            label1.Text = $"{xRotation.Value}, {yRotation.Value}, {zRotation.Value}, {scalar}";

            var worldTransformation = new List<float4x4>
            {
                Transforms.Scale(_baseZoom * float3(scalar, scalar, scalar)),
                Transforms.RotateX((xRotation.Value * two_pi) / (xRotation.Maximum - xRotation.Minimum)),
                Transforms.RotateY((yRotation.Value * two_pi) / (yRotation.Maximum - yRotation.Minimum)),
                Transforms.RotateZ((zRotation.Value * two_pi) / (zRotation.Maximum - zRotation.Minimum))
            };

            // WORLD TRANSFORMATIONS
            _model = _baseModel.ApplyTransforms(worldTransformation.ToArray());
            _modelMesh = _baseModelMesh.ApplyTransforms(worldTransformation.ToArray());

            // Camera, Perspective
            var cameraPos = float3(0,//-(_wall.BoundBox.topCorner.x - _wall.BoundBox.oppositeCorner.x) / 2,
                                   20,
                                   0//-(_wall.BoundBox.topCorner.z - _wall.BoundBox.oppositeCorner.z) / 2
                                   );
            var direction = float3(0, -1, 0);
            var upDirection = float3(0, 0, 1);
            
            _model = _model.ApplyTransforms(Transforms.Translate(-_model.BoundBox.oppositeCorner));
            _modelMesh = _modelMesh.ApplyTransforms(Transforms.Translate(-_modelMesh.BoundBox.oppositeCorner));

            float4x4 viewMatrix = Transforms.LookAtLH(cameraPos, direction, upDirection);
            float4x4 projectionMatrix = Transforms.PerspectiveFovLH(pi_over_4*2, (imagePbx.Image?.Height ?? 1) / (float?)imagePbx.Image?.Width ?? 1, 0.01f, 200);
            //float4x4 projectionMatrix = Transforms.PerspectiveLH((imagePbx.Image?.Height ?? 1), (float?)imagePbx.Image?.Width ?? 1, 0.01f, 40);
            
            // RASTER CONFIG
            // Define a vertex shader that projects a vertex into the NDC.
            _raster = new Raster<MyVertex, MyProjectedVertex>(imagePbx.Width, imagePbx.Height);
            _raster.VertexShader = v =>
            {
                float4 hPosition = float4(v.Position, 1);
                hPosition = mul(hPosition, viewMatrix);
                //hPosition = mul(hPosition, projectionMatrix);
                return new MyProjectedVertex { Homogeneous = hPosition };
            };

            // Define a pixel shader that colors using a constant value
            _raster.PixelShader = p =>
            {
                return float4(p.Homogeneous.x / 1024.0f, p.Homogeneous.y / 512.0f, 1, 1);
            };

            var cameraPerspectiveTransformations = new List<float4x4>
            {
                viewMatrix,
                //projectionMatrix
            };

            _model = _model.ApplyTransforms(cameraPerspectiveTransformations.ToArray());
            //_modelMesh = _modelMesh.ApplyTransforms(cameraPerspectiveTransformations.ToArray());

            var finalTransformations = new List<float4x4>
            {
                Transforms.Translate(_baseTranslation + float3((float)xTranslation.Value, (float)yTranslation.Value, (float)zTranslation.Value))
            };

           _model = _model.ApplyTransforms(finalTransformations.ToArray());
           _modelMesh = _modelMesh.ApplyTransforms(finalTransformations.ToArray());

            DrawModel();
        }

        public void SetBaseTranslation() 
        {
            _baseTranslation = float3(0, -300, 0);//float3(1.0f / 2.0f * imagePbx.Width, 1.0f / 2.0f * imagePbx.Height, 0);
        }

        private void imagePbx_SizeChanged(object sender, EventArgs e)
        {
            SetBaseTranslation();
            UpdateModel(sender, e);
        }

        private void saveBtn_Click(object sender, EventArgs e)
        {
            SaveModel();
        }
    }
}
