using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;

namespace AppLou1
{
    public partial class Form1 : Form
    {
        private int _index = 0;
        //here use a List of type of your baseclass
        private List<DrawnRectangle> _fList = new List<DrawnRectangle>();
        private float _zoomP1 = 1.0F;
        private bool _trackingP1 = false;
        private DrawnRectangle _currentPos = null;
        private float _xP1 = 0;
        private float _yP1 = 0;
        private float _rotationStart = 0;
        public Form1()
        {
            InitializeComponent();

            this.DoubleBuffered = true;

            DrawnRectangle dr = new DrawnRectangle(new RectangleF(24, 24, 200, 200), _index, 0, 0, new Pen(Color.Blue, 4));
            dr.ForceHQRendering = true;
            this._fList.Add(dr);
            _index++;

            DrawnRectangle dr2 = new DrawnRectangle(new RectangleF(124, 24, 200, 200), _index, 0, 0, new Pen(Color.Red, 4));
            dr2.ForceHQRendering = true;
            this._fList.Add(dr2);
            _index++;

            DrawnRectangle dr3 = new DrawnRectangle(new RectangleF(200, 24, 200, 200), _index, 0, 0, new Pen(Color.Green, 4));
            dr3.ForceHQRendering = true;
            this._fList.Add(dr3);
            _index++;

            //commentaire.

        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            for (int i = 0; i < this._fList.Count; i++)
                _fList[i].Draw(_fList[i].FLocation.Location, e.Graphics);
        }

        private void this_MouseDown(object sender, MouseEventArgs e)
        {
            float ix = (e.X - this.AutoScrollPosition.X) / _zoomP1;
            float iy = (e.Y - this.AutoScrollPosition.Y) / _zoomP1;

            DrawnRectangle picpos = HitTest(new PointF(ix, iy));

            if ((e.Button == MouseButtons.Left | e.Button == MouseButtons.Right) && ((picpos != null)))
            {
                _currentPos = picpos;
                _xP1 = ix - picpos.FLocation.X;
                _yP1 = iy - picpos.FLocation.Y;
                _trackingP1 = true;

                _rotationStart = Convert.ToSingle(Math.Atan2(_yP1, _xP1) / (Math.PI / 180)) - _currentPos.Rotation;

                this.Invalidate();
            }
        }

        private void this_MouseMove(object sender, MouseEventArgs e)
        {
            float ix = Convert.ToSingle((e.X - this.AutoScrollPosition.X) / _zoomP1);
            float iy = Convert.ToSingle((e.Y - this.AutoScrollPosition.Y) / _zoomP1);

            if (_trackingP1 && e.Button == MouseButtons.Left)
            {
                _currentPos.FLocation = new RectangleF(new PointF(ix - _xP1, iy - _yP1), _currentPos.FLocation.Size);
                this.Invalidate();
            }

            if (_trackingP1 && e.Button == MouseButtons.Right)
            {
                PointF f = _currentPos.FLocation.Location;

                float pX = ix - f.X;
                float pY = iy - f.Y;

                _currentPos.Rotation = Convert.ToSingle(Math.Atan2(pY, pX) / (Math.PI / 180)) - _rotationStart;

                if (_currentPos.Rotation > 360f)
                {
                    _currentPos.Rotation -= 360;
                }

                if (_currentPos.Rotation < -360f)
                {
                    _currentPos.Rotation += 360;
                }

                this.Invalidate();
            }
        }

        private void this_MouseUp(object sender, MouseEventArgs e)
        {
            _trackingP1 = false;
            _currentPos = null;
            this.Invalidate();
        }

        private DrawnRectangle HitTest(PointF p)
        {

            for (int i = _fList.Count - 1; i >= 0; i += -1)
            {
                GraphicsPath gp = new GraphicsPath();
                Matrix m = new Matrix(1, 0, 0, 1, this.AutoScrollPosition.X, this.AutoScrollPosition.Y);

                gp.AddRectangle(_fList[i].FLocation);

                if (_fList[i].Rotation != 0)
                {
                    m.RotateAt(_fList[i].Rotation, _fList[i].FLocation.Location, MatrixOrder.Prepend);
                }

                gp.Transform(m);

                if (gp.IsVisible(p))
                {
                    ReSortFList(i);
                    gp.Dispose();
                    return _fList[_fList.Count - 1];
                }

                gp.Dispose();
            }

            return null;
        }

        private void ReSortFList(int indx)
        {
            //wegen z-index neu sortieren, Bild an mouseposition soll ganz nach oben

            DrawnRectangle tmp = _fList[indx];

            for (int i = indx; i < _fList.Count - 1; i++)
            {
                _fList[i] = _fList[i + 1];
            }

            _fList[_fList.Count - 1] = tmp;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (this.saveFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                bool b = SerializeFList(this.saveFileDialog1.FileName);

                if (b)
                    MessageBox.Show("success!");
                else
                    MessageBox.Show("error.");
            }
        }

        private bool SerializeFList(string FileName)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = null;

            bool bError = false;

            try
            {
                object[] o = new object[this._fList.Count];

                for (int i = 0; i < _fList.Count; i++)
                    o[i] = _fList[i];

                stream = new FileStream(FileName, FileMode.Create, FileAccess.Write, FileShare.None);
                formatter.Serialize(stream, o);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                bError = true;
            }

            try
            {
                stream.Close();
                stream = null;
            }
            catch
            {

            }

            return !bError;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (this.openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                DeserializeFList(this.openFileDialog1.FileName);
            }
        }

        private void DeserializeFList(string FileName)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = null;
            List<DrawnRectangle> gList = new List<DrawnRectangle>();
            object[] o = null;

            try
            {
                stream = new FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                o = (object[])formatter.Deserialize(stream);

                for (int i = 0; i < o.Length; i++)
                    gList.Add((DrawnRectangle)o[i]);

                this._fList = null;
                this._fList = gList;

                this.Invalidate();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            try
            {
                stream.Close();
                stream = null;
            }
            catch
            {

            }
        }
    }

    //Create an Interface or a mustinherit-baseclass for the methods used in all types
    [Serializable]
    public class DrawnRectangle : ISerializable, IDisposable, ICloneable
    {
        public RectangleF FLocation { get; set; }
        public int ZPosition { get; set; }
        public int ID { get; set; }
        public float Rotation { get; set; }
        //further Properties like zoom etc...

        public bool ForceHQRendering { get; set; }
        public Pen FPen { get; set; }

        public DrawnRectangle(RectangleF floc, int id, int zpos, float rot, Pen p)
        {
            FLocation = floc;
            ID = id;
            ZPosition = zpos;
            Rotation = rot;
            FPen = p;
        }

        protected DrawnRectangle(SerializationInfo info, StreamingContext context)
        {
            this.FLocation = (RectangleF)info.GetValue("_flocation", typeof(RectangleF));
            this.ZPosition = (int)info.GetValue("_z", typeof(Int32));
            this.ID = (int)info.GetValue("_id", typeof(Int32));
            this.Rotation = (float)info.GetValue("_rotation", typeof(float));
            this.ForceHQRendering = (bool)info.GetValue("_hq", typeof(bool));

            int w = (int)info.GetValue("_pWidth", typeof(Int32));
            Color c = (Color)info.GetValue("_pCol", typeof(Color));
            this.FPen = new Pen(c, w);
        }

        public object Clone()
        {
            return new DrawnRectangle(this.FLocation, this.ID, this.ZPosition, this.Rotation, (Pen)this.FPen.Clone());
        }

        public void Dispose()
        {
            //dispose any unmanaged data here
            this.FPen.Dispose();
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("_flocation", this.FLocation);
            info.AddValue("_z", this.ZPosition);
            info.AddValue("_id", this.ID);
            info.AddValue("_rotation", this.Rotation);
            info.AddValue("_hq", this.ForceHQRendering);

            //this is only for demo - you'd have to implement a helper class for that
            //which parses every possible Property of every possiblöe Brush and Pen
            //like Alignment, StartCap, Colors, WrapModes etc
            info.AddValue("_pCol", FPen.Color);
            info.AddValue("_pWidth", FPen.Width);
        }

        public void Draw(PointF DoRotationAt, Graphics g)
        {
            if (ForceHQRendering)
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            }

            GraphicsContainer con = g.BeginContainer();

            if (this.Rotation != 0f)
            {
                Matrix mx = g.Transform;
                mx.RotateAt(this.Rotation, DoRotationAt, MatrixOrder.Append);
                g.Transform = mx;
            }

            this.Draw(g);

            g.EndContainer(con);
        }

        public void Draw(Graphics g)
        {
            if (ForceHQRendering)
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            }

            g.DrawRectangle(this.FPen, this.FLocation.X, this.FLocation.Y, this.FLocation.Width, this.FLocation.Height);
        }
    }
}