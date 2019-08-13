using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace Json2CSV
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        protected bool GetFilename(out string filename, DragEventArgs e)
        {
            bool ret = false;
            filename = String.Empty;

            if ((e.AllowedEffect & DragDropEffects.Copy) == DragDropEffects.Copy)
            {
                Array data = ((IDataObject)e.Data).GetData("FileName") as Array;
                if (data != null)
                {
                    if ((data.Length == 1) && (data.GetValue(0) is String))
                    {
                        filename = ((string[])data)[0];
                        string ext = Path.GetExtension(filename).ToLower();
                        if ((ext == ".jpg") || (ext == ".png") || (ext == ".bmp"))
                        {
                            ret = true;
                        }
                    }
                }
            }
            return ret;
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            foreach (var paths in filePaths)
            {
                if (!Json2CSV.Convert(paths))
                {
                    return;
                }
            }
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            foreach (var paths in filePaths)
            {
                if (paths.Substring(paths.LastIndexOf(".") + 1) != "json")
                    return;
            }

            e.Effect = DragDropEffects.Copy;
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            Pen pen = new Pen(Color.Green, 2.0F);
            pen.DashStyle = DashStyle.Dash;

            Brush brush = new SolidBrush(Color.FromArgb(230, 230, 230));
            
            e.Graphics.DrawRectangle(pen, 10, 10, this.ClientRectangle.Width - 20, this.ClientRectangle.Height - 20);
            e.Graphics.FillRectangle(brush, 10, 10, this.ClientRectangle.Width - 20, this.ClientRectangle.Height - 20);
            pen.Dispose();
        }
    }
}
