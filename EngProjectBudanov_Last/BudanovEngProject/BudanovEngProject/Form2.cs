using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using OpenCvSharp.Extensions;
using OpenCvSharp;
using System.Drawing.Imaging;

namespace BudanovEngProject
{
    public partial class Form2 : Form
    {
        public Form2(Mat QRsource)
        {
            InitializeComponent();
            this.QRsource = QRsource;
        }
        Mat QRsource;
        Bitmap resultQR;
        double minLength = 6; //6
        double sideLength = 162;

        private double SmallestContour ()
        {
            Mat invQR = new Mat();
            Cv2.Threshold(QRsource, invQR, 215, 255, ThresholdTypes.TozeroInv);
            Mat grey = new Mat();
            Cv2.CvtColor(invQR, grey, ColorConversionCodes.BGRA2GRAY);
            OpenCvSharp.Point[][] found_contours;
            HierarchyIndex[] contours_indexes;
            Cv2.FindContours(grey, out found_contours, out contours_indexes, mode: RetrievalModes.External, method: ContourApproximationModes.ApproxSimple);

            var contourIndex = 0;
            var previousArea = 0;
            OpenCvSharp.Point[] finalContour = found_contours[contourIndex];
            var smallestContourRect = Cv2.BoundingRect(found_contours[0]);
            int index_counter = 0;
            while ((contourIndex >= 0))
            {
                var contour = found_contours[contourIndex];
                finalContour = contour;
                var boundingRect = Cv2.BoundingRect(contour); //Find bounding rect for each contour
                var boundingRectArea = boundingRect.Width * boundingRect.Height;

                if (boundingRectArea < previousArea)
                {
                    if (foundRect(boundingRect.Width, boundingRect.Height) && boundingRectArea > 30)
                    {
                        smallestContourRect = boundingRect;
                        previousArea = boundingRectArea;
                        index_counter = contourIndex;
                    }
                }
                contourIndex = contours_indexes[contourIndex].Next;
            }

            //double diag = Math.Sqrt(Math.Pow() + Math.Pow());
            minLength = smallestContourRect.Bottom - smallestContourRect.Top;

            Cv2.WaitKey(1);

            //Cv2.ImShow("SC", invQR);
            return minLength;
        }

        public bool foundRect(int width, int height)
        {
            bool AllRectFound;
            if (width + 20 > height & width - 20 < height)
                AllRectFound = true;
            else
                AllRectFound = false;

            return AllRectFound;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Dispose();
            this.Close();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            SmallestContour();
            QRDecode();
            pictureBox1.Image = resultQR;
            label1.Text = minLength.ToString();
            //textBox1.Text = 
        }

        private void QRDecode()
        {
            Form1 MainForm = new Form1();
            int picBoxArea = MainForm.pictureBox3.Width * MainForm.pictureBox3.Height;
            Bitmap WorkingBmp = BitmapConverter.ToBitmap(QRsource);
            Bitmap QRbmp = new Bitmap(MainForm.pictureBox3.Width, MainForm.pictureBox3.Height);

            int quantityOfCells = (int)(sideLength / minLength);
            //Byte[,] bytes = new byte[sideLength/minLength, sideLength / minLength];
            Bitmap[,] cutBMP = new Bitmap[(int) (sideLength / minLength), (int)(sideLength / minLength)];

            double X = 0;
            double Y = 0;
            for (int i = 0; i < quantityOfCells; i++)
            {
                Y = 0;
                for (int j = 0; j < quantityOfCells; j++)
                {
                    cutBMP[i, j] = WorkingBmp.Clone(new Rectangle(new System.Drawing.Point((int)(X), (int)(Y)), new System.Drawing.Size((int)(minLength), (int)(minLength))), PixelFormat.Format32bppArgb);
                    Y += minLength;
                }
                X += minLength;
            }

            for (int i = 0; i < cutBMP.GetLength(0); i++)
            {
                for (int j = 0; j < cutBMP.GetLength(1); j++)
                {
                    if (CalculatePixelPercent(cutBMP[i, j], 200) > 40)
                        using (var g = Graphics.FromImage(cutBMP[i, j]))
                            g.Clear(Color.Black);
                    else
                        using (var g = Graphics.FromImage(cutBMP[i, j]))
                            g.Clear(Color.White);
                }
            }

            X = 0;
            Y = 0;
            for (int i = 0; i < cutBMP.GetLength(0); i++)
            {
                Y = 0;
                for (int j = 0; j < cutBMP.GetLength(1); j++)
                {
                    Graphics g = Graphics.FromImage(QRbmp);
                    g.DrawImage(cutBMP[i, j], (int)(X), (int)(Y), (int)(minLength), (int)(minLength));
                    g.Dispose();
                    Y += minLength;
                }
                X += minLength;
            }

            resultQR = QRbmp;            
        }

        double CalculatePixelPercent(Bitmap bm, int Red)
        {
            double pixelCount = 0.0;
            double allPixelCount = bm.Width * bm.Height;
            for (int i = 0; i < bm.Width; i++)
            {
                for (int j = 0; j < bm.Height; j++)
                {
                    if (bm.GetPixel(i, j).R < Red)
                    {
                        pixelCount++;
                    }
                }
            }
            return pixelCount / allPixelCount * 100;
        }
    }
}
