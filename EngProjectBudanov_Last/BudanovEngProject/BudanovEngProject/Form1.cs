using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Threading;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using OpenCvSharp.Internal.Vectors;
using OpenCvSharp.Internal;
using System.IO;


namespace BudanovEngProject
{
    public partial class Form1 : Form
    {

        

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            button1.Text = "Start";
        }


        QRCodeDetector detector = new QRCodeDetector();
        VideoCapture capture;
        Mat frame;
        Bitmap image;
        private Thread camera;
        bool isCameraRunning = false;
        Mat MeanCInv = new Mat();
        Mat dst;
        Mat src;
        public Mat QRsource;


        private void CaptureCamera()
        {
            camera = new Thread(new ThreadStart(CaptureCameraCallback));
            camera.Start();
        }        


        private void Binarization(Mat frame)
        {
            OpenCvSharp.Point[][] found_contours;
            HierarchyIndex[] contours_indexes;

            dst = new Mat();
            src = frame;
            Cv2.CvtColor(src, dst, ColorConversionCodes.BGR2GRAY);

            Scalar loweb = new Scalar(225, 225, 225);
            Scalar upperb = new Scalar(255, 255, 255);

            Cv2.InRange(src, loweb, upperb, dst); // Опционально

            Cv2.Threshold(dst, MeanCInv, 215, 255, ThresholdTypes.Tozero);

            Cv2.FindContours(MeanCInv, out found_contours, out contours_indexes, mode: RetrievalModes.CComp, method: ContourApproximationModes.ApproxSimple);

            if (found_contours.Length == 0)
            {
                pictureBox2.Image = ResizeImage(BitmapConverter.ToBitmap(dst), pictureBox2.Width, pictureBox2.Height);
                
            }

            var contourIndex = 0;
            var previousArea = 0;
            OpenCvSharp.Point[] finalContour = found_contours[contourIndex];

            var biggestContourRect = Cv2.BoundingRect(found_contours[0]);
            int index_counter = 0;
            while ((contourIndex >= 0))
            {
                var contour = found_contours[contourIndex];
                finalContour = contour;
                var boundingRect = Cv2.BoundingRect(contour); //Find bounding rect for each contour
                var boundingRectArea = boundingRect.Width * boundingRect.Height;
                
                if (boundingRectArea > previousArea)
                {
                    if (foundRect(boundingRect.Width, boundingRect.Height))
                    {
                        biggestContourRect = boundingRect;
                        previousArea = boundingRectArea;
                        index_counter = contourIndex;
                    }
                }
                contourIndex = contours_indexes[contourIndex].Next;
            }

            OpenCvSharp.Point[] points_marker = Cv2.ApproxPolyDP(found_contours[index_counter], 4, true);

            var QR = new Mat(src, biggestContourRect); //Crop the image
            Cv2.CvtColor(QR, QR, ColorConversionCodes.BGRA2GRAY);         

            promising_transformation(ref dst, points_marker, biggestContourRect.Width, biggestContourRect.Height);

            pictureBox2.Image = ResizeImage(BitmapConverter.ToBitmap(QR), pictureBox2.Width, pictureBox2.Height);
            
            QRsource = dst;
            

            Cv2.WaitKey(1); // do events
        }
        


        void promising_transformation(ref Mat input, OpenCvSharp.Point[] Aproxy_Point, int Width, int Height)
        {
            Point2d[] Points_2d = new Point2d[4];          
            Mat Clone_input = input.Clone();
            for (int i = 0; i < 4; i++)
            {
                try
                {
                    Points_2d[i] = Aproxy_Point[i];
                    
                }
                catch { }

                // новые точки сторон
                IEnumerable<Point2d> AnglePoint = new List<Point2d>()
            {
                new Point2d(0, 0),
                new Point2d(0, Height),
                new Point2d(Width, Height),
                new Point2d(Width, 0)
            };
                Mat New_Mat_Markers_Points = Cv2.FindHomography(Points_2d, AnglePoint);
                Mat persperctiveQR = new Mat(Width, Height, MatType.CV_8U);
                Cv2.WarpPerspective(Clone_input, input, New_Mat_Markers_Points, new OpenCvSharp.Size(Width, Height));
                
            }
            
        }


        public bool  foundRect(int width, int height)
        {
            bool AllRectFound;
            if (width + 20 > height & width - 20 < height)
                AllRectFound = true;
            else
                AllRectFound = false;

            return AllRectFound;
        }
        


        private void CaptureCameraCallback()
        {

            frame = new Mat();
            capture = new VideoCapture(0);
            capture.Open(0);

            if (capture.IsOpened())
            {
                while (isCameraRunning)
                {
                    try
                    {
                        capture.Read(frame);
                        image = BitmapConverter.ToBitmap(frame);
                    }
                    catch { }

                    if (pictureBox1.Image != null)
                    {
                        pictureBox1.Image.Dispose();
                    }
                    pictureBox1.Image = ResizeImage(image, pictureBox1.Width ,pictureBox1.Height);

                    if (pictureBox2.Image != null)
                        pictureBox2.Image.Dispose();

                    try
                    {
                        Binarization(frame);
                        pictureBox3.Image = ResizeImage(BitmapConverter.ToBitmap(QRsource), pictureBox3.Width, pictureBox3.Height);
                        Point2f[] rectPoints;
                        if (detector.DetectMulti(QRsource, out rectPoints))
                        {
                            Form2 QRForm = new Form2(BitmapConverter.ToMat((Bitmap)pictureBox3.Image));
                            QRForm.ShowDialog();
                        }
                            
                    }
                    catch { }                 
                }
            }
        }

        

        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text.Equals("Start"))
            {
                CaptureCamera();
                button1.Text = "Stop";
                isCameraRunning = true;
            }
            else
            {
                capture.Release();
                button1.Text = "Start";
                isCameraRunning = false;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //capture.Release();
            //this.Dispose();
        }

        
    }
}
