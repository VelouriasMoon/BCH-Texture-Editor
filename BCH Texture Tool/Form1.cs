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
using Microsoft.WindowsAPICodePack.Dialogs;
using FE3D.IO;
using FE3D;
using FE3D.GovanifY;

namespace BCH_Texture_Tool
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            label1.Text = "";
        }
        public H3D Scene;

        private void Reset()
        {
            button1.Enabled = false;
            button2.Enabled = false;
            button3.Enabled = false;
            button4.Enabled = false;
            button5.Enabled = false;
            button6.Enabled = false;
            button7.Enabled = false;
            saveToolStripMenuItem.Enabled = false;
            exportAllToolStripMenuItem.Enabled = false;
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

        #region Tool Strip
        private void Open_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Compatible File|*.bch;*.lz;*.arc|BCH File (*.bch)|*.bch|Lz File (*.lz)|*.lz|Arc File (*.arc)|*.arc|All files (*.*)|*.*";
                openFileDialog.RestoreDirectory = true;
                openFileDialog.Title = "Open File";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    if (Path.GetExtension(openFileDialog.FileName) == ".arc")
                        OpenArc(openFileDialog.FileName);
                    else
                        OpenFile(openFileDialog.FileName);
                }
            }
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Reset();
            treeView1.Nodes.Clear();
            button1.Enabled = true;
            button7.Enabled = true;
            Scene = new H3D();
            label1.Text = "New BCH File";
            saveToolStripMenuItem.Enabled = true;
            exportAllToolStripMenuItem.Enabled = true;
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Compatible File|*.bch;*.lz;*.arc|BCH File (*.bch)|*.bch|Lz File (*.lz)|*.lz|Arc File (*.arc)|*.arc|All files (*.*)|*.*";
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.RestoreDirectory = true;
                saveFileDialog.Title = "Save File";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    if (Path.GetExtension(saveFileDialog.FileName) == ".arc")
                    {
                        SaveArc(saveFileDialog.FileName);
                    }
                    else
                    {
                        if (Path.GetExtension(saveFileDialog.FileName) == ".lz")
                        {
                            byte[] file = FEIO.LZ13Compress(H3D.Save(Scene));

                            File.WriteAllBytes(saveFileDialog.FileName, file);
                        }
                        else
                        {
                            H3D.Save(saveFileDialog.FileName, Scene);
                        }
                    }
                }
            }
        }

        private void exportAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (CommonOpenFileDialog commonOpenFileDialog = new CommonOpenFileDialog())
            {
                commonOpenFileDialog.IsFolderPicker = true;
                commonOpenFileDialog.RestoreDirectory = true;
                commonOpenFileDialog.Title = "Save Textures";

                if (commonOpenFileDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    foreach (var texture in Scene.Textures)
                    {
                        Image image = texture.ToBitmap();
                        image.Save($"{commonOpenFileDialog.FileName}\\{texture.Name}.png", System.Drawing.Imaging.ImageFormat.Png);
                    }
                }
            }
        }

        #endregion

        private void OpenFile(string infile)
        {
            Reset();
            treeView1.Nodes.Clear();
            byte[] file = File.ReadAllBytes(infile);

            if (Path.GetExtension(infile) == ".lz" && file[0] == 0x13 && file[4] == 0x11)
            {
                file = file.Skip(4).ToArray();
                file = FEIO.LZ11Decompress(file);
            }

            if (FEIO.GetMagic(file) != "BCH")
                return;

            Scene = H3D.Open(file);

            if (Scene.Models.Count > 0)
            {
                MessageBox.Show("File Contains models", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                saveToolStripMenuItem.Enabled = false;
                return;
            }

            button1.Enabled = true;
            saveToolStripMenuItem.Enabled = true;
            exportAllToolStripMenuItem.Enabled = true;
            label1.Text = Path.GetFileName(infile);

            if (Scene.Textures.Count <= 0)
                return;

            foreach (var texture in Scene.Textures)
            {
                treeView1.Nodes.Add(texture.Name);
            }
        }

        private void OpenArc(string infile)
        {
            Reset();
            treeView1.Nodes.Clear();
            Scene = new H3D();
            FEArc arc = new FEArc();

            using (MemoryStream ms = new MemoryStream(File.ReadAllBytes(infile)))
            {
                BinaryStream bs = new BinaryStream(ms);
                arc.Read(bs);
            }
            int i = 0;

            foreach (byte[] file in arc.Files)
            {
                byte[] bch = FEIO.LZ11Decompress(file.Skip(4).ToArray());

                if (FEIO.GetMagic(bch) != "BCH")
                    continue;

                H3D NewBch = H3D.Open(bch);
                NewBch.Textures[0].Name = arc.FileNames[i].Replace(".bch.lz","");

                Scene.Textures.Add(NewBch.Textures[0]);
                i++;
            }

            button1.Enabled = true;
            saveToolStripMenuItem.Enabled = true;
            exportAllToolStripMenuItem.Enabled = true;
            label1.Text = Path.GetFileName(infile);

            if (Scene.Textures.Count <= 0)
                return;

            foreach (var texture in Scene.Textures)
            {
                treeView1.Nodes.Add(texture.Name);
            }
        }

        private void SaveArc(string outname)
        {
            if (File.Exists(outname))
                File.Delete(outname);
            List<byte[]> arcfiles = new List<byte[]>();
            List<string> names = new List<string>();
            foreach (var texture in Scene.Textures)
            {
                H3D NewBch = new H3D();
                NewBch.Textures.Add(texture);
                string filename = $"{NewBch.Textures[0].Name}.bch.lz";
                NewBch.Textures[0].Name = "tmp";
                NewBch.ConverterVersion = 44139;
                NewBch.BackwardCompatibility = 34;
                NewBch.ForwardCompatibility = 35;

                byte[] file = FEIO.LZ13Compress(H3D.Save(NewBch));

                arcfiles.Add(file);
                names.Add(filename);
            }

            using (FileStream fs = new FileStream(outname, FileMode.Create))
            {
                BinaryStream bs = new BinaryStream(fs);
                FEArc arc = new FEArc();

                arc.Files = arcfiles;
                arc.FileNames = names;

                arc.Write(bs);
            }
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            button2.Enabled = true;
            button3.Enabled = true;
            button4.Enabled = true;
            button5.Enabled = true;
            button6.Enabled = true;
            button7.Enabled = true;
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
                pictureBox2.Location = new Point(270 + (texture.Width - 256), pictureBox2.Location.Y);
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

        #region Buttons

        private void Import_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "PNG Image (*.png)|*.png";
                openFileDialog.RestoreDirectory = true;
                openFileDialog.Title = "Open PNG Image Texture";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    MagickImage texture = new MagickImage(new MagickImageFactory().Create(openFileDialog.FileName));
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

                    if (!texture.HasAlpha)
                    {
                        texture.Alpha(AlphaOption.Opaque);
                    }

                    var newtext = new SPICA.Formats.CtrH3D.Texture.H3DTexture(filename, texture.ToBitmap(), SPICA.PICA.Commands.PICATextureFormat.RGBA8);
                    Scene.Textures.Add(newtext);
                    treeView1.Nodes.Add(filename);
                }
            }
        }

        private void Replace_Click(object sender, EventArgs e)
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

        private void Remove_Click(object sender, EventArgs e)
        {
            Scene.Textures.Remove(Scene.Textures[treeView1.SelectedNode.Index]);
            treeView1.Nodes.Remove(treeView1.SelectedNode);
        }

        private void Rename_Click(object sender, EventArgs e)
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

        private void Export_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "PNG (*.png)|*.png|All files (*.*)|*.*";
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.RestoreDirectory = true;
                saveFileDialog.Title = "Save Texture Image";
                saveFileDialog.FileName = treeView1.SelectedNode.Text;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    Image texture = Scene.Textures[treeView1.SelectedNode.Index].ToBitmap();
                    texture.Save(saveFileDialog.FileName, System.Drawing.Imaging.ImageFormat.Png);
                }
            }
        }

        private void ExportSplit_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "PNG (*.png)|*.png|All files (*.*)|*.*";
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.RestoreDirectory = true;
                saveFileDialog.Title = "Save Texture Image";
                saveFileDialog.FileName = treeView1.SelectedNode.Text;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    pictureBox1.Image.Save($"{Path.GetDirectoryName(saveFileDialog.FileName)}\\{Path.GetFileNameWithoutExtension(saveFileDialog.FileName)}_RGB{Path.GetExtension(saveFileDialog.FileName)}", System.Drawing.Imaging.ImageFormat.Png);
                    pictureBox2.Image.Save($"{Path.GetDirectoryName(saveFileDialog.FileName)}\\{Path.GetFileNameWithoutExtension(saveFileDialog.FileName)}_A{Path.GetExtension(saveFileDialog.FileName)}", System.Drawing.Imaging.ImageFormat.Png);
                }
            }
        }

        private void ImportSplit_Click(object sender, EventArgs e)
        {
            string filename = "null";
            MagickImage rgb = new MagickImage();
            MagickImage alpha = new MagickImage();

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "PNG Image (*.png)|*.png";
                openFileDialog.RestoreDirectory = true;
                openFileDialog.Title = "Open RGB Texture";
                openFileDialog.Multiselect = false;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    Bitmap newtexture = (Bitmap)Bitmap.FromFile(openFileDialog.FileName);
                    pictureBox1.Image = newtexture;
                    filename = Path.GetFileNameWithoutExtension(openFileDialog.FileName).Replace("_RGB", "");
                    rgb = new MagickImage(new MagickImageFactory().Create(openFileDialog.FileName));
                }
                else
                    return;
            }
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "PNG Image (*.png)|*.png";
                openFileDialog.RestoreDirectory = true;
                openFileDialog.Title = "Open Alpha Texture";
                openFileDialog.Multiselect = false;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    Bitmap newtexture = (Bitmap)Bitmap.FromFile(openFileDialog.FileName);
                    pictureBox2.Image = newtexture;
                    alpha = new MagickImage(new MagickImageFactory().Create(openFileDialog.FileName));
                }
                else
                    return;
            }

            var splitrgb = rgb.Separate().ToList();
            var newimage = new MagickImageCollection();
            newimage.Add(splitrgb[0]);
            newimage.Add(splitrgb[1]);
            newimage.Add(splitrgb[2]);
            newimage.Add(alpha);

            Bitmap texture = newimage.Combine().ToBitmap();
            var newtext = new SPICA.Formats.CtrH3D.Texture.H3DTexture(filename, texture, SPICA.PICA.Commands.PICATextureFormat.RGBA8);
            Scene.Textures.Add(newtext);
            treeView1.Nodes.Add(filename);
        }

        #endregion
    }
}
