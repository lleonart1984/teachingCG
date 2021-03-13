namespace MainForm
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.xRotation = new System.Windows.Forms.TrackBar();
            this.yRotation = new System.Windows.Forms.TrackBar();
            this.zRotation = new System.Windows.Forms.TrackBar();
            this.label1 = new System.Windows.Forms.Label();
            this.imagePbx = new System.Windows.Forms.PictureBox();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.imageFile = new System.Windows.Forms.OpenFileDialog();
            this.zoomBar = new System.Windows.Forms.TrackBar();
            this.xRotationLabel = new System.Windows.Forms.Label();
            this.yRotationLabel = new System.Windows.Forms.Label();
            this.zRotationLabel = new System.Windows.Forms.Label();
            this.zoomLabel = new System.Windows.Forms.Label();
            this.xTranslation = new System.Windows.Forms.NumericUpDown();
            this.xTranslationLbl = new System.Windows.Forms.Label();
            this.yTranslation = new System.Windows.Forms.NumericUpDown();
            this.yTranslationLbl = new System.Windows.Forms.Label();
            this.zTranslation = new System.Windows.Forms.NumericUpDown();
            this.zTranslationLbl = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.xRotation)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.yRotation)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.zRotation)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.imagePbx)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.zoomBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xTranslation)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.yTranslation)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.zTranslation)).BeginInit();
            this.SuspendLayout();
            // 
            // xRotation
            // 
            this.xRotation.Location = new System.Drawing.Point(12, 32);
            this.xRotation.Maximum = 100;
            this.xRotation.Minimum = -100;
            this.xRotation.Name = "xRotation";
            this.xRotation.Size = new System.Drawing.Size(237, 56);
            this.xRotation.TabIndex = 1;
            this.xRotation.Scroll += new System.EventHandler(this.UpdateModel);
            // 
            // yRotation
            // 
            this.yRotation.Location = new System.Drawing.Point(12, 133);
            this.yRotation.Maximum = 100;
            this.yRotation.Minimum = -100;
            this.yRotation.Name = "yRotation";
            this.yRotation.Size = new System.Drawing.Size(237, 56);
            this.yRotation.TabIndex = 1;
            this.yRotation.Scroll += new System.EventHandler(this.UpdateModel);
            // 
            // zRotation
            // 
            this.zRotation.Location = new System.Drawing.Point(12, 215);
            this.zRotation.Maximum = 100;
            this.zRotation.Minimum = -100;
            this.zRotation.Name = "zRotation";
            this.zRotation.Size = new System.Drawing.Size(237, 56);
            this.zRotation.TabIndex = 1;
            this.zRotation.Scroll += new System.EventHandler(this.UpdateModel);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 464);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(50, 20);
            this.label1.TabIndex = 2;
            this.label1.Text = "label1";
            // 
            // imagePbx
            // 
            this.imagePbx.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.imagePbx.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.imagePbx.Location = new System.Drawing.Point(255, 12);
            this.imagePbx.Name = "imagePbx";
            this.imagePbx.Size = new System.Drawing.Size(538, 472);
            this.imagePbx.TabIndex = 3;
            this.imagePbx.TabStop = false;
            this.imagePbx.Click += new System.EventHandler(this.imagePbx_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(46, 24);
            this.toolStripMenuItem1.Text = "FIle";
            // 
            // imageFile
            // 
            this.imageFile.DefaultExt = "rbm";
            this.imageFile.FileName = "openFileDialog1";
            this.imageFile.Title = "Choose Image";
            // 
            // zoomBar
            // 
            this.zoomBar.Location = new System.Drawing.Point(12, 297);
            this.zoomBar.Maximum = 100;
            this.zoomBar.Name = "zoomBar";
            this.zoomBar.Size = new System.Drawing.Size(237, 56);
            this.zoomBar.TabIndex = 1;
            this.zoomBar.Scroll += new System.EventHandler(this.UpdateModel);
            // 
            // xRotationLabel
            // 
            this.xRotationLabel.AutoSize = true;
            this.xRotationLabel.Location = new System.Drawing.Point(12, 9);
            this.xRotationLabel.Name = "xRotationLabel";
            this.xRotationLabel.Size = new System.Drawing.Size(75, 20);
            this.xRotationLabel.TabIndex = 4;
            this.xRotationLabel.Text = "X rotation";
            // 
            // yRotationLabel
            // 
            this.yRotationLabel.AutoSize = true;
            this.yRotationLabel.Location = new System.Drawing.Point(12, 91);
            this.yRotationLabel.Name = "yRotationLabel";
            this.yRotationLabel.Size = new System.Drawing.Size(74, 20);
            this.yRotationLabel.TabIndex = 4;
            this.yRotationLabel.Text = "Y rotation";
            // 
            // zRotationLabel
            // 
            this.zRotationLabel.AutoSize = true;
            this.zRotationLabel.Location = new System.Drawing.Point(12, 192);
            this.zRotationLabel.Name = "zRotationLabel";
            this.zRotationLabel.Size = new System.Drawing.Size(75, 20);
            this.zRotationLabel.TabIndex = 4;
            this.zRotationLabel.Text = "Z rotation";
            // 
            // zoomLabel
            // 
            this.zoomLabel.AutoSize = true;
            this.zoomLabel.Location = new System.Drawing.Point(12, 274);
            this.zoomLabel.Name = "zoomLabel";
            this.zoomLabel.Size = new System.Drawing.Size(49, 20);
            this.zoomLabel.TabIndex = 4;
            this.zoomLabel.Text = "Zoom";
            // 
            // xTranslation
            // 
            this.xTranslation.Location = new System.Drawing.Point(111, 354);
            this.xTranslation.Name = "xTranslation";
            this.xTranslation.Size = new System.Drawing.Size(57, 27);
            this.xTranslation.TabIndex = 5;
            this.xTranslation.ValueChanged += new System.EventHandler(this.UpdateModel);
            // 
            // xTranslationLbl
            // 
            this.xTranslationLbl.AutoSize = true;
            this.xTranslationLbl.Location = new System.Drawing.Point(13, 356);
            this.xTranslationLbl.Name = "xTranslationLbl";
            this.xTranslationLbl.Size = new System.Drawing.Size(92, 20);
            this.xTranslationLbl.TabIndex = 4;
            this.xTranslationLbl.Text = "X translation";
            // 
            // yTranslation
            // 
            this.yTranslation.Location = new System.Drawing.Point(110, 388);
            this.yTranslation.Name = "yTranslation";
            this.yTranslation.Size = new System.Drawing.Size(57, 27);
            this.yTranslation.TabIndex = 5;
            this.yTranslation.ValueChanged += new System.EventHandler(this.UpdateModel);
            // 
            // yTranslationLbl
            // 
            this.yTranslationLbl.AutoSize = true;
            this.yTranslationLbl.Location = new System.Drawing.Point(12, 390);
            this.yTranslationLbl.Name = "yTranslationLbl";
            this.yTranslationLbl.Size = new System.Drawing.Size(91, 20);
            this.yTranslationLbl.TabIndex = 4;
            this.yTranslationLbl.Text = "Y translation";
            // 
            // zTranslation
            // 
            this.zTranslation.Location = new System.Drawing.Point(111, 422);
            this.zTranslation.Name = "zTranslation";
            this.zTranslation.Size = new System.Drawing.Size(57, 27);
            this.zTranslation.TabIndex = 5;
            this.zTranslation.ValueChanged += new System.EventHandler(this.UpdateModel);
            // 
            // zTranslationLbl
            // 
            this.zTranslationLbl.AutoSize = true;
            this.zTranslationLbl.Location = new System.Drawing.Point(13, 424);
            this.zTranslationLbl.Name = "zTranslationLbl";
            this.zTranslationLbl.Size = new System.Drawing.Size(92, 20);
            this.zTranslationLbl.TabIndex = 4;
            this.zTranslationLbl.Text = "Z translation";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(805, 496);
            this.Controls.Add(this.zTranslationLbl);
            this.Controls.Add(this.zTranslation);
            this.Controls.Add(this.yTranslationLbl);
            this.Controls.Add(this.yTranslation);
            this.Controls.Add(this.xTranslationLbl);
            this.Controls.Add(this.xTranslation);
            this.Controls.Add(this.zoomLabel);
            this.Controls.Add(this.zRotationLabel);
            this.Controls.Add(this.yRotationLabel);
            this.Controls.Add(this.xRotationLabel);
            this.Controls.Add(this.zoomBar);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.zRotation);
            this.Controls.Add(this.yRotation);
            this.Controls.Add(this.xRotation);
            this.Controls.Add(this.imagePbx);
            this.Name = "Form1";
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(this.xRotation)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.yRotation)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.zRotation)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.imagePbx)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.zoomBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xTranslation)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.yTranslation)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.zTranslation)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.TrackBar xRotation;
        private System.Windows.Forms.TrackBar yRotation;
        private System.Windows.Forms.TrackBar zRotation;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.PictureBox imagePbx;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
        private System.Windows.Forms.OpenFileDialog imageFile;
        private System.Windows.Forms.TrackBar zoomBar;
        private System.Windows.Forms.Label xRotationLabel;
        private System.Windows.Forms.Label yRotationLabel;
        private System.Windows.Forms.Label zRotationLabel;
        private System.Windows.Forms.Label zoomLabel;
        private System.Windows.Forms.NumericUpDown xTranslation;
        private System.Windows.Forms.Label xTranslationLbl;
        private System.Windows.Forms.NumericUpDown yTranslation;
        private System.Windows.Forms.Label yTranslationLbl;
        private System.Windows.Forms.NumericUpDown zTranslation;
        private System.Windows.Forms.Label zTranslationLbl;
    }
}

