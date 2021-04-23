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
using System.Threading;

namespace MainForm
{
    public partial class Form1 : Form
    {
        private Model _baseModel;
        private Model _model;
        private Mesh<PositionNormal> _baseModelMesh;
        private Mesh<PositionNormal> _modelMesh;
        private float3 _baseTranslation;
        private float3 _baseZoom;
        private float _maxScale;
        private float _minScale;
        private Model _wall;
        private bool mesh = true;
        private MyTexture2D _texture;

        private float _currScale;
        private float _currRotX;
        private float _currRotY;
        private float _currRotZ;
        private float _currTraX;
        private float _currTraY;
        private float _currTraZ;


        private Task drawingTask;
        private CancellationTokenSource cts;
        private CancellationToken cancelToken;

        public Form1()
        {
            InitializeComponent();

            imageFile.FileOk += ImageFile_FileOk;

            _baseModel = new Model();
            _baseModelMesh = new Mesh<PositionNormal>();
            SetGuitar();
            AddWalls();
            SetBaseTranslation();
            _baseZoom = float3(1, 1, 1);

            _model = _baseModel.ApplyTransforms(Transforms.Scale(_baseZoom), Transforms.Translate(_baseTranslation));
            _modelMesh = _baseModelMesh.ApplyTransforms(Transforms.Scale(_baseZoom), Transforms.Translate(_baseTranslation));

            _maxScale = 4.0f;
            _minScale = .5f;
            var stepSize = (_maxScale - _minScale) / (zoomBar.Maximum - zoomBar.Minimum);
            zoomBar.Value = (int)((1 - _minScale) / stepSize); // Setting zoomBar value to the value that represent scaling by 1
            DrawModel();
        }

        private float4x4 WorldTransformation()
        {
            var stepSize = (_maxScale - _minScale) / (zoomBar.Maximum - zoomBar.Minimum);
            var scalar = _minScale + stepSize * _currScale;

            var worldTransformation = new List<float4x4>
            {
                Transforms.Translate(_baseTranslation + float3(_currTraX, _currTraY, _currTraZ)),
                Transforms.Scale(_baseZoom * float3(scalar, scalar, scalar)),
                Transforms.RotateX((_currRotX * two_pi) / (xRotation.Maximum - xRotation.Minimum)),
                Transforms.RotateY((_currRotY * two_pi) / (yRotation.Maximum - yRotation.Minimum)),
                Transforms.RotateZ((_currRotZ * two_pi) / (zRotation.Maximum - zRotation.Minimum))
            };

            var id = Transforms.Identity;
            foreach (var item in worldTransformation)
            {
                id = mul(id, item);
            }
            return id;
        }

        private void SetGuitar()
        {
            var generator = new GuitarBuilder();
            if (mesh)
                _baseModelMesh = generator.GuitarMesh();
            else
                _baseModel = generator.Guitar().ApplyTransforms(Transforms.Translate(-40, 0, 0));
        }

        private void AddWalls()
        {
            //var builder = new WallsBuilder();
            //if (!mesh)
            //{
            //    _wall = builder.Wall();
            //    var wallWidth = _wall.BoundBox.topCorner.x - _wall.BoundBox.oppositeCorner.x;
            //    _baseModel += _wall.ApplyTransforms(Transforms.Scale(2 / 3.0f, 1, 1), Transforms.Translate(-1 / 3.0f * wallWidth, 0, 0));
            //}
        }

        public void ClearImagePbx()
        {
            imagePbx.BackColor = Color.DarkBlue;
        }

        public void DrawModel()
        {
            if (drawingTask == null || drawingTask.IsCompleted)
            {
                cts = new CancellationTokenSource();
                cancelToken = cts.Token;
                drawingTask = Task.Run(DrawingModel, cancelToken);
            }
            else
            {
                cts.Cancel();
                Task.WaitAll(drawingTask);
                drawingTask = null;
                DrawModel();
            }
            
        }

        private void DrawingModel()
        {
            ClearImagePbx();

            if (imagePbx.Width == 0 || imagePbx.Height == 0)
                return;
            Bitmap image = new Bitmap(imagePbx.Width, imagePbx.Height);

            _texture = new MyTexture2D(imagePbx.Width, imagePbx.Height);
            //int p = 0;
            //_texture.PixelDrawed += (x, e) =>
            //{
            //    p++;
            //    if (label1.InvokeRequired)
            //    {
            //        label1.Invoke(new Action(() => label1.Text = $"{p}"));
            //        return;
            //    }
            //    label1.Text = $"{p}";
            //};

            if (mesh)
            {
                GuitarDrawer.DrawStep = 6;
                GuitarDrawer.GuitarRaycast(_texture, WorldTransformation());
                for (int i = 0; i < _texture.Width; i++)
                {
                    for (int j = 0; j < _texture.Height; j++)
                    {
                        var colorVector = _texture.Read(i, j);
                        image.SetPixel(Math.Min(i, imagePbx.Width - 1), Math.Min(j, imagePbx.Height - 1),
                            Color.FromArgb(ColorComponent(colorVector.w), ColorComponent(colorVector.x), ColorComponent(colorVector.y), ColorComponent(colorVector.z))
                            );
                    }
                }
            }
            else
            {
                // Drawing Model
                for (int i = 0; i < _model.Length; i++)
                {
                    var (point, color) = _model[i];
                    (int x, int y) = ((int)point.x, (int)point.y);
                    if (x < 0 || y < 0 || x >= image.Width || y >= image.Height)
                        continue;
                    image.SetPixel(x, y, color);
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
            var texture = model.XY(width, height, Color.Brown);
            
            // Save Mesh
            texture = _texture;
            height = texture.Height;
            width = texture.Width;

            var bitmap = imagePbx.Image as Bitmap;
            bitmap?.Save("image.bmp", ImageFormat.Bmp);
            
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

            _currScale = scalar;
            _currRotX = xRotation.Value;
            _currRotY = yRotation.Value;
            _currRotZ = zRotation.Value;
            _currTraX = (float)xTranslation.Value;
            _currTraY = (float)yTranslation.Value;
            _currTraZ = (float)zTranslation.Value;

            label1.Text = $"{xRotation.Value}, {yRotation.Value}, {zRotation.Value}, {scalar}";
            Task.Run(DrawModel);
        }

        public void SetBaseTranslation() 
        {
            _baseTranslation = float3(0, 0, 0);//float3(1.0f / 2.0f * imagePbx.Width, 1.0f / 2.0f * imagePbx.Height, 0);
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
