using System;
using System.Drawing;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace ImageProcessorLib
{
    public class Mask
    {
        private int avenum = 1;
        private int contrast = 30;
        private int erode = 1;
        private DIAMOND_GROUPING diamond_group = DIAMOND_GROUPING.RBC;

        protected Mat src;

        protected MASK_TYPE type = MASK_TYPE.MASK_DEFAULT;
        protected double kThreshold = 185;
        protected double hullThreshold = 125;
        protected double cannyThreshold1 = 25;
        protected double cannyThreshold2 = 75;
        protected double KThresholdCal { get; private set; }
        protected double HullThresholdCal { get; private set; }
        protected double CannyThreshold1Cal { get; private set; }
        protected double CannyThreshold2Cal { get; private set; }
        protected string saveMaskDataPath = "";
        //protected bool UseNewMask { get; private set; }
        //protected bool DustDetectOn { get; private set; }
        protected bool ConvexHullOnMask { get; private set; }
        protected double kThresholdLab = 100;
        //protected bool UseMeleeMask { get; private set; }

        protected Mat Img_mask_spc { get; private set; }
        public double Length { get; private set; }
        public double Area { get; private set; }
        protected double Width { get; private set; }
        protected double Height { get; private set; }
        protected double Pvheight { get; private set; }
        protected double Area2 { get; private set; }

        public Mask(System.Drawing.Bitmap bmp)
        {
            src = BitmapConverter.ToMat(bmp);
            LoadMaskSettings();
        }

        public Mask(Mat s)
        {
            src = s.Clone();
            LoadMaskSettings();
        }

        public Mask()
        {
            LoadMaskSettings();
        }

        public void SetSrc(Bitmap bmp)
        {
            src = BitmapConverter.ToMat(bmp);
        }

        public void SetSrc(Mat img)
        {
            //Bitmap bmp = BitmapConverter.ToBitmap(img);
            //src = BitmapConverter.ToMat(bmp);
            src = img;
        }

        public int GetAveNum()
        {
            return avenum;
        }

        public void SetAveNum(int num)
        {
            avenum = num;
        }

        public MASK_TYPE GetMaskType()
        {
            return type;
        }

        protected void SaveState(Mat img_mask_spc, 
            double length, double area, double width, double height, double pvheight, double area2)
        {
            Img_mask_spc = img_mask_spc;
            Length = length;
            Area = area;
            Width = width;
            Height = height;
            Pvheight = pvheight;
            Area2 = area2;
        }

        protected void SaveState(double length, double area, double width, double height, double pvheight)
        {
            Length = length;
            Area = area;
            Width = width;
            Height = height;
            Pvheight = pvheight;
        }

        public bool CreateGrabCut(ref System.Drawing.Bitmap src, out System.Drawing.Bitmap dst, bool displayWindows = false)
        {
            dst = null;
            Mat srcImg = BitmapConverter.ToMat(src);
            Cv2.CvtColor(srcImg, srcImg, ColorConversionCodes.BGRA2BGR);
            Mat mask = new Mat(new OpenCvSharp.Size(src.Width, src.Height), MatType.CV_8UC1, 0);

            //dilate process 
            //Cv2.Dilate(srcImg, dstImg, new Mat());

            //grabcut
            //Mat bgdModel = new Mat(new OpenCvSharp.Size(65, 1), MatType.CV_64FC1);
            //Mat fgdModel = new Mat(new OpenCvSharp.Size(65, 1), MatType.CV_64FC1);

            Mat bgdModel = new Mat();
            Mat fgdModel = new Mat();

            OpenCvSharp.Rect r = new OpenCvSharp.Rect(50, 50, (int)Width - 100, (int)Height - 100);
            Cv2.GrabCut(srcImg, mask, r, bgdModel, fgdModel, 1, GrabCutModes.InitWithRect);

            for (int i = mask.Cols / 2 - 50; i < mask.Cols / 2 + 50; i++)
            {
                for (int j = mask.Rows / 2 - 25; j < mask.Rows / 2 + 75; j++)
                {
                    mask.Set<byte>(j, i, 1);
                }
            }

            Cv2.GrabCut(srcImg, mask, r, bgdModel, fgdModel, 1, GrabCutModes.InitWithMask);

            for (int i = 0; i < mask.Cols; i++)
            {
                for (int j = 0; j < mask.Rows; j++)
                {
                    byte e = mask.Get<byte>(j, i);
                    if (e == 0 | e == 2)
                    {
                        mask.Set<byte>(j, i, 0);
                    }
                    else
                    {
                        mask.Set<byte>(j, i, 255);
                    }
                }
            }
            Mat res = srcImg.Clone();

            dst = BitmapConverter.ToBitmap(mask);
            return true;
        }

        //private System.Drawing.Bitmap BitmapImage2Bitmap(BitmapImage bitmapImage)
        //{
        //    using (MemoryStream outStream = new MemoryStream())
        //    {
        //        BitmapEncoder enc = new BmpBitmapEncoder();
        //        enc.Frames.Add(BitmapFrame.Create(bitmapImage));
        //        enc.Save(outStream);
        //        System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(outStream);

        //        return new System.Drawing.Bitmap(bitmap);
        //    }
        //}

        //public BitmapImage ToBitmapImage(System.Drawing.Bitmap bitmap)
        //{
        //    using (var memory = new MemoryStream())
        //    {
        //        bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
        //        memory.Position = 0;

        //        var bitmapImage = new BitmapImage();
        //        bitmapImage.BeginInit();
        //        bitmapImage.StreamSource = memory;
        //        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        //        bitmapImage.EndInit();
        //        bitmapImage.Freeze();

        //        return bitmapImage;
        //    }
        //}

        public virtual bool Create(out System.Drawing.Bitmap mask, out System.Drawing.Bitmap mask2, int brightAreaThreshold = -1, int darkAreaThreshold = -1, bool displayWindows = false)
        {
            bool result = false;
            mask = null;
            mask2 = null;
            Mat maskMat = null;
            Mat maskMat2 = null;
            if (Create(out maskMat, out maskMat2, brightAreaThreshold, darkAreaThreshold, displayWindows))
            {
                mask = BitmapConverter.ToBitmap(maskMat);
                if (maskMat2 != null)
                {
                    mask2 = BitmapConverter.ToBitmap(maskMat2);
                }
                result = true;
            }

            return result;
        }

        public virtual bool Create(out Mat mask, out Mat img_mask_spc, int brightAreaThreshold = -1, int darkAreaThreshold = -1, bool displayWindows = false)
        {
            mask = null;
            img_mask_spc = null;
            double length, area, width, height, pvheight, area2;
            int num_smooth = avenum;
            int contrast = this.contrast;
            int filter_size = 3;
            //int brightAreaThreshold = -1;
            //int darkAreaThreshold = -1;
            try
            {
                Bitmap bitmap = CreateObjectMask(src.ToBitmap(), /*out img_mask,*/
                    out length, out area, out width, out height, 
                    out pvheight, num_smooth, contrast, cannyThreshold1, cannyThreshold2, 
                    out img_mask_spc, out area2, filter_size, 
                    brightAreaThreshold, darkAreaThreshold);

                mask = BitmapConverter.ToMat(bitmap);
                SaveState(img_mask_spc, length, area, width, height, pvheight, area2);

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            return false;
        }

        private void LoadMaskSettings()
        {
            try
            {
                string currentDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);

                using (StreamReader sr = new StreamReader(currentDirectory + @"\maskSettings.txt"))
                {
                    String line;
                    // Read and display lines from the file until the end of 
                    // the file is reached.
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line[0] != '!')
                        {
                            string value = line.Substring(line.IndexOf('=') + 1);
                            if (line.Contains("kThresholdLab"))
                                kThresholdLab = Convert.ToDouble(value);
                            else if (line.Contains("kThresholdCal"))
                                KThresholdCal = Convert.ToDouble(value);
                            else if (line.Contains("hullThresholdCal"))
                                HullThresholdCal = Convert.ToDouble(value);
                            else if (line.Contains("cannyThreshold1Cal"))
                                CannyThreshold1Cal = Convert.ToDouble(value);
                            else if (line.Contains("cannyThreshold2Cal"))
                                CannyThreshold2Cal = Convert.ToDouble(value);
                            else if (line.Contains("kThreshold"))
                                kThreshold = Convert.ToDouble(value);
                            else if (line.Contains("hullThreshold"))
                                hullThreshold = Convert.ToDouble(value);
                            else if (line.Contains("cannyThreshold1"))
                                cannyThreshold1 = Convert.ToDouble(value);
                            else if (line.Contains("cannyThreshold2"))
                                cannyThreshold2 = Convert.ToDouble(value);
                            else if (line.Contains("SaveMaskDataPath"))
                                saveMaskDataPath = value;
                            //else if (line.Contains("UseNewMask"))
                            //    UseNewMask = Convert.ToBoolean(value);
                            //else if (line.Contains("UseMeleeMask"))
                            //    UseMeleeMask = Convert.ToBoolean(value);
                            //else if (line.Contains("DustDetectOn"))
                            //    DustDetectOn = Convert.ToBoolean(value);
                            else if (line.Contains("ConvexHullOnMask"))
                                ConvexHullOnMask = Convert.ToBoolean(value);
                            else if (line.Contains("erode"))
                                erode = Convert.ToInt16(value);
                            else if (line.Contains("contrast"))
                                contrast = Convert.ToInt16(value);
                            else if (line.Contains("erode"))
                                avenum = Convert.ToInt16(value);
                        }
                    }
                }
            }
            catch
            {
                kThreshold = 185;
                kThresholdLab = 100;
                hullThreshold = 125;
                cannyThreshold1 = 25;
                cannyThreshold2 = 75;
                KThresholdCal = 55;
                HullThresholdCal = 25;
                CannyThreshold1Cal = 25;
                CannyThreshold2Cal = 75;
                saveMaskDataPath = "";
                //UseNewMask = false;
                //DustDetectOn = false;
                ConvexHullOnMask = false;
                //UseMeleeMask = false;
                avenum = 1;
                contrast = 30;
                erode = 1;
            }

        }

        private byte[] CalcLut(int contrast, int brightness)
        {
            byte[] lut = new byte[256];
            if (contrast > 0)
            {
                double delta = 127.0 * contrast / 100;
                double a = 255.0 / (255.0 - delta * 2);
                double b = a * (brightness - delta);
                for (int i = 0; i < 256; i++)
                {
                    int v = (int)Math.Round(a * i + b);
                    if (v < 0) v = 0;
                    if (v > 255) v = 255;
                    lut[i] = (byte)v;
                }
            }
            else
            {
                double delta = -128.0 * contrast / 100;
                double a = (256.0 - delta * 2) / 255.0;
                double b = a * brightness + delta;

                for (int i = 0; i < 256; i++)
                {
                    int v = (int)Math.Round(a * i + b);

                    if (v < 0) v = 0;
                    if (v > 255) v = 255;
                    lut[i] = (byte)v;
                }
            }
            return lut;
        }

        private Bitmap CreateObjectMask(Bitmap img, /*out IplImage image_mask,*/
            out double mask_length, out double mask_area, out double mask_width, out double mask_height,
            out double mask_pvheight, int num_smooth, int contrast, double canny1, double canny2,
            out Mat image_mask_spc, out double mask2_area, int filter_size = 3, 
            int brightAreaThreshold = -1, int darkAreaThreshold = -1)
        {
            Bitmap dst = null;
            //IplImage img_mask = Cv.CreateImage(new CvSize(img.Width, img.Height), BitDepth.U8, 1);
            Mat img_mask = new Mat(new OpenCvSharp.Size(img.Width, img.Height), MatType.CV_8UC1, 0);
            image_mask_spc = null;
            mask_length = mask_area = mask_width = mask_height = mask_pvheight = mask2_area = 0;

            Mat img_gray;
            Mat img_canny;
            Mat img_mask_copy;

            int i, x, y, offset;
            IntPtr ptr;
            Byte pixel;

            //////////////////  
            var distance = new List<double>();
            double center_x = 0;
            double center_y = 0;
            double center_count = 0;
            double distance_mean = 0;
            double distance_stddev = 0;
            double sum_m = 0;
            double sum_v = 0;
            double temp = 0;
            //////////////////

            ////////////////////////////////////////////////////////////
            ////////////////////////Mask make///////////////////////////
            ////////////////////////////////////////////////////////////
            img_gray = new Mat(new OpenCvSharp.Size(img.Width, img.Height), MatType.CV_8UC1, 0);
            img_canny = new Mat(new OpenCvSharp.Size(img.Width, img.Height), MatType.CV_8UC1, 0);
            img_mask_copy = new Mat(new OpenCvSharp.Size(img.Width, img.Height), MatType.CV_8UC1, 0);

            Mat src = BitmapConverter.ToMat(img);
            Cv2.CvtColor(src, img_gray, ColorConversionCodes.BGR2GRAY);

            //Contrast -> Increase the edge contrast for transparent diamond
            byte[] lut = CalcLut(contrast, 0);
            //img_gray.LUT(img_gray, lut);
            Cv2.LUT(img_gray, lut, img_gray);

            //Median filter -> Eliminate point noise in the image



            //Elimination of big dusts should be coded here 
            if (num_smooth > 0)
            {
                //for (i = 0; i < num_smooth; i++) img_gray.Smooth(img_gray, SmoothType.Median, 3, 3, 0, 0);
                //for (i = 0; i < num_smooth; i++) img_gray.Smooth(img_gray, SmoothType.Median, filter_size, filter_size, 0, 0);
                for (i = 0; i < num_smooth; i++) Cv2.MedianBlur(img_gray, img_gray, filter_size);

                img_canny = img_gray.Canny(canny1, canny2);
            }
            else
            {
                img_canny = img_gray.Canny(canny1, canny2);
            }

            /////////////////////////////////////////////////////////////
            //ConvexHull
            /////////////////////////////////////////////////////////////
           
            //OpenCvSharp.CvMemStorage storage = new CvMemStorage(0);
            //CvSeq points = Cv.CreateSeq(SeqType.EltypePoint, CvSeq.SizeOf, CvPoint.SizeOf, storage);
            //CvSeq<CvPoint> points = new CvSeq<CvPoint>(SeqType.EltypePoint, CvSeq.SizeOf, storage);
            //CvPoint pt;

            List<OpenCvSharp.Point> points = new List<OpenCvSharp.Point>();
            OpenCvSharp.Point pt;

            ptr = img_canny.Data;
            for (y = 0; y < img_canny.Height; y++)
            {
                for (x = 0; x < img_canny.Width; x++)
                {
                    offset = (img_canny.Width * y) + (x);
                    pixel = Marshal.ReadByte(ptr, offset);
                    if (pixel > 0)
                    {
                        pt.X = x;
                        pt.Y = y;
                        points.Add(pt);
                        //////////////////////
                        center_x = center_x + x;
                        center_y = center_y + y;
                        center_count++;
                        //////////////////////
                    }
                }
            }

            center_x = center_x / center_count;
            center_y = center_y / center_count;

            //CvPoint[] hull;
            //CvMemStorage storage1 = new CvMemStorage(0);
            //CvSeq<CvPoint> contours;
            //List<Mat> hull = new List<Mat>();
            MatOfPoint hull = new MatOfPoint();

            int x_min = 3000, x_max = 0, y_min = 3000, y_max = 0;
            int y_x_min = 3000, y_x_max = 3000;

            if (points.Count > 0)
            {
                //Calcurate Ave and Std of distance from each edge points to the weighed center 
                for (i = 0; i < points.Count; i++)
                {
                    pt = points[i];
                    temp = Math.Sqrt((pt.X - center_x) * (pt.X - center_x) + (pt.Y - center_y) * (pt.Y - center_y));
                    distance.Add(temp);
                    sum_m += temp;
                    sum_v += temp * temp;
                }

                distance_mean = sum_m / points.Count;
                temp = (sum_v / points.Count) - distance_mean * distance_mean;
                distance_stddev = Math.Sqrt(temp);

                // Outlier elimination
                for (i = points.Count - 1; i >= 0; i--)
                {
                    if (distance[i] > (distance_mean + 3.0 * distance_stddev)) points.RemoveAt(i);
                }

                Cv2.ConvexHull(MatOfPoint.FromArray(points), hull, true);


                //2014/4/14 Add calc mask_width, mask_height and mask_pvheight

                foreach (OpenCvSharp.Point item in hull)
                {
                    if (x_min > item.X)
                    {
                        x_min = item.X;
                        y_x_min = item.Y;
                    }
                    else if (x_min == item.X && y_x_min > item.Y)
                    {
                        y_x_min = item.Y;
                    }

                    if (x_max < item.X)
                    {
                        x_max = item.X;
                        y_x_max = item.Y;
                    }
                    else if (x_max == item.X && y_x_max > item.Y)
                    {
                        y_x_max = item.Y;
                    }

                    if (y_min > item.Y) y_min = item.Y;
                    if (y_max < item.Y) y_max = item.Y;
                }
                mask_width = x_max - x_min;
                mask_height = y_max - y_min;
                mask_pvheight = ((double)y_x_max + (double)y_x_min) / 2 - (double)y_min;

                /////////////////////////////////////////////////////////////
                // For icecream cone shape diamond, need to use triangle mask
                /////////////////////////////////////////////////////////////

                if (diamond_group == DIAMOND_GROUPING.RBC_HighDepth)
                {
                    for (i = 0; i < hull.Count(); i++)
                    {
                        OpenCvSharp.Point p = hull.At<OpenCvSharp.Point>(i);
                        if (y_x_max >= y_x_min)
                        {
                            if (p.Y > y_x_min)
                            {
                                p.X = x_max;
                                p.Y = y_x_max;
                            }
                        }
                        else
                        {
                            if (p.Y > y_x_max)
                            {
                                p.X = x_min;
                                p.Y = y_x_min;
                            }
                        }
                    }
                }

                //////////////////////////////////////////////////////////////

                Cv2.FillConvexPoly(img_mask, hull, Scalar.White, LineTypes.AntiAlias, 0);

                //2013/11/3 Add erode function
                if (erode > 0)
                {
                    for (i = 0; i < erode; i++) Cv2.Erode(img_mask, img_mask, null);
                }

                //Calc length and area of img_mask -> use for fancy shape diamonds
                //Cv.FindContours(img_mask, storage1, out contours, CvContour.SizeOf, ContourRetrieval.External, ContourChain.ApproxSimple);
                //Cv.FIndCOntours overwrites img_mask, need to use copy image
                //IplImage img_mask_copy = Cv.Clone(img_mask);
                //Cv2.Copy(img_mask, img_mask_copy);
                Mat hierarchy = new Mat();
                Mat[] contours;
                img_mask.CopyTo(img_mask_copy);
                Cv2.FindContours(img_mask_copy, out contours, hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
                //Cv.ReleaseImage(img_mask_copy);
                
                mask_length = Cv2.ArcLength(contours[0], true);
                mask_area = Math.Abs(Cv2.ContourArea(contours[0]));
                //Cv.ClearSeq(contours);
            }
            else
            {
                mask_length = 0.0;
                mask_area = 0.0;
            }

            //Memory release
            //Cv.ReleaseImage(img_gray);
            //Cv.ReleaseImage(img_canny);
            //Cv.ReleaseImage(img_mask_copy);
            //Cv.ClearSeq(points);
            //Cv.ReleaseMemStorage(storage);
            //Cv.ReleaseMemStorage(storage1);

            //if the diamond is out of croped image, do not calc color values
            if (x_min == 0 | x_max == (img.Width - 1) | y_min == 0 | y_max == (img.Height - 1)) return dst;

            //img_mask.SaveImage(@"P:\Projects\DustDetection\TestSamples\gColorFancyImages\temp\image_mask_hiroshi.jpg");

            if (mask_length > 0)
            {
                dst = BitmapConverter.ToBitmap(img_mask);
            }

            return dst;
        }
    }

    public class NewMask : Mask
    {
        public NewMask(System.Drawing.Bitmap bmp) : base(bmp)
        {
            type = MASK_TYPE.MASK_NEW;
        }

        public NewMask(Mat src) : base(src)
        {
            type = MASK_TYPE.MASK_NEW;
        }

        public NewMask() : base()
        {
            type = MASK_TYPE.MASK_NEW;
        }

        public override bool Create(out Mat mask, out Mat img_mask_spc, int brightAreaThreshold = -1, int darkAreaThreshold = -1, bool displayWindows = false)
        {
            mask = null;
            Mat img_mask;
            img_mask_spc = null;
            double length, area, width, height, pvheight, area2;
            //int brightAreaThreshold = -1;
            //int darkAreaThreshold = -1;
            try
            {
                Bitmap bitmap = CreateObjectMaskNew(src.ToBitmap(), out img_mask,
                    out length, out area, out width, out height, out pvheight, kThreshold, hullThreshold,
                    cannyThreshold1, cannyThreshold2, out img_mask_spc, out area2, brightAreaThreshold, darkAreaThreshold);

                mask = displayWindows ? BitmapConverter.ToMat(bitmap) : img_mask;
                //mask = img_mask;
                SaveState(img_mask_spc, length, area, width, height, pvheight, area2);

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            return false;
        }

        private Bitmap CreateObjectMaskNew(Bitmap image, out Mat image_mask,
            out double mask_length, out double mask_area, out double mask_width, out double mask_height,
            out double mask_pvheight, double kThresh, double hThresh, double canny1, double canny2,
            out Mat image_mask_spc, out double mask2_area, int brightAreaThreshold = -1, int darkAreaThreshold = -1)
        {
            Bitmap dst = null;
            image_mask = null;
            image_mask_spc = null;
            mask_length = mask_area = mask_width = mask_height = mask_pvheight = mask2_area = 0;

            try
            {
                Mat src = BitmapConverter.ToMat(image);

                Mat src_kirsch = BitmapConverter.ToMat(image.KirschFilter());

                Mat kirsch_gray = new Mat();
                Cv2.CvtColor(src_kirsch, kirsch_gray, ColorConversionCodes.RGB2GRAY);

                Mat kirsch_threshold = new Mat();
                Cv2.Threshold(kirsch_gray, kirsch_threshold, kThresh, 255, ThresholdTypes.Binary);


                Mat[] contours;
                List<OpenCvSharp.Point> hierarchy;
                List<Mat> hulls;
                Mat morph_element = Cv2.GetStructuringElement(MorphShapes.Ellipse, new OpenCvSharp.Size(2, 2), new OpenCvSharp.Point(1, 1));

                #region morphology

                Mat kirsch_threshold_copy = new Mat();
                kirsch_threshold.CopyTo(kirsch_threshold_copy);

                int hullCount = 0, numLoops = 0;
                do
                {
                    numLoops++;

                    Mat kirsch_morph = kirsch_threshold_copy.MorphologyEx(MorphTypes.Gradient, morph_element);

                    hierarchy = new List<OpenCvSharp.Point>();
                    Cv2.FindContours(kirsch_morph, out contours, OutputArray.Create(hierarchy),
                        RetrievalModes.External, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));

                    hulls = new List<Mat>();
                    for (int j = 0; j < contours.Length; j++)
                    {
                        Mat hull = new Mat();
                        Cv2.ConvexHull(contours[j], hull);
                        hulls.Add(hull);
                    }

                    Mat drawing = Mat.Zeros(src.Size(), MatType.CV_8UC1);
                    Cv2.DrawContours(drawing, hulls, -1, Scalar.White);

                    if (hulls.Count != hullCount && numLoops < 100)
                    {
                        hullCount = hulls.Count;
                        kirsch_threshold_copy = drawing;
                    }
                    else
                    {
                        break;
                    }

                } while (true);

                #endregion

                if (numLoops >= 100)
                {
                    throw new Exception("Could not find hull");
                }

                #region bestHull
                //try and filter out dust near to stone

                double largestArea = hulls.Max(m => Cv2.ContourArea(m));
                var bestHulls = hulls.Where(m => Cv2.ContourArea(m) == largestArea).ToList();

                Mat hulls_mask = Mat.Zeros(src.Size(), MatType.CV_8UC1);
                Cv2.DrawContours(hulls_mask, bestHulls, -1, Scalar.White, -1);

                //hulls_mask is the convex hull of outline, now look for clefts
                Cv2.Threshold(kirsch_gray, kirsch_threshold, hThresh, 255, ThresholdTypes.Binary);
                Mat kirsch_mask = Mat.Zeros(kirsch_threshold.Size(), kirsch_threshold.Type());
                kirsch_threshold.CopyTo(kirsch_mask, hulls_mask);

                Mat kirsch_mask_canny = new Mat();
                Cv2.Canny(kirsch_mask, kirsch_mask_canny, canny1, canny2, 3);

                morph_element = Cv2.GetStructuringElement(MorphShapes.Ellipse, new OpenCvSharp.Size(5, 5), new OpenCvSharp.Point(2, 2));
                Mat kirsch_filled = new Mat();
                Cv2.Dilate(kirsch_mask_canny, kirsch_filled, morph_element);
                Cv2.Dilate(kirsch_filled, kirsch_filled, morph_element);
                Cv2.Erode(kirsch_filled, kirsch_filled, morph_element);
                Cv2.Erode(kirsch_filled, kirsch_filled, morph_element);

                hierarchy = new List<OpenCvSharp.Point>(); ;
                Cv2.FindContours(kirsch_filled, out contours, OutputArray.Create(hierarchy),
                        RetrievalModes.External, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));

                #endregion

                hulls_mask = Mat.Zeros(src.Size(), MatType.CV_8UC1);
                Cv2.DrawContours(hulls_mask, contours, -1, Scalar.White, -1);

                Cv2.Erode(hulls_mask, hulls_mask, morph_element);
                Cv2.Erode(hulls_mask, hulls_mask, morph_element);

                image_mask = hulls_mask;

                //remove bright areas
                if ((brightAreaThreshold > -1) || (darkAreaThreshold > -1))
                {
                    Mat src_mask = new Mat();
                    Mat hulls_mask_spc = hulls_mask.Clone();
                    src.CopyTo(src_mask, hulls_mask_spc);
                    Mat gray = new Mat();

                    Cv2.CvtColor(src_mask, gray, ColorConversionCodes.BGR2GRAY);
                    if (brightAreaThreshold > -1)
                    {
                        Mat bright = new Mat();
                        Cv2.Threshold(gray, bright, brightAreaThreshold, 255, ThresholdTypes.BinaryInv);
                        Cv2.ImWrite(@"C:\gColorFancy\Image\bright.jpg", bright);
                        Mat t = new Mat();
                        hulls_mask_spc.CopyTo(t, bright);
                        hulls_mask_spc = t.Clone();
                    }
                    if (darkAreaThreshold > -1)
                    {
                        Mat dark = new Mat();
                        Cv2.Threshold(gray, dark, darkAreaThreshold, 255, ThresholdTypes.Binary);
                        Cv2.ImWrite(@"C:\gColorFancy\Image\dark.jpg", dark);
                        Mat t = new Mat();
                        hulls_mask_spc.CopyTo(t, dark);
                        hulls_mask_spc = t.Clone();
                    }

                    image_mask_spc = hulls_mask_spc;

                    var hierarchy2 = new List<OpenCvSharp.Point>(); ;
                    Cv2.FindContours(hulls_mask_spc, out contours, OutputArray.Create(hierarchy2),
                            RetrievalModes.External, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));

                    largestArea = contours.Max(m => Cv2.ContourArea(m));
                    Mat finalHullSpc = contours.Where(m => Cv2.ContourArea(m) == largestArea).ToList()[0];

                    if (ConvexHullOnMask)
                    {
                        Mat hull = new Mat();
                        Cv2.ConvexHull(finalHullSpc, hull);
                        Mat polySpc = new Mat();
                        Cv2.ApproxPolyDP(hull, polySpc, 3, true);
                        mask2_area = Cv2.ContourArea(polySpc);
                    }
                    else
                        mask2_area = largestArea;
                }
                ///////////////////////////

                hierarchy = new List<OpenCvSharp.Point>(); ;
                Cv2.FindContours(hulls_mask, out contours, OutputArray.Create(hierarchy),
                        RetrievalModes.External, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));

                largestArea = contours.Max(m => Cv2.ContourArea(m));
                Mat finalHull = contours.Where(m => Cv2.ContourArea(m) == largestArea).ToList()[0];

                if (ConvexHullOnMask)
                {
                    var hull = new Mat();
                    Cv2.ConvexHull(finalHull, hull);
                    finalHull = hull;
                }
                List<Mat> finalHulls = new List<Mat>();
                finalHulls.Add(finalHull);
                Cv2.DrawContours(src, finalHulls, -1, new Scalar(128, 0, 128, 255), 3);

                #region bounding

                Mat poly = new Mat();
                Cv2.ApproxPolyDP(finalHull, poly, 3, true);
                Rect boundaryRect = Cv2.BoundingRect(poly);
                mask_width = boundaryRect.Width;
                mask_height = boundaryRect.Height;
                if (ConvexHullOnMask)
                    mask_area = Cv2.ContourArea(poly);
                else
                    mask_area = largestArea;
                mask_length = Cv2.ArcLength(finalHull, true);

                List<OpenCvSharp.Point> finalPoints = new List<OpenCvSharp.Point>();
                int m1Count = (finalHull.Rows % 2 > 0) ? finalHull.Rows + 1 : finalHull.Rows;
                OpenCvSharp.Point[] p1 = new OpenCvSharp.Point[m1Count];
                finalHull.GetArray(0, 0, p1);
                Array.Resize(ref p1, finalHull.Rows);
                finalPoints.AddRange(p1.ToList());

                double y_min = boundaryRect.Bottom;
                double y_x_min = finalPoints.Where(p => p.X == boundaryRect.Left).ToList()[0].Y;
                double y_x_max = finalPoints.Where(p => p.X == boundaryRect.Right).ToList()[0].Y;

                mask_pvheight = ((double)y_x_max + (double)y_x_min) / 2 - (double)y_min;

                #endregion

                //dst = BitmapConverter.ToBitmap(src);
                using (var ms = src.ToMemoryStream())
                {
                    dst = (Bitmap)Image.FromStream(ms);
                }

                try
                {
                    if (saveMaskDataPath.Length > 0)
                    {
                        //StringBuilder sb = new StringBuilder();
                        //sb.AppendLine("mask_length,mask_area,mask_width,mask_height,mask_pvheight");
                        //sb.AppendLine(mask_length + "," + mask_area + "," + mask_width + "," + mask_height + "," + mask_pvheight);
                        image_mask.SaveImage(saveMaskDataPath + @"\image_mask.jpg");
                        if (image_mask_spc != null)
                            image_mask_spc.SaveImage(saveMaskDataPath + @"\image_mask_spc.jpg");
                        BitmapConverter.ToMat(image).SaveImage(saveMaskDataPath + @"\src.jpg");
                        //File.WriteAllText(saveMaskDataPath + @"\mask_vals.csv", sb.ToString());
                        //File.AppendAllText(saveMaskDataPath + @"\exception.txt", DateTime.Now + ":" + av.Message);
                        //File.AppendAllText(saveMaskDataPath + @"\exception.txt", DateTime.Now + ":" + av.StackTrace);
                        //File.AppendAllText(saveMaskDataPath + @"\exception.txt", DateTime.Now + ":" + av.Source);
                    }
                }
                catch
                {

                }

            }
            catch(Exception ex)
            {
                dst = null;
            }

            return dst;
        }
    }

    public class MeleeMask : Mask
    {
        public MeleeMask(System.Drawing.Bitmap bmp) : base(bmp)
        {
            type = MASK_TYPE.MASK_MELEE;
        }

        public MeleeMask(Mat src) : base(src)
        {
            type = MASK_TYPE.MASK_MELEE;
        }

        public MeleeMask() : base()
        {
            type = MASK_TYPE.MASK_MELEE;
        }

        public override bool Create(out Mat mask, out Mat img_mask_spc, int brightAreaThreshold = -1, int darkAreaThreshold = -1, bool displayWindows = false)
        {
            mask = null;
            img_mask_spc = null;
            Mat img_mask;
            double length, area, width, height, pvheight;
            bool useKthresholdLab = false;
            try
            {
                Bitmap bitmap = CreateObjectMaskMelee(src.ToBitmap(), out img_mask,
                    out length, out area, out width, out height, out pvheight, useKthresholdLab);

                mask = BitmapConverter.ToMat(bitmap);
                SaveState(length, area, width, height, pvheight);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            return false;
        }

        private Bitmap CreateObjectMaskMelee(Bitmap image, out Mat image_mask,
            out double mask_length, out double mask_area, out double mask_width, out double mask_height,
            out double mask_pvheight, bool useKthresholdLab = false)
        {
            Bitmap dst = null;
            image_mask = null;
            mask_length = mask_area = mask_width = mask_height = mask_pvheight = 0;

            try
            {
                Mat src = BitmapConverter.ToMat(image);

                Mat src_kirsch = BitmapConverter.ToMat(image.KirschFilter());

                Mat kirsch_gray = new Mat();
                Cv2.CvtColor(src_kirsch, kirsch_gray, ColorConversionCodes.RGB2GRAY);

                Mat kirsch_threshold = new Mat();
                if (!useKthresholdLab)
                    Cv2.Threshold(kirsch_gray, kirsch_threshold, kThreshold, 255, ThresholdTypes.Binary);
                else
                    Cv2.Threshold(kirsch_gray, kirsch_threshold, kThresholdLab, 255, ThresholdTypes.Binary);

                Mat[] contours;
                List<OpenCvSharp.Point> hierarchy;
                List<Mat> hulls;
                Mat morph_element = Cv2.GetStructuringElement(MorphShapes.Ellipse, new OpenCvSharp.Size(2, 2), new OpenCvSharp.Point(1, 1));

                #region morphology

                Mat kirsch_threshold_copy = new Mat();
                kirsch_threshold.CopyTo(kirsch_threshold_copy);

                int hullCount = 0, numLoops = 0;
                do
                {
                    numLoops++;

                    Mat kirsch_morph = kirsch_threshold_copy.MorphologyEx(MorphTypes.Gradient, morph_element);

                    hierarchy = new List<OpenCvSharp.Point>();
                    Cv2.FindContours(kirsch_morph, out contours, OutputArray.Create(hierarchy),
                        RetrievalModes.External, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));

                    hulls = new List<Mat>();
                    for (int j = 0; j < contours.Length; j++)
                    {
                        Mat hull = new Mat();
                        Cv2.ConvexHull(contours[j], hull);
                        hulls.Add(hull);
                    }

                    Mat drawing = Mat.Zeros(src.Size(), MatType.CV_8UC1);
                    Cv2.DrawContours(drawing, hulls, -1, Scalar.White);

                    if (hulls.Count != hullCount && numLoops < 100)
                    {
                        hullCount = hulls.Count;
                        kirsch_threshold_copy = drawing;
                    }
                    else
                    {
                        break;
                    }

                } while (true);

                #endregion

                if (numLoops >= 100)
                {
                    throw new Exception("Could not find hull");
                }

                #region bestHull
                //try and filter out dust near to stone

                double largestArea = hulls.Max(m => Cv2.ContourArea(m));
                var bestHulls = hulls.Where(m => Cv2.ContourArea(m) == largestArea).ToList();

                Mat hulls_mask = Mat.Zeros(src.Size(), MatType.CV_8UC1);
                Cv2.DrawContours(hulls_mask, bestHulls, -1, Scalar.White, -1);

                //hulls_mask is the convex hull of main outline excluding nearby dust
                Cv2.Threshold(kirsch_gray, kirsch_threshold, hullThreshold, 255, ThresholdTypes.Binary);
                Mat kirsch_mask = Mat.Zeros(kirsch_threshold.Size(), kirsch_threshold.Type());
                kirsch_threshold.CopyTo(kirsch_mask, hulls_mask);

                #endregion

                hierarchy = new List<OpenCvSharp.Point>(); ;
                Cv2.FindContours(kirsch_mask, out contours, OutputArray.Create(hierarchy),
                        RetrievalModes.External, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));

                List<OpenCvSharp.Point> points = new List<OpenCvSharp.Point>();
                foreach (Mat contour in contours)
                {
                    int m2Count = (contour.Rows % 2 > 0) ? contour.Rows + 1 : contour.Rows;
                    OpenCvSharp.Point[] p2 = new OpenCvSharp.Point[m2Count];
                    contour.GetArray(0, 0, p2);
                    Array.Resize(ref p2, contour.Rows);

                    points.AddRange(p2.ToList());
                }
                Mat finalHull = new Mat();
                Cv2.ConvexHull(InputArray.Create(points), finalHull);


                List<Mat> finalHulls = new List<Mat>();
                finalHulls.Add(finalHull);
                Cv2.DrawContours(src, finalHulls, -1, new Scalar(128, 0, 128, 255), 2);

                hulls_mask = Mat.Zeros(src.Size(), MatType.CV_8UC1);
                Cv2.DrawContours(hulls_mask, finalHulls, -1, Scalar.White, -1);
                image_mask = hulls_mask;

                #region bounding

                Mat poly = new Mat();
                Cv2.ApproxPolyDP(finalHull, poly, 3, true);
                Rect boundaryRect = Cv2.BoundingRect(poly);
                mask_width = boundaryRect.Width;
                mask_height = boundaryRect.Height;
                mask_area = Cv2.ContourArea(poly);
                mask_length = Cv2.ArcLength(finalHull, true);

                List<OpenCvSharp.Point> finalPoints = new List<OpenCvSharp.Point>();
                int m1Count = (finalHull.Rows % 2 > 0) ? finalHull.Rows + 1 : finalHull.Rows;
                OpenCvSharp.Point[] p1 = new OpenCvSharp.Point[m1Count];
                finalHull.GetArray(0, 0, p1);
                Array.Resize(ref p1, finalHull.Rows);
                finalPoints.AddRange(p1.ToList());

                double y_min = boundaryRect.Bottom;
                double y_x_min = finalPoints.Where(p => p.X == boundaryRect.Left).ToList()[0].Y;
                double y_x_max = finalPoints.Where(p => p.X == boundaryRect.Right).ToList()[0].Y;

                mask_pvheight = ((double)y_x_max + (double)y_x_min) / 2 - (double)y_min;

                #endregion

                //dst = BitmapConverter.ToBitmap(src);
                using (var ms = src.ToMemoryStream())
                {
                    dst = (Bitmap)Image.FromStream(ms);
                }

                try
                {
                    if (saveMaskDataPath.Length > 0)
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine("mask_length,mask_area,mask_width,mask_height,mask_pvheight");
                        sb.AppendLine(mask_length + "," + mask_area + "," + mask_width + "," + mask_height + "," + mask_pvheight);
                        image_mask.SaveImage(saveMaskDataPath + @"\image_mask.jpg");
                        File.WriteAllText(saveMaskDataPath + @"\mask_vals.csv", sb.ToString());
                    }

                }
                catch
                {
                }
            }
            catch
            {
                dst = null;
            }

            return dst;
        }
    }
}
