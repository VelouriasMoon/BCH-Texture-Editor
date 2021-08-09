using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SPICA.Formats.CtrH3D;
using ImageMagick;
using Microsoft.VisualBasic;

namespace BCH_Texture_Tool
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            groupBox1.Text = "";
        }
        public H3D Scene;

        private void Reset()
        {
            button1.Enabled = false;
            button2.Enabled = false;
            button3.Enabled = false;
            button4.Enabled = false;
            saveToolStripMenuItem.Enabled = false;
            pictureBox1.Image = null;
            pictureBox2.Image = null;
            label1.Text = "";
            groupBox1.Text = "";

            pictureBox1.Size = new Size(256, 256);
            pictureBox2.Location = new Point(270, 14);
            pictureBox2.Size = new Size(256, 256);
            ActiveForm.Size = new Size(766, 396);
            groupBox1.Size = new Size(535, 278);
            treeView1.Height = 318;
        }

        private void Open_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "BCH File (*.bch)|*.bch|All files (*.*)|*.*";
                openFileDialog.RestoreDirectory = true;
                openFileDialog.Title = "Open BCH File";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    OpenFile(openFileDialog.FileName);
                }
            }
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Reset();
            treeView1.Nodes.Clear();
            button1.Enabled = true;
            Scene = new H3D();
            label1.Text = "New BCH File";
            saveToolStripMenuItem.Enabled = true;
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "BCH File (*.bch)|*.bch|All files (*.*)|*.*";
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.RestoreDirectory = true;
                saveFileDialog.Title = "Save Signal File";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    H3D.Save(saveFileDialog.FileName, Scene);
                }
            }
        }

        private void OpenFile(string infile)
        {
            Reset();
            treeView1.Nodes.Clear();
            Scene = H3D.Open(File.ReadAllBytes(infile));

            if (Scene.Models.Count > 0)
            {
                MessageBox.Show("File Contains models", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                saveToolStripMenuItem.Enabled = false;
                return;
            }

            button1.Enabled = true;
            saveToolStripMenuItem.Enabled = true;
            label1.Text = Path.GetFileName(infile);

            if (Scene.Textures.Count <= 0)
                return;

            foreach (var texture in Scene.Textures)
            {
                treeView1.Nodes.Add(texture.Name);
            }
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            button2.Enabled = true;
            button3.Enabled = true;
            button4.Enabled = true;
            MagickImage texture = new MagickImage(new MagickFactory().Image.Create(Scene.Textures[treeView1.SelectedNode.Index].ToBitmap()));

            
            var channels = texture.Separate(Channels.Alpha).ToList();
            MagickImage alpha = new MagickImage();
            if (texture.HasAlpha)
            {
                alpha = new MagickImage(channels[0]);
            }

            texture.Alpha(AlphaOption.Off);

            if (texture.Width > 256)
            {
                pictureBox1.Size = new Size(texture.Width, texture.Height);
                pictureBox2.Location = new Point(pictureBox2.Location.X + (texture.Width - 256), pictureBox2.Location.Y);
                pictureBox2.Size = new Size(texture.Width, texture.Height);
                treeView1.Height = groupBox1.Height + 29;
            }
            else
            {
                pictureBox1.Size = new Size(256, 256);
                pictureBox2.Location = new Point(270, 14);
                pictureBox2.Size = new Size(256, 256);
            }

            pictureBox1.Image = texture.ToBitmap();
            pictureBox2.Image = alpha.ToBitmap();
            groupBox1.Text = Scene.Textures[treeView1.SelectedNode.Index].Name;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "PNG Image (*.png)|*.png";
                openFileDialog.RestoreDirectory = true;
                openFileDialog.Title = "Open PNG Image Texture";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    Bitmap texture = (Bitmap)Bitmap.FromFile(openFileDialog.FileName);
                    string filename = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                    int i = 0;

                    foreach (var text in Scene.Textures)
                    {
                        if (text.Name == filename)
                        {
                            i++;
                            filename = $"{filename}_{i}";
                        }
                    }

                    var newtext = new SPICA.Formats.CtrH3D.Texture.H3DTexture(filename, texture, SPICA.PICA.Commands.PICATextureFormat.RGBA8);
                    Scene.Textures.Add(newtext);
                    treeView1.Nodes.Add(filename);
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "PNG Image (*.png)|*.png";
                openFileDialog.RestoreDirectory = true;
                openFileDialog.Title = "Open PNG Image Texture";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    Bitmap texture = (Bitmap)Bitmap.FromFile(openFileDialog.FileName);
                    var newtext = new SPICA.Formats.CtrH3D.Texture.H3DTexture(Path.GetFileNameWithoutExtension(openFileDialog.FileName), texture, SPICA.PICA.Commands.PICATextureFormat.RGBA8);
                    Scene.Textures[treeView1.SelectedNode.Index].ReplaceData(newtext);
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Scene.Textures.Remove(Scene.Textures[treeView1.SelectedNode.Index]);
            treeView1.Nodes.Remove(treeView1.SelectedNode);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            string name = Interaction.InputBox("New Texture Name", "New Texture Name", Scene.Textures[treeView1.SelectedNode.Index].Name).Replace(" ", "");

            if (name == "")
                return;

            int i = 0;

            foreach (var text in Scene.Textures)
            {
                if (text.Name == name)
                {
                    i++;
                    name = $"{name}_{i}";
                }
            }

            Scene.Textures[treeView1.SelectedNode.Index].Name = name;
            treeView1.SelectedNode.Text = name;
        }
    }
}
