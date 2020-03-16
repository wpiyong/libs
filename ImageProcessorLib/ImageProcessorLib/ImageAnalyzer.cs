using OpenCvSharp;
using OpenCvSharp.Extensions;
using PeakFinder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ImageProcessorLib
{
    public class N3Results
    {
        public double h;
        public double s;
        public double l;
        public double time;
        public List<double> delays;
        public string type;

        public N3Results(double h, double s, double l, double time, List<double> delays, string type)
        {
            this.h = h;
            this.s = s;
            this.l = l;
            this.time = time;
            this.delays = delays;
            this.type = type;
        }
    }

    public class ImageAnalyzer
    {
        protected Mat src;

        Mat src_mask;

        protected Mask mask;

        protected bool MultiThreading = true;

        public ImageAnalyzer()
        {

        }

        public ImageAnalyzer(System.Drawing.Bitmap bmp)
        {
            src = BitmapConverter.ToMat(bmp);
        }

        public ImageAnalyzer(Mat s, Mat mask)
        {
            if (mask != null)
            {
                s.CopyTo(src, mask);
                src_mask = mask.Clone();
            }
            else
            {
                src_mask = new Mat(s.Size(), MatType.CV_8UC1, new Scalar(255) );
                src = s.Clone();
            }
        }

        public void SetMultiThreading(bool multiThreading)
        {
            MultiThreading = multiThreading;
        }

        public virtual bool HasMultipleBrightRegions(out bool? result)
        {            
            try
            {
                result = false;
                Mat src_gray = new Mat();
                Cv2.CvtColor(src, src_gray, ColorConversionCodes.RGB2GRAY);
                
                Mat histogram = new Mat();
                int histSize = 231;
                int[] dimensions = { (int)histSize }; // Histogram size for each dimension
                Rangef[] ranges = { new Rangef(5, 236) }; // min/max

                Cv2.CalcHist(
                        images: new[] { src_gray },
                        channels: new[] { 0 }, //The channel (dim) to be measured. In this case it is just the intensity (each array is single-channel) so we just write 0.
                        mask: null,
                        hist: histogram,
                        dims: 1, //The histogram dimensionality.
                        histSize: dimensions,
                        ranges: ranges);

                var yData = new float[histogram.Rows * histogram.Cols];
                histogram.GetArray(0, 0, yData);
                var xData = Enumerable.Range(5, yData.Length);
                var points = xData.Zip(yData, (x, y) => new System.Windows.Point(x, y)).ToList();
                

                var spectrum = new Spectrum(points);
                List<Peak> peaks = new List<Peak>();
                while (true)
                {
                    spectrum = spectrum.SmoothedSpectrum(3, 1);
                    peaks = spectrum.FindPeaksByGradient(1).ToList();
                    //System.Diagnostics.Debug.WriteLine("Smoothing... " + peaks.Count);
                    if (peaks.Count(p => p.Height > 0 && p.Height < 5 && p.Width < 10) == 0)
                        break;
                }

                var heights = peaks.Select(p => p.Height).ToList();
                heights.Sort();
                heights.Reverse();
                heights = heights.Where(h => h > 10).Take(5).ToList();

                if (heights.Count > 0)
                {
                    var bigpeaks = peaks.Where(p => p.Height >= heights.Last()).ToList();
                    //foreach (var peak in bigpeaks)
                    //{
                    //    Debug.WriteLine(peak.Top.X + ":" + ", height = " + peak.Height +
                    //        ", start = " + peak.Start.X
                    //        + ", end = " + peak.End.X
                    //        + ", width = " + peak.Width);
                    //}

                    double threshold = yData.Sum() * 1.5 / yData.Count();
                    //Debug.WriteLine("threshold = " + threshold);

                    if (bigpeaks.Count(p => p.Top.Y > threshold) > 2)
                    {
                        result = true;//uneven
                    }
                }
                
                return true;
            }
            catch (Exception ex)
            {
                result = null;
                Log.Error(ex);
            }

            return false;
        }

        public virtual bool HasMultipleHueRegions(out bool? result)
        {
            
            try
            {
                result = false;
                Mat src_hue = new Mat();

                Mat src_hsv = new Mat();
                Cv2.CvtColor(src, src_hsv, ColorConversionCodes.BGR2HSV_FULL);
                Mat[] channels = new Mat[0];
                Cv2.Split(src_hsv, out channels);
                src_hue = channels[0].Clone();

                var huePixels = new byte[src_hue.Cols * src_hue.Rows];
                src_hue.GetArray(0, 0, huePixels);


                Mat histogram = new Mat();
                int histSize = 251;
                int[] dimensions = { (int)histSize }; // Histogram size for each dimension
                Rangef[] ranges = { new Rangef(5, 256) }; // min/max

                Cv2.CalcHist(
                        images: new[] { src_hue },
                        channels: new[] { 0 }, //The channel (dim) to be measured. In this case it is just the intensity (each array is single-channel) so we just write 0.
                        mask: null,
                        hist: histogram,
                        dims: 1, //The histogram dimensionality.
                        histSize: dimensions,
                        ranges: ranges);


                var yData = new float[histogram.Rows * histogram.Cols];
                histogram.GetArray(0, 0, yData);
                var xData = Enumerable.Range(5, yData.Length);
                var points = xData.Zip(yData, (x, y) => new System.Windows.Point(x, y)).ToList();
                
                var spectrum = new Spectrum(points);
                List<Peak> peaks = spectrum.FindPeaksByGradient(1).Where(p => p.Height > 0).ToList();
                var highestPeak = peaks.Where(pk => pk.Height == peaks.Max(p => p.Height)).First();

                var count = huePixels.Where(gp => gp >= highestPeak.Start.X && gp <= highestPeak.End.X).Count();
                var mask_area = Cv2.CountNonZero(src_mask);
                double percent = Math.Round(count * 100d / mask_area);
                
                //Debug.WriteLine(highestPeak.Top.X + ":" + percent + ", height = " + highestPeak.Height +
                //    ", start = " + highestPeak.Start.X + ", end = " + highestPeak.End.X);

                if (percent < 66)
                {
                    result = true;//uneven
                }

                return true;
            }
            catch(Exception ex)
            {
                result = null;
                Log.Error(ex);
            }

            return false;
        }

        public virtual bool Get_Description(ref List<Bitmap> imageList, ref List<Bitmap> imgList_background, ref List<Rectangle> maskRect,
            out List<List<double>> descriptions, out List<string> comments)
        {
            descriptions = new List<List< double >> ();
            comments = new List<string>() { "No implementation" };
            return false;
        }

        public virtual bool Get_Description(ref List<Bitmap> imageList, ref List<Bitmap> imgList_background, ref List<Rectangle> maskRect,
            out List<string> descriptions, out List<string> comments, out List<List<N3Results>> n3ResultsListList)
        {
            descriptions = new List<string>() { "No implementation" };
            comments = new List<string>() { "No implementation" };
            n3ResultsListList = null;
            return false;
        }

        public virtual bool Get_Description(ref List<Bitmap> imageList, ref List<Bitmap> imgList_background,
            int brightAreaThreshold, int darkAreaThreshold,
            out string description, out string comment)
        {
            description = "No implementation";
            comment = "No implementation";
            return false;
        }

        public virtual bool Get_Description(ref List<Bitmap> imageList, ref List<Bitmap> imgList_background,
            ref double L, ref double a, ref double b, ref double C, ref double H, ref string L_description,
            ref string C_description, ref string H_description, ref double mask_L,
            ref double mask_A, ref string comment1, ref string comment2, ref string comment3,
            ref double mask2_A, ref List<Tuple<double, double, double, double, double, double>> hsvList1,
            bool useKthresholdLab = false, double photochromaL = -1,
            int brightAreaThreshold = -1, int darkAreaThreshold = -1)
        {
            C_description = "No implementation";
            comment3 = "No implementation";
            return false;
        }

        public virtual bool Get_Description(ref List<Bitmap> imageList, ref List<Bitmap> imgList_background, ref Bitmap maskbmp,
            ref double L, ref double a, ref double b, ref double C, ref double H, ref string L_description,
            ref string C_description, ref string H_description, ref double mask_L,
            ref double mask_A, ref string comment1, ref string comment2, ref string comment3,
            ref double mask2_A, ref List<Tuple<double, double, double, double, double, double>> hsvList1,
            bool useKthresholdLab = false, double photochromaL = -1,
            int brightAreaThreshold = -1, int darkAreaThreshold = -1)
        {
            C_description = "No implementation";
            comment3 = "No implementation";
            return false;
        }

        public virtual Boolean check_diamond_centered(ref Bitmap img_Bmp_diamond, int x, int y, ref string comment, int maxDistance)
        {
            comment = "Not implemented";
            return false;
        }

        public virtual void check_pearl_max_lumi(ref Bitmap img_Bmp_diamond, ref string comment, out double maxValue, out double shift)
        {
            shift = 0;
            maxValue = 0;
            comment = "Not implemented";
        }

        public virtual void CalcPearlLusterWidthHeight(List<System.Drawing.Bitmap> imgList, ref double shiftValue, ref double maxValue, out double w, out double h)
        {
            w = 0;
            h = 0;
        }

        public virtual Boolean maskCreate(ref Bitmap img, out Bitmap img_mask, bool displalyMask = false)
        {
            Console.WriteLine("Not implemented");
            img_mask = null;
            return false;
        }

        public virtual void setLabAdjustment(double Conv_L, double Conv_a, double Conv_b, double Shift_L, double Shift_a, double Shift_b)
        {
            return;
        }

        public double calc_C(ref double a, ref double b)
        {
            return Math.Sqrt(a * a + b * b);
        }

        public double calc_H(ref double a, ref double b)
        {
            return Math.Atan2(b, a) * 180 / Math.PI;
        }
    }

    public class ImageAnalyzer_FancyColor : ImageAnalyzer
    {
        string _deviceName = "CV";

        DIAMOND_GROUPING _diamond_group = DIAMOND_GROUPING.RBC;

        // Diamond grouping parameter
        double _widthratio_RBC = 1.1;
        double _aspect_min_RBC = 0.58; //0.58;
        double _aspect_max_RBC = 0.76;

        double _widthratio_Fancy_L = 1.35;
        double _widthratio_Fancy_H = 1.70;

        double _aspect_min_FancyL = 0.44; //0.44;
        double _aspect_max_FancyL = 0.7; //0.7;

        double _aspect_min_FancyH = 0.34; //0.34;
        double _aspect_max_FancyH = 0.7; //0.7;

        double _aspect_min_FancyHH = 0.24; //0.24;
        double _aspect_max_FancyHH = 0.5; //0.5;

        // Calibration parameter
        public double _shift_L = 0.0, _shift_a = 0.0, _shift_b = 0.0;
        public double _conv_L = 1.0, _conv_a = 1.0, _conv_b = 1.0;

        double _maxPhotoChromicLDiff = 0.03;

        List<FancyColorAnalyzer> fancyColorAnalyzers = new List<FancyColorAnalyzer>();
        List<ManualResetEvent> doneEvents = new List<ManualResetEvent>();

        public ImageAnalyzer_FancyColor() : base()
        {
            mask = new NewMask(); 
        }

        public ImageAnalyzer_FancyColor(System.Drawing.Bitmap bmp) : base(bmp)
        {
            mask = new NewMask();
        }

        public ImageAnalyzer_FancyColor(Mat s, Mat mask) : base (s, mask)
        {
            this.mask = new NewMask();
        }

        public override bool Get_Description(ref List<Bitmap> imageList, ref List<Bitmap> imgList_background,
            int brightAreaThreshold, int darkAreaThreshold,
            out string description, out string comment)
        {
            description = "";
            comment = "";

            string L_description = "", H_description = "", comment1 = "", comment2 = "";
            List<Tuple<double, double, double, double, double, double>> hsvList = new List<Tuple<double, double, double, double, double, double>>();
            double L = 0, a = 0, b = 0, C = 0, H = 0;
            double mask_L = 0, mask_A = 0, mask2_A = 0;

            return GetColor_Description(ref imageList, ref imgList_background,
                ref L, ref a, ref b, ref C, ref H, ref L_description, ref description,
                ref H_description, ref mask_L, ref mask_A, ref comment1, ref comment2, ref comment,
                ref mask2_A, ref hsvList, false,
                _maxPhotoChromicLDiff, brightAreaThreshold, darkAreaThreshold);
        }

        public override bool Get_Description(ref List<Bitmap> imageList, ref List<Bitmap> imgList_background,
            ref double L, ref double a, ref double b, ref double C, ref double H, ref string L_description,
            ref string C_description, ref string H_description, ref double mask_L,
            ref double mask_A, ref string comment1, ref string comment2, ref string comment3,
            ref double mask2_A, ref List<Tuple<double, double, double, double, double, double>> hsvList1,
            bool useKthresholdLab = false, double photochromaL = -1,
            int brightAreaThreshold = -1, int darkAreaThreshold = -1)
        {
            return GetColor_Description(ref imageList, ref imgList_background,
                ref L, ref a, ref b, ref C, ref H, ref L_description, ref C_description,
                ref H_description, ref mask_L, ref mask_A, ref comment1, ref comment2, ref comment3,
                ref mask2_A, ref hsvList1, useKthresholdLab,
                _maxPhotoChromicLDiff, brightAreaThreshold, darkAreaThreshold);
        }

        public override bool Get_Description(ref List<Bitmap> imageList, ref List<Bitmap> imgList_background, ref Bitmap maskBmp,
            ref double L, ref double a, ref double b, ref double C, ref double H, ref string L_description,
            ref string C_description, ref string H_description, ref double mask_L,
            ref double mask_A, ref string comment1, ref string comment2, ref string comment3,
            ref double mask2_A, ref List<Tuple<double, double, double, double, double, double>> hsvList1,
            bool useKthresholdLab = false, double photochromaL = -1,
            int brightAreaThreshold = -1, int darkAreaThreshold = -1)
        {
            return GetColor_Description(ref imageList, ref imgList_background, ref maskBmp,
                ref L, ref a, ref b, ref C, ref H, ref L_description, ref C_description,
                ref H_description, ref mask_L, ref mask_A, ref comment1, ref comment2, ref comment3,
                ref mask2_A, ref hsvList1, useKthresholdLab,
                _maxPhotoChromicLDiff, brightAreaThreshold, darkAreaThreshold);
        }

        private bool GetColor_Description(ref List<Bitmap> imageList, ref List<Bitmap> imgList_background, 
            ref double L, ref double a, ref double b, ref double C, ref double H, 
            ref string L_description, ref string C_description, ref string H_description, ref double mask_L,
            ref double mask_A, ref string comment1, ref string comment2, ref string comment3,
            ref double mask2_A, ref List<Tuple<double, double, double, double, double, double>> hsvList1,
            bool useKthresholdLab = false, double photochromaL = -1,
            int brightAreaThreshold = -1, int darkAreaThreshold = -1)
        {
            bool result = true;

            try
            {
                Dictionary<string, string> result_RBC;
                Dictionary<string, string> result_Fancy;
                //double shift_C;
                string boundary_version = "";
                string refer_stone = "";

                //RBC
                List<double> list_L_diamond = new List<double>();
                List<double> list_a_diamond = new List<double>();
                List<double> list_b_diamond = new List<double>();
                List<double> list_L_background = new List<double>();
                List<double> list_a_background = new List<double>();
                List<double> list_b_background = new List<double>();
                List<double> list_L = new List<double>();
                List<double> list_a = new List<double>();
                List<double> list_b = new List<double>();
                List<double> list_C = new List<double>();
                List<double> list_H = new List<double>();
                List<double> list_masklength = new List<double>();
                List<double> list_maskarea = new List<double>();
                List<double> list_mask2area = new List<double>();
                List<double> list_maskwidth = new List<double>();
                List<double> list_maskheight = new List<double>();
                List<double> list_maskpvheight = new List<double>();
                List<double> list_maskaspectratio = new List<double>();

                double avg_L = 0, avg_a = 0, avg_b = 0, avg_C = 0, avg_H = 0;
                double avg_L_diamond = 0, avg_a_diamond = 0, avg_b_diamond = 0;
                double avg_L_background = 0, avg_a_background = 0, avg_b_background = 0;
                double avg_masklength = 0, avg_maskarea = 0;
                double avg_maskheight = 0;
                double avg_maskpvheight = 0;
                double maxmin_widthratio = 0, max_aspectratio = 0, min_aspectratio = 0, avg_aspectratio = 0;
                //string comment_RBC = "";

                //FANCY
                List<double> list_L_diamond_Fancy = new List<double>();
                List<double> list_a_diamond_Fancy = new List<double>();
                List<double> list_b_diamond_Fancy = new List<double>();
                List<double> list_L_background_Fancy = new List<double>();
                List<double> list_a_background_Fancy = new List<double>();
                List<double> list_b_background_Fancy = new List<double>();
                List<double> list_L_Fancy = new List<double>();
                List<double> list_a_Fancy = new List<double>();
                List<double> list_b_Fancy = new List<double>();
                List<double> list_C_Fancy = new List<double>();
                List<double> list_H_Fancy = new List<double>();
                List<double> list_masklength_Fancy = new List<double>();
                List<double> list_maskarea_Fancy = new List<double>();
                List<double> list_mask2area_Fancy = new List<double>();
                List<double> list_maskwidth_Fancy = new List<double>();
                List<double> list_maskheight_Fancy = new List<double>();
                List<double> list_maskpvheigth_Fancy = new List<double>();
                List<double> list_maskaspectratio_Fancy = new List<double>();

                double avg_L_Fancy = 0, avg_a_Fancy = 0, avg_b_Fancy = 0, avg_C_Fancy = 0, avg_H_Fancy = 0;

                double avg_L_diamond_Fancy = 0, avg_a_diamond_Fancy = 0, avg_b_diamond_Fancy = 0;
                double avg_L_background_Fancy = 0, avg_a_background_Fancy = 0, avg_b_background_Fancy = 0;
                double avg_masklength_Fancy = 0, avg_maskarea_Fancy = 0;
                double maxmin_widthratio_Fancy = 0, max_aspectratio_Fancy = 0, min_aspectratio_Fancy = 0, avg_aspectratio_Fancy = 0;
                string L_description_Fancy = "", C_description_Fancy = "", H_description_Fancy = "";
                //string comment_Fancy = "";
                double lFirst = 0, lLast = 0;

                double volume = 0.0; //This is used for the calc of mask area: Fancy shape diamond and special RBC

                int i, j;

                // For icecream cone shape
                _diamond_group = DIAMOND_GROUPING.Default;
                comment3 = "FINALIZED";

                i = 0;

                if (MultiThreading)
                {
                    if (hsvList1 != null && hsvList1.Count == 0)
                    {
                        hsvList1.Add(new Tuple<double, double, double, double, double, double>(0, 0, 0, 0, 0, 0));
                    }
                    fancyColorAnalyzers.Clear();
                    doneEvents.Clear();
                    // todo: multi threading
                    for (int m = 0; m < imageList.Count; m++)
                    {
                        // todo: thread pool for calcLab_diamond_background_all(..)
                        Bitmap image = imageList[m];
                        Bitmap image_background = imgList_background[m];

                        i++;
                        bool calculateClusters = false;
                        if (i >= imgList_background.Count)
                        {
                            i = 0;
                            if (hsvList1 != null)
                                calculateClusters = true;
                        }

                        ManualResetEvent resetEvent = new ManualResetEvent(false);
                        FancyColorAnalyzer analyzer = new FancyColorAnalyzer(this, resetEvent, ref image, ref image_background, useKthresholdLab, ref hsvList1, false,
                            brightAreaThreshold, darkAreaThreshold, calculateClusters);
                        doneEvents.Add(resetEvent);
                        fancyColorAnalyzers.Add(analyzer);
                        ThreadPool.QueueUserWorkItem(analyzer.ThreadPoolCallback, m);
                    }
                    Console.WriteLine("Analyzing ...");
                    WaitHandle.WaitAll(doneEvents.ToArray());
                    Console.WriteLine("All calculations are complete.");
                    for (int m = 0; m < fancyColorAnalyzers.Count; m++)
                    {
                        FancyColorAnalyzer analyzer = fancyColorAnalyzers[m];
                        list_L.Add(analyzer.L);
                        list_a.Add(analyzer.a);
                        list_b.Add(analyzer.b);
                        list_C.Add(calc_C(ref analyzer.a, ref analyzer.b));
                        list_H.Add(calc_H(ref analyzer.a, ref analyzer.b));
                        list_L_diamond.Add(analyzer.L_diamond);
                        list_a_diamond.Add(analyzer.a_diamond);
                        list_b_diamond.Add(analyzer.b_diamond);
                        list_L_background.Add(analyzer.L_background);
                        list_a_background.Add(analyzer.a_background);
                        list_b_background.Add(analyzer.b_background);
                        list_masklength.Add(analyzer.mask_length);
                        list_maskarea.Add(analyzer.mask_area);
                        list_mask2area.Add(analyzer.mask2_area);
                        list_maskwidth.Add(analyzer.mask_width);
                        list_maskheight.Add(analyzer.mask_height);
                        list_maskpvheight.Add(analyzer.mask_pvheight);
                        list_maskaspectratio.Add(analyzer.mask_height / analyzer.mask_width);
                    }
                }
                else
                {
                    i = 0;
                    foreach (Bitmap bm in imageList)
                    {
                        double LL = 0, aa = 0, bb = 0;
                        double LL_diamond = 0, aa_diamond = 0, bb_diamond = 0;
                        double LL_background = 0, aa_background = 0, bb_background = 0;
                        double mask_length = 0, mask_area = 0, mask_width = 0, mask_height = 0, mask_pvheight = 0, mask2_area = 0;
                        Bitmap image = bm;
                        Bitmap image_background = imgList_background[i];

                        i++;
                        bool calculateClusters = false;
                        if (i >= imgList_background.Count)
                        {
                            i = 0;
                            if (hsvList1 != null)
                                calculateClusters = true;
                        }

                        //_erode = 1;

                        if (calcLab_diamond_background_all(ref image, ref image_background, ref LL_diamond,
                            ref aa_diamond, ref bb_diamond, ref LL_background, ref aa_background, ref bb_background,
                            ref LL, ref aa, ref bb, ref mask_length, ref mask_area, ref mask_width,
                            ref mask_height, ref mask_pvheight, useKthresholdLab, ref hsvList1, ref mask2_area, false,
                                brightAreaThreshold, darkAreaThreshold, calculateClusters) == true)
                        {
                            list_L.Add(LL);
                            list_a.Add(aa);
                            list_b.Add(bb);
                            list_C.Add(calc_C(ref aa, ref bb));
                            list_H.Add(calc_H(ref aa, ref bb));
                            list_L_diamond.Add(LL_diamond);
                            list_a_diamond.Add(aa_diamond);
                            list_b_diamond.Add(bb_diamond);
                            list_L_background.Add(LL_background);
                            list_a_background.Add(aa_background);
                            list_b_background.Add(bb_background);
                            list_masklength.Add(mask_length);
                            list_maskarea.Add(mask_area);
                            list_mask2area.Add(mask2_area);
                            list_maskwidth.Add(mask_width);
                            list_maskheight.Add(mask_height);
                            list_maskpvheight.Add(mask_pvheight);
                            list_maskaspectratio.Add(mask_height / mask_width);
                        }
                    }
                }

                avg_L = list_L.Average();
                avg_a = list_a.Average();
                avg_b = list_b.Average();
                avg_L_diamond = list_L_diamond.Average();
                avg_a_diamond = list_a_diamond.Average();
                avg_b_diamond = list_b_diamond.Average();
                avg_L_background = list_L_background.Average();
                avg_a_background = list_a_background.Average();
                avg_b_background = list_b_background.Average();
                avg_masklength = list_masklength.Average();
                avg_maskarea = list_maskarea.Average();
                avg_C = calc_C(ref avg_a, ref avg_b);
                avg_H = calc_H(ref avg_a, ref avg_b);
                maxmin_widthratio = list_maskwidth.Max() / list_maskwidth.Min();
                max_aspectratio = list_maskaspectratio.Max();
                min_aspectratio = list_maskaspectratio.Min();
                avg_aspectratio = list_maskaspectratio.Average();

                avg_maskheight = list_maskheight.Average();
                avg_maskpvheight = list_maskpvheight.Average();

                L = avg_L;
                a = avg_a;
                b = avg_b;
                C = avg_C;
                H = avg_H;
                mask_L = avg_masklength;
                mask_A = avg_maskarea;
                mask2_A = list_mask2area.Average();

                lFirst = list_L[0];
                lLast = list_L[list_L.Count - 1];

                //Grouping the diamond
                String diamond_proportion = "";

                //if (maxmin_widthratio < _widthratio_RBC && min_aspectratio / max_aspectratio >= _aspect_min_max_RBC)
                if (maxmin_widthratio <= _widthratio_RBC)
                {
                    //RBC
                    //shift_C = _shiftC_RBC;

                    if (avg_aspectratio >= _aspect_min_RBC && avg_aspectratio <= _aspect_max_RBC)
                    {
                        diamond_proportion = "RBC: Normal ";
                        _diamond_group = DIAMOND_GROUPING.RBC;
                        volume = 1.0;
                    }
                    else if (avg_aspectratio > _aspect_max_RBC)
                    {
                        // Deep pavilion, High crown, Thick girdle
                        diamond_proportion = "RBC: High depth ";
                        _diamond_group = DIAMOND_GROUPING.RBC_HighDepth;
                        volume = 0.5; //For triangle mask
                    }
                    else
                    {
                        // Shallow pavilion
                        diamond_proportion = "RBC: Low depth ";
                        _diamond_group = DIAMOND_GROUPING.RBC_LowDepth;
                        volume = 1.0;
                    }
                }
                else if (maxmin_widthratio <= _widthratio_Fancy_L)
                {
                    //Fancy_L: HT, CU
                    //shift_C = _shiftC_FancyL;

                    if (min_aspectratio >= _aspect_min_FancyL && min_aspectratio <= _aspect_max_FancyL)
                    {
                        diamond_proportion = "FANCY_L: Normal ";
                        _diamond_group = DIAMOND_GROUPING.FANCY_L;
                        volume = 1.0;
                    }
                    else if (min_aspectratio < _aspect_min_FancyL)
                    {
                        diamond_proportion = "FANCY_L: Low depth ";
                        _diamond_group = DIAMOND_GROUPING.FANCY_L_LowDepth;
                        volume = 1.0;
                    }
                    else
                    {
                        diamond_proportion = "FANCY_L: High depth ";
                        _diamond_group = DIAMOND_GROUPING.FANCY_L_HighDepth;
                        volume = 1.0;
                    }
                }
                else if (maxmin_widthratio <= _widthratio_Fancy_H)
                {
                    //Fancy_H: REC, SQ, OV, PR
                    //shift_C = _shiftC_FancyH;

                    if (min_aspectratio >= _aspect_min_FancyH && min_aspectratio <= _aspect_max_FancyH)
                    {
                        diamond_proportion = "FANCY_H: Normal ";
                        _diamond_group = DIAMOND_GROUPING.FNACY_H;
                        volume = 1.0;
                    }
                    else if (min_aspectratio < _aspect_min_FancyH)
                    {
                        diamond_proportion = "FANCY_H: Low depth ";
                        _diamond_group = DIAMOND_GROUPING.FANCY_H_LowDepth;
                        volume = 1.0;
                    }
                    else
                    {
                        diamond_proportion = "FANCY_H: High depth ";
                        _diamond_group = DIAMOND_GROUPING.FANCY_H_HighDepth;
                        volume = 1.0;
                    }
                }
                else
                {
                    //Fancy_HH: mosly MQ
                    //shift_C = _shiftC_FancyHH;

                    if (min_aspectratio >= _aspect_min_FancyHH && min_aspectratio <= _aspect_max_FancyHH)
                    {
                        diamond_proportion = "FANCY_HH: Normal ";
                        _diamond_group = DIAMOND_GROUPING.FANCY_HH;
                        volume = 1.0;
                    }
                    else if (min_aspectratio < _aspect_min_FancyH)
                    {
                        diamond_proportion = "FANCY_HH: Low depth ";
                        _diamond_group = DIAMOND_GROUPING.FANCY_HH_LowDepth;
                        volume = 1.0;
                    }
                    else
                    {
                        diamond_proportion = "FANCY_HH: High depth ";
                        _diamond_group = DIAMOND_GROUPING.FANCY_HH_HighDepth;
                        volume = 1.0;
                    }
                }

                //debug

                //result_RBC = Boundary.GetGrade_shifting(H, C, L, shift_C);
                Boundary boundary = new Boundary();
                result_RBC = boundary.GetGrade(H, C, L, (int)_diamond_group);
                L_description = result_RBC["L_description"];
                C_description = result_RBC["C_description"];
                H_description = result_RBC["H_description"];
                boundary_version = result_RBC["Version"];
                refer_stone = result_RBC["Refer"];

                //getColorGrade_description(L, C, H, ref L_description, ref C_description, ref H_description, ref comment_RBC);

                comment1 = avg_L_diamond.ToString() + ", " + avg_a_diamond.ToString() + ", " + avg_b_diamond.ToString() + ", "
                    + avg_L_background.ToString() + ", " + avg_a_background.ToString() + ", " + avg_b_background.ToString() + ", "
                    + avg_L.ToString() + ", " + avg_a.ToString() + ", " + avg_b.ToString() + ", "
                    + avg_C.ToString() + ", " + avg_H.ToString() + ", "
                    + L_description + ", " + C_description + ", " + H_description + ", " + boundary_version + ", "
                    + avg_masklength.ToString() + ", " + avg_maskarea.ToString();

                comment1 = _deviceName + ", 1.0, " + comment1 + ", " + avg_maskheight.ToString() +
                                ", " + avg_maskpvheight.ToString() + ", " + maxmin_widthratio.ToString() + ", " +
                                min_aspectratio.ToString() + ", " + diamond_proportion;

                //if (_diamond_group == DIAMOND_GROUPING.RBC_LowDepth || _diamond_group == DIAMOND_GROUPING.RBC_HighDepth ||
                //    _diamond_group == DIAMOND_GROUPING.FANCY_L_LowDepth || _diamond_group == DIAMOND_GROUPING.FANCY_L_HighDepth ||
                //    _diamond_group == DIAMOND_GROUPING.FANCY_H_LowDepth || _diamond_group == DIAMOND_GROUPING.FANCY_H_HighDepth ||
                //    _diamond_group == DIAMOND_GROUPING.FANCY_HH_LowDepth || _diamond_group == DIAMOND_GROUPING.FANCY_HH_HighDepth)
                if (_diamond_group == DIAMOND_GROUPING.RBC_HighDepth)
                {
                    comment3 = "GO TO VISUAL - DEPTH";
                }
                else
                {
                    if (C_description == "N/A")
                    {
                        comment3 = "GO TO VISUAL";
                    }
                    else
                    {
                        if (refer_stone != "FALSE")
                        {
                            comment3 = "GO TO VISUAL - ANALYZE";
                        }

                    }
                }
                ////////////////////////////////////////////////////////////
                // If the diamond is high depth RBC (ice cream cone shape)
                // calcurate LCH with triangle mask
                ////////////////////////////////////////////////////////////

                if (_diamond_group == DIAMOND_GROUPING.RBC_HighDepth)
                {
                    i = 0;
                    j = 0;
                    if (hsvList1 != null)
                        hsvList1 = new List<Tuple<double, double, double, double, double, double>>();

                    foreach (Bitmap bm in imageList)
                    {
                        double LL = 0, aa = 0, bb = 0;
                        double LL_diamond = 0, aa_diamond = 0, bb_diamond = 0;
                        double LL_background = 0, aa_background = 0, bb_background = 0;
                        double mask_length = 0, mask_area = 0, mask_width = 0, mask_height = 0, mask_pvheight = 0, mask2_area = 0;
                        Bitmap image = bm;
                        Bitmap image_background = imgList_background[i];

                        i++;
                        bool calculateClusters = false;
                        if (i >= imgList_background.Count)
                        {
                            i = 0;
                            if (hsvList1 != null)
                                calculateClusters = true;
                        }

                        ////////////////////////////////////////////////
                        // Need to calc appropriate erode for each image
                        ////////////////////////////////////////////////

                        //double _a = list_maskheight[j];
                        //double _b = list_maskwidth[j];
                        //double temp = (_a + _b) / 4.0 - 1 / 2.0 * Math.Sqrt(((_a + _b) / 2) * ((_a + _b) / 2) - _a * _b * (1 - volume));
                        //temp = temp / 1.6;

                        //if (volume < 1.0) _erode = (int)temp;
                        //else _erode = 1;

                        ////////////////////////////////////////////////
                        ////////////////////////////////////////////////
                        //_erode = 1;

                        if (calcLab_diamond_background_all(ref image, ref image_background, ref LL_diamond,
                            ref aa_diamond, ref bb_diamond, ref LL_background, ref aa_background,
                            ref bb_background, ref LL, ref aa, ref bb, ref mask_length, ref mask_area,
                            ref mask_width, ref mask_height, ref mask_pvheight, useKthresholdLab, ref hsvList1, ref mask2_area, false,
                                brightAreaThreshold, darkAreaThreshold, calculateClusters) == true)
                        {
                            list_L_Fancy.Add(LL);
                            list_a_Fancy.Add(aa);
                            list_b_Fancy.Add(bb);
                            list_C_Fancy.Add(calc_C(ref aa, ref bb));
                            list_H_Fancy.Add(calc_H(ref aa, ref bb));
                            list_L_diamond_Fancy.Add(LL_diamond);
                            list_a_diamond_Fancy.Add(aa_diamond);
                            list_b_diamond_Fancy.Add(bb_diamond);
                            list_L_background_Fancy.Add(LL_background);
                            list_a_background_Fancy.Add(aa_background);
                            list_b_background_Fancy.Add(bb_background);
                            list_masklength_Fancy.Add(mask_length);
                            list_maskarea_Fancy.Add(mask_area);
                            list_mask2area_Fancy.Add(mask2_area);
                            list_maskwidth_Fancy.Add(mask_width);
                            list_maskheight_Fancy.Add(mask_height);
                            list_maskpvheigth_Fancy.Add(mask_pvheight);
                            list_maskaspectratio_Fancy.Add(mask_height / mask_width);
                        }
                        j++;
                    }

                    avg_L_Fancy = list_L_Fancy.Average();
                    avg_a_Fancy = list_a_Fancy.Average();
                    avg_b_Fancy = list_b_Fancy.Average();
                    avg_L_diamond_Fancy = list_L_diamond_Fancy.Average();
                    avg_a_diamond_Fancy = list_a_diamond_Fancy.Average();
                    avg_b_diamond_Fancy = list_b_diamond_Fancy.Average();
                    avg_L_background_Fancy = list_L_background_Fancy.Average();
                    avg_a_background_Fancy = list_a_background_Fancy.Average();
                    avg_b_background_Fancy = list_b_background_Fancy.Average();
                    avg_masklength_Fancy = list_masklength_Fancy.Average();
                    avg_maskarea_Fancy = list_maskarea_Fancy.Average();
                    avg_C_Fancy = calc_C(ref avg_a_Fancy, ref avg_b_Fancy);
                    avg_H_Fancy = calc_H(ref avg_a_Fancy, ref avg_b_Fancy);
                    maxmin_widthratio_Fancy = list_maskwidth_Fancy.Max() / list_maskwidth_Fancy.Min();
                    max_aspectratio_Fancy = list_maskaspectratio_Fancy.Max();
                    min_aspectratio_Fancy = list_maskaspectratio_Fancy.Min();
                    avg_aspectratio_Fancy = list_maskaspectratio_Fancy.Average();
                    mask2_A = list_mask2area_Fancy.Average();

                    //result_Fancy = Boundary.GetGrade_shifting(H, C, L, shift_C);
                    result_Fancy = boundary.GetGrade(avg_H_Fancy, avg_C_Fancy, avg_L_Fancy, (int)_diamond_group);
                    L_description_Fancy = result_Fancy["L_description"];
                    C_description_Fancy = result_Fancy["C_description"];
                    H_description_Fancy = result_Fancy["H_description"];
                    boundary_version = result_Fancy["Version"];
                    refer_stone = result_Fancy["Refer"];

                    //getColorGrade_description(avg_L_Fancy, avg_C_Fancy, avg_H_Fancy, ref L_description_Fancy, ref C_description_Fancy, ref H_description_Fancy, ref comment_Fancy);

                    comment2 = avg_L_diamond_Fancy.ToString() + ", " + avg_a_diamond_Fancy.ToString() + ", " + avg_b_diamond_Fancy.ToString() + ", "
                                   + avg_L_background_Fancy.ToString() + ", " + avg_a_background_Fancy.ToString() + ", " + avg_b_background_Fancy.ToString() + ", "
                                   + avg_L_Fancy.ToString() + ", " + avg_a_Fancy.ToString() + ", " + avg_b_Fancy.ToString() + ", "
                                   + avg_C_Fancy.ToString() + ", " + avg_H_Fancy.ToString() + ", "
                                   + L_description_Fancy + ", " + C_description_Fancy + ", " + H_description_Fancy + ", " + boundary_version + ", "
                                   + avg_masklength.ToString() + ", " + avg_maskarea.ToString();

                    comment2 = _deviceName + ", " + volume.ToString() + ", " + comment2 + ", " + avg_maskheight.ToString() +
                                ", " + avg_maskpvheight.ToString() + ", " + maxmin_widthratio.ToString() + ", " +
                                max_aspectratio.ToString() + ", " + diamond_proportion;

                    comment1 = comment2;
                    comment2 = "";
                    comment3 = "GO TO VISUAL - DEPTH";

                    L = avg_L_Fancy;
                    C = avg_C_Fancy;
                    H = avg_H_Fancy;
                    L_description = L_description_Fancy;
                    C_description = C_description_Fancy;
                    H_description = H_description_Fancy;

                    lFirst = list_L_Fancy[0];
                    lLast = list_L_Fancy[list_L_Fancy.Count - 1];
                }

                //Compare image count and mask count 
                if (list_C.Count < imageList.Count)
                {
                    comment3 = "Measure again: failed image ="
                     + list_C.Count.ToString() + "/" + imageList.Count.ToString();

                    return false;
                }

                if (Math.Abs(imageList.Count - imgList_background.Count) > 3)
                {
                    comment3 = "Measure again: diamond/bg =" + imageList.Count.ToString() +
                    "/" + imgList_background.Count.ToString();

                    return false;
                }

                if (photochromaL >= 0)
                {
                    double lDiff = Math.Abs(lLast - lFirst);
                    if (lDiff > photochromaL)
                        comment3 = "Color change[" + lDiff + "]";
                    else
                        comment3 = "";
                }

            }
            catch (Exception ex)
            {
                result = false;
                comment3 = "Measure again: process failure";
                Log.Error(ex);
            }

            return result;
        }

        private bool GetColor_Description(ref List<Bitmap> imageList, ref List<Bitmap> imgList_background, ref Bitmap maskBmp,
            ref double L, ref double a, ref double b, ref double C, ref double H,
            ref string L_description, ref string C_description, ref string H_description, ref double mask_L,
            ref double mask_A, ref string comment1, ref string comment2, ref string comment3,
            ref double mask2_A, ref List<Tuple<double, double, double, double, double, double>> hsvList1,
            bool useKthresholdLab = false, double photochromaL = -1,
            int brightAreaThreshold = -1, int darkAreaThreshold = -1)
        {
            bool result = true;

            try
            {
                Dictionary<string, string> result_RBC;
                Dictionary<string, string> result_Fancy;
                //double shift_C;
                string boundary_version = "";
                string refer_stone = "";

                //RBC
                List<double> list_L_diamond = new List<double>();
                List<double> list_a_diamond = new List<double>();
                List<double> list_b_diamond = new List<double>();
                List<double> list_L_background = new List<double>();
                List<double> list_a_background = new List<double>();
                List<double> list_b_background = new List<double>();
                List<double> list_L = new List<double>();
                List<double> list_a = new List<double>();
                List<double> list_b = new List<double>();
                List<double> list_C = new List<double>();
                List<double> list_H = new List<double>();
                List<double> list_masklength = new List<double>();
                List<double> list_maskarea = new List<double>();
                List<double> list_mask2area = new List<double>();
                List<double> list_maskwidth = new List<double>();
                List<double> list_maskheight = new List<double>();
                List<double> list_maskpvheight = new List<double>();
                List<double> list_maskaspectratio = new List<double>();

                double avg_L = 0, avg_a = 0, avg_b = 0, avg_C = 0, avg_H = 0;
                double avg_L_diamond = 0, avg_a_diamond = 0, avg_b_diamond = 0;
                double avg_L_background = 0, avg_a_background = 0, avg_b_background = 0;
                double avg_masklength = 0, avg_maskarea = 0;
                double avg_maskheight = 0;
                double avg_maskpvheight = 0;
                double maxmin_widthratio = 0, max_aspectratio = 0, min_aspectratio = 0, avg_aspectratio = 0;
                //string comment_RBC = "";

                //FANCY
                List<double> list_L_diamond_Fancy = new List<double>();
                List<double> list_a_diamond_Fancy = new List<double>();
                List<double> list_b_diamond_Fancy = new List<double>();
                List<double> list_L_background_Fancy = new List<double>();
                List<double> list_a_background_Fancy = new List<double>();
                List<double> list_b_background_Fancy = new List<double>();
                List<double> list_L_Fancy = new List<double>();
                List<double> list_a_Fancy = new List<double>();
                List<double> list_b_Fancy = new List<double>();
                List<double> list_C_Fancy = new List<double>();
                List<double> list_H_Fancy = new List<double>();
                List<double> list_masklength_Fancy = new List<double>();
                List<double> list_maskarea_Fancy = new List<double>();
                List<double> list_mask2area_Fancy = new List<double>();
                List<double> list_maskwidth_Fancy = new List<double>();
                List<double> list_maskheight_Fancy = new List<double>();
                List<double> list_maskpvheigth_Fancy = new List<double>();
                List<double> list_maskaspectratio_Fancy = new List<double>();

                double avg_L_Fancy = 0, avg_a_Fancy = 0, avg_b_Fancy = 0, avg_C_Fancy = 0, avg_H_Fancy = 0;

                double avg_L_diamond_Fancy = 0, avg_a_diamond_Fancy = 0, avg_b_diamond_Fancy = 0;
                double avg_L_background_Fancy = 0, avg_a_background_Fancy = 0, avg_b_background_Fancy = 0;
                double avg_masklength_Fancy = 0, avg_maskarea_Fancy = 0;
                double maxmin_widthratio_Fancy = 0, max_aspectratio_Fancy = 0, min_aspectratio_Fancy = 0, avg_aspectratio_Fancy = 0;
                string L_description_Fancy = "", C_description_Fancy = "", H_description_Fancy = "";
                //string comment_Fancy = "";
                double lFirst = 0, lLast = 0;

                double volume = 0.0; //This is used for the calc of mask area: Fancy shape diamond and special RBC

                int i, j;

                // For icecream cone shape
                _diamond_group = DIAMOND_GROUPING.Default;
                comment3 = "FINALIZED";

                i = 0;
                foreach (Bitmap bm in imageList)
                {
                    double LL = 0, aa = 0, bb = 0;
                    double LL_diamond = 0, aa_diamond = 0, bb_diamond = 0;
                    double LL_background = 0, aa_background = 0, bb_background = 0;
                    double mask_length = 0, mask_area = 0, mask_width = 0, mask_height = 0, mask_pvheight = 0, mask2_area = 0;
                    Bitmap image = bm;
                    Bitmap image_background = imgList_background[i];

                    i++;
                    bool calculateClusters = false;
                    if (i >= imgList_background.Count)
                    {
                        i = 0;
                        if (hsvList1 != null)
                            calculateClusters = true;
                    }

                    //_erode = 1;

                    if (calcLab_diamond_background_all(ref image, ref image_background, ref maskBmp, ref LL_diamond,
                        ref aa_diamond, ref bb_diamond, ref LL_background, ref aa_background, ref bb_background,
                        ref LL, ref aa, ref bb, ref mask_length, ref mask_area, ref mask_width,
                        ref mask_height, ref mask_pvheight, useKthresholdLab, ref hsvList1, ref mask2_area, false,
                            brightAreaThreshold, darkAreaThreshold, calculateClusters) == true)
                    {
                        list_L.Add(LL);
                        list_a.Add(aa);
                        list_b.Add(bb);
                        list_C.Add(calc_C(ref aa, ref bb));
                        list_H.Add(calc_H(ref aa, ref bb));
                        list_L_diamond.Add(LL_diamond);
                        list_a_diamond.Add(aa_diamond);
                        list_b_diamond.Add(bb_diamond);
                        list_L_background.Add(LL_background);
                        list_a_background.Add(aa_background);
                        list_b_background.Add(bb_background);
                        list_masklength.Add(mask_length);
                        list_maskarea.Add(mask_area);
                        list_mask2area.Add(mask2_area);
                        list_maskwidth.Add(mask_width);
                        list_maskheight.Add(mask_height);
                        list_maskpvheight.Add(mask_pvheight);
                        list_maskaspectratio.Add(mask_height / mask_width);
                    }
                }

                avg_L = list_L.Average();
                avg_a = list_a.Average();
                avg_b = list_b.Average();
                avg_L_diamond = list_L_diamond.Average();
                avg_a_diamond = list_a_diamond.Average();
                avg_b_diamond = list_b_diamond.Average();
                avg_L_background = list_L_background.Average();
                avg_a_background = list_a_background.Average();
                avg_b_background = list_b_background.Average();
                avg_masklength = list_masklength.Average();
                avg_maskarea = list_maskarea.Average();
                avg_C = calc_C(ref avg_a, ref avg_b);
                avg_H = calc_H(ref avg_a, ref avg_b);
                maxmin_widthratio = list_maskwidth.Max() / list_maskwidth.Min();
                max_aspectratio = list_maskaspectratio.Max();
                min_aspectratio = list_maskaspectratio.Min();
                avg_aspectratio = list_maskaspectratio.Average();

                avg_maskheight = list_maskheight.Average();
                avg_maskpvheight = list_maskpvheight.Average();

                L = avg_L;
                a = avg_a;
                b = avg_b;
                C = avg_C;
                H = avg_H;
                mask_L = avg_masklength;
                mask_A = avg_maskarea;
                mask2_A = list_mask2area.Average();

                lFirst = list_L[0];
                lLast = list_L[list_L.Count - 1];

                //Grouping the diamond
                String diamond_proportion = "";

                //if (maxmin_widthratio < _widthratio_RBC && min_aspectratio / max_aspectratio >= _aspect_min_max_RBC)
                if (maxmin_widthratio <= _widthratio_RBC)
                {
                    //RBC
                    //shift_C = _shiftC_RBC;

                    if (avg_aspectratio >= _aspect_min_RBC && avg_aspectratio <= _aspect_max_RBC)
                    {
                        diamond_proportion = "RBC: Normal ";
                        _diamond_group = DIAMOND_GROUPING.RBC;
                        volume = 1.0;
                    }
                    else if (avg_aspectratio > _aspect_max_RBC)
                    {
                        // Deep pavilion, High crown, Thick girdle
                        diamond_proportion = "RBC: High depth ";
                        _diamond_group = DIAMOND_GROUPING.RBC_HighDepth;
                        volume = 0.5; //For triangle mask
                    }
                    else
                    {
                        // Shallow pavilion
                        diamond_proportion = "RBC: Low depth ";
                        _diamond_group = DIAMOND_GROUPING.RBC_LowDepth;
                        volume = 1.0;
                    }
                }
                else if (maxmin_widthratio <= _widthratio_Fancy_L)
                {
                    //Fancy_L: HT, CU
                    //shift_C = _shiftC_FancyL;

                    if (min_aspectratio >= _aspect_min_FancyL && min_aspectratio <= _aspect_max_FancyL)
                    {
                        diamond_proportion = "FANCY_L: Normal ";
                        _diamond_group = DIAMOND_GROUPING.FANCY_L;
                        volume = 1.0;
                    }
                    else if (min_aspectratio < _aspect_min_FancyL)
                    {
                        diamond_proportion = "FANCY_L: Low depth ";
                        _diamond_group = DIAMOND_GROUPING.FANCY_L_LowDepth;
                        volume = 1.0;
                    }
                    else
                    {
                        diamond_proportion = "FANCY_L: High depth ";
                        _diamond_group = DIAMOND_GROUPING.FANCY_L_HighDepth;
                        volume = 1.0;
                    }
                }
                else if (maxmin_widthratio <= _widthratio_Fancy_H)
                {
                    //Fancy_H: REC, SQ, OV, PR
                    //shift_C = _shiftC_FancyH;

                    if (min_aspectratio >= _aspect_min_FancyH && min_aspectratio <= _aspect_max_FancyH)
                    {
                        diamond_proportion = "FANCY_H: Normal ";
                        _diamond_group = DIAMOND_GROUPING.FNACY_H;
                        volume = 1.0;
                    }
                    else if (min_aspectratio < _aspect_min_FancyH)
                    {
                        diamond_proportion = "FANCY_H: Low depth ";
                        _diamond_group = DIAMOND_GROUPING.FANCY_H_LowDepth;
                        volume = 1.0;
                    }
                    else
                    {
                        diamond_proportion = "FANCY_H: High depth ";
                        _diamond_group = DIAMOND_GROUPING.FANCY_H_HighDepth;
                        volume = 1.0;
                    }
                }
                else
                {
                    //Fancy_HH: mosly MQ
                    //shift_C = _shiftC_FancyHH;

                    if (min_aspectratio >= _aspect_min_FancyHH && min_aspectratio <= _aspect_max_FancyHH)
                    {
                        diamond_proportion = "FANCY_HH: Normal ";
                        _diamond_group = DIAMOND_GROUPING.FANCY_HH;
                        volume = 1.0;
                    }
                    else if (min_aspectratio < _aspect_min_FancyH)
                    {
                        diamond_proportion = "FANCY_HH: Low depth ";
                        _diamond_group = DIAMOND_GROUPING.FANCY_HH_LowDepth;
                        volume = 1.0;
                    }
                    else
                    {
                        diamond_proportion = "FANCY_HH: High depth ";
                        _diamond_group = DIAMOND_GROUPING.FANCY_HH_HighDepth;
                        volume = 1.0;
                    }
                }

                //debug

                //result_RBC = Boundary.GetGrade_shifting(H, C, L, shift_C);
                Boundary boundary = new Boundary();
                result_RBC = boundary.GetGrade(H, C, L, (int)_diamond_group);
                L_description = result_RBC["L_description"];
                C_description = result_RBC["C_description"];
                H_description = result_RBC["H_description"];
                boundary_version = result_RBC["Version"];
                refer_stone = result_RBC["Refer"];

                //getColorGrade_description(L, C, H, ref L_description, ref C_description, ref H_description, ref comment_RBC);

                comment1 = avg_L_diamond.ToString() + ", " + avg_a_diamond.ToString() + ", " + avg_b_diamond.ToString() + ", "
                    + avg_L_background.ToString() + ", " + avg_a_background.ToString() + ", " + avg_b_background.ToString() + ", "
                    + avg_L.ToString() + ", " + avg_a.ToString() + ", " + avg_b.ToString() + ", "
                    + avg_C.ToString() + ", " + avg_H.ToString() + ", "
                    + L_description + ", " + C_description + ", " + H_description + ", " + boundary_version + ", "
                    + avg_masklength.ToString() + ", " + avg_maskarea.ToString();

                comment1 = _deviceName + ", 1.0, " + comment1 + ", " + avg_maskheight.ToString() +
                                ", " + avg_maskpvheight.ToString() + ", " + maxmin_widthratio.ToString() + ", " +
                                min_aspectratio.ToString() + ", " + diamond_proportion;

                //if (_diamond_group == DIAMOND_GROUPING.RBC_LowDepth || _diamond_group == DIAMOND_GROUPING.RBC_HighDepth ||
                //    _diamond_group == DIAMOND_GROUPING.FANCY_L_LowDepth || _diamond_group == DIAMOND_GROUPING.FANCY_L_HighDepth ||
                //    _diamond_group == DIAMOND_GROUPING.FANCY_H_LowDepth || _diamond_group == DIAMOND_GROUPING.FANCY_H_HighDepth ||
                //    _diamond_group == DIAMOND_GROUPING.FANCY_HH_LowDepth || _diamond_group == DIAMOND_GROUPING.FANCY_HH_HighDepth)
                if (_diamond_group == DIAMOND_GROUPING.RBC_HighDepth)
                {
                    comment3 = "GO TO VISUAL - DEPTH";
                }
                else
                {
                    if (C_description == "N/A")
                    {
                        comment3 = "GO TO VISUAL";
                    }
                    else
                    {
                        if (refer_stone != "FALSE")
                        {
                            comment3 = "GO TO VISUAL - ANALYZE";
                        }

                    }
                }
                ////////////////////////////////////////////////////////////
                // If the diamond is high depth RBC (ice cream cone shape)
                // calcurate LCH with triangle mask
                ////////////////////////////////////////////////////////////

                if (_diamond_group == DIAMOND_GROUPING.RBC_HighDepth)
                {
                    i = 0;
                    j = 0;
                    if (hsvList1 != null)
                        hsvList1 = new List<Tuple<double, double, double, double, double, double>>();

                    foreach (Bitmap bm in imageList)
                    {
                        double LL = 0, aa = 0, bb = 0;
                        double LL_diamond = 0, aa_diamond = 0, bb_diamond = 0;
                        double LL_background = 0, aa_background = 0, bb_background = 0;
                        double mask_length = 0, mask_area = 0, mask_width = 0, mask_height = 0, mask_pvheight = 0, mask2_area = 0;
                        Bitmap image = bm;
                        Bitmap image_background = imgList_background[i];

                        i++;
                        bool calculateClusters = false;
                        if (i >= imgList_background.Count)
                        {
                            i = 0;
                            if (hsvList1 != null)
                                calculateClusters = true;
                        }

                        ////////////////////////////////////////////////
                        // Need to calc appropriate erode for each image
                        ////////////////////////////////////////////////

                        //double _a = list_maskheight[j];
                        //double _b = list_maskwidth[j];
                        //double temp = (_a + _b) / 4.0 - 1 / 2.0 * Math.Sqrt(((_a + _b) / 2) * ((_a + _b) / 2) - _a * _b * (1 - volume));
                        //temp = temp / 1.6;

                        //if (volume < 1.0) _erode = (int)temp;
                        //else _erode = 1;

                        ////////////////////////////////////////////////
                        ////////////////////////////////////////////////
                        //_erode = 1;

                        if (calcLab_diamond_background_all(ref image, ref image_background, ref LL_diamond,
                            ref aa_diamond, ref bb_diamond, ref LL_background, ref aa_background,
                            ref bb_background, ref LL, ref aa, ref bb, ref mask_length, ref mask_area,
                            ref mask_width, ref mask_height, ref mask_pvheight, useKthresholdLab, ref hsvList1, ref mask2_area, false,
                                brightAreaThreshold, darkAreaThreshold, calculateClusters) == true)
                        {
                            list_L_Fancy.Add(LL);
                            list_a_Fancy.Add(aa);
                            list_b_Fancy.Add(bb);
                            list_C_Fancy.Add(calc_C(ref aa, ref bb));
                            list_H_Fancy.Add(calc_H(ref aa, ref bb));
                            list_L_diamond_Fancy.Add(LL_diamond);
                            list_a_diamond_Fancy.Add(aa_diamond);
                            list_b_diamond_Fancy.Add(bb_diamond);
                            list_L_background_Fancy.Add(LL_background);
                            list_a_background_Fancy.Add(aa_background);
                            list_b_background_Fancy.Add(bb_background);
                            list_masklength_Fancy.Add(mask_length);
                            list_maskarea_Fancy.Add(mask_area);
                            list_mask2area_Fancy.Add(mask2_area);
                            list_maskwidth_Fancy.Add(mask_width);
                            list_maskheight_Fancy.Add(mask_height);
                            list_maskpvheigth_Fancy.Add(mask_pvheight);
                            list_maskaspectratio_Fancy.Add(mask_height / mask_width);
                        }
                        j++;
                    }

                    avg_L_Fancy = list_L_Fancy.Average();
                    avg_a_Fancy = list_a_Fancy.Average();
                    avg_b_Fancy = list_b_Fancy.Average();
                    avg_L_diamond_Fancy = list_L_diamond_Fancy.Average();
                    avg_a_diamond_Fancy = list_a_diamond_Fancy.Average();
                    avg_b_diamond_Fancy = list_b_diamond_Fancy.Average();
                    avg_L_background_Fancy = list_L_background_Fancy.Average();
                    avg_a_background_Fancy = list_a_background_Fancy.Average();
                    avg_b_background_Fancy = list_b_background_Fancy.Average();
                    avg_masklength_Fancy = list_masklength_Fancy.Average();
                    avg_maskarea_Fancy = list_maskarea_Fancy.Average();
                    avg_C_Fancy = calc_C(ref avg_a_Fancy, ref avg_b_Fancy);
                    avg_H_Fancy = calc_H(ref avg_a_Fancy, ref avg_b_Fancy);
                    maxmin_widthratio_Fancy = list_maskwidth_Fancy.Max() / list_maskwidth_Fancy.Min();
                    max_aspectratio_Fancy = list_maskaspectratio_Fancy.Max();
                    min_aspectratio_Fancy = list_maskaspectratio_Fancy.Min();
                    avg_aspectratio_Fancy = list_maskaspectratio_Fancy.Average();
                    mask2_A = list_mask2area_Fancy.Average();

                    //result_Fancy = Boundary.GetGrade_shifting(H, C, L, shift_C);
                    result_Fancy = boundary.GetGrade(avg_H_Fancy, avg_C_Fancy, avg_L_Fancy, (int)_diamond_group);
                    L_description_Fancy = result_Fancy["L_description"];
                    C_description_Fancy = result_Fancy["C_description"];
                    H_description_Fancy = result_Fancy["H_description"];
                    boundary_version = result_Fancy["Version"];
                    refer_stone = result_Fancy["Refer"];

                    //getColorGrade_description(avg_L_Fancy, avg_C_Fancy, avg_H_Fancy, ref L_description_Fancy, ref C_description_Fancy, ref H_description_Fancy, ref comment_Fancy);

                    comment2 = avg_L_diamond_Fancy.ToString() + ", " + avg_a_diamond_Fancy.ToString() + ", " + avg_b_diamond_Fancy.ToString() + ", "
                                   + avg_L_background_Fancy.ToString() + ", " + avg_a_background_Fancy.ToString() + ", " + avg_b_background_Fancy.ToString() + ", "
                                   + avg_L_Fancy.ToString() + ", " + avg_a_Fancy.ToString() + ", " + avg_b_Fancy.ToString() + ", "
                                   + avg_C_Fancy.ToString() + ", " + avg_H_Fancy.ToString() + ", "
                                   + L_description_Fancy + ", " + C_description_Fancy + ", " + H_description_Fancy + ", " + boundary_version + ", "
                                   + avg_masklength.ToString() + ", " + avg_maskarea.ToString();

                    comment2 = _deviceName + ", " + volume.ToString() + ", " + comment2 + ", " + avg_maskheight.ToString() +
                                ", " + avg_maskpvheight.ToString() + ", " + maxmin_widthratio.ToString() + ", " +
                                max_aspectratio.ToString() + ", " + diamond_proportion;

                    comment1 = comment2;
                    comment2 = "";
                    comment3 = "GO TO VISUAL - DEPTH";

                    L = avg_L_Fancy;
                    C = avg_C_Fancy;
                    H = avg_H_Fancy;
                    L_description = L_description_Fancy;
                    C_description = C_description_Fancy;
                    H_description = H_description_Fancy;

                    lFirst = list_L_Fancy[0];
                    lLast = list_L_Fancy[list_L_Fancy.Count - 1];
                }

                //Compare image count and mask count 
                if (list_C.Count < imageList.Count)
                {
                    comment3 = "Measure again: failed image ="
                     + list_C.Count.ToString() + "/" + imageList.Count.ToString();

                    return false;
                }

                if (Math.Abs(imageList.Count - imgList_background.Count) > 3)
                {
                    comment3 = "Measure again: diamond/bg =" + imageList.Count.ToString() +
                    "/" + imgList_background.Count.ToString();

                    return false;
                }

                if (photochromaL >= 0)
                {
                    double lDiff = Math.Abs(lLast - lFirst);
                    if (lDiff > photochromaL)
                        comment3 = "Color change[" + lDiff + "]";
                    else
                        comment3 = "";
                }

            }
            catch (Exception ex)
            {
                result = false;
                comment3 = "Measure again: process failure";
                Log.Error(ex);
            }

            return result;
        }

        //private double calc_C(ref double a, ref double b)
        //{
        //    return Math.Sqrt(a * a + b * b);
        //}

        //private double calc_H(ref double a, ref double b)
        //{
        //    return Math.Atan2(b, a) * 180 / Math.PI;
        //}

        private Boolean calcLab_diamond_background_all(ref Bitmap img_Bmp_diamond,
            ref Bitmap img_Bmp_background, ref double L_diamond, ref double a_diamond,
            ref double b_diamond, ref double L_background, ref double a_background,
            ref double b_background, ref double L, ref double a, ref double b,
            ref double mask_length, ref double mask_area, ref double mask_width, ref double mask_height, ref double mask_pvheight,
            bool useKthresholdLab, ref List<Tuple<double, double, double, double, double, double>> hsvList,
            ref double mask2_area,
            bool sRGB = false,
            int brightAreaThreshold = -1, int darkAreaThreshold = -1, bool calcCluster = false)
        {
            if (img_Bmp_diamond == null | img_Bmp_background == null) return false;
            if (img_Bmp_diamond.Width != img_Bmp_background.Width | img_Bmp_diamond.Height != img_Bmp_background.Height) return false;

            //// Bitmap -> IplImage
            Mat img_diamond;
            Mat img_background;
            Mat img_mask;
            Mat img_mask2 = null;
            img_diamond = BitmapConverter.ToMat(img_Bmp_diamond);
            img_background = BitmapConverter.ToMat(img_Bmp_background);
            img_mask = new Mat(new OpenCvSharp.Size(img_diamond.Width, img_diamond.Height), MatType.CV_8UC1, 0);

            //// Create software mask
            //Cv2.Zero(img_mask);
            // TODO: maskCreate using modified settings, need to fix
            if (maskCreate(ref img_diamond, ref img_mask, ref img_mask2, 3, brightAreaThreshold, darkAreaThreshold) == false)
            {
                return false;
            }

            //Mat mat_mask = new Mat(img_mask);
            Mat mat_mask = img_mask.Clone();

            if (mask.GetMaskType() == MASK_TYPE.MASK_NEW)
            {
                mask_area = Cv2.CountNonZero(mat_mask);
            }

            Mat img_Lab_diamond = new Mat(new OpenCvSharp.Size(img_diamond.Width, img_diamond.Height), MatType.CV_8UC3, 0);
            Mat img_Lab_background = new Mat(new OpenCvSharp.Size(img_background.Width, img_background.Height), MatType.CV_8UC3, 3);

            if (sRGB == true)
            {
                Cv2.CvtColor(img_diamond, img_Lab_diamond, ColorConversionCodes.BGR2Lab);
                Cv2.CvtColor(img_background, img_Lab_background, ColorConversionCodes.BGR2Lab);
            }
            else
            {
                Cv2.CvtColor(img_diamond, img_Lab_diamond, ColorConversionCodes.LBGR2Lab);
                Cv2.CvtColor(img_background, img_Lab_background, ColorConversionCodes.LBGR2Lab);
            }

            // Calculate Ave of L*a*b* 

            Scalar mean_diamond, mean_background;
            Scalar std_diamond, std_background;

            Cv2.MeanStdDev(img_Lab_diamond, out mean_diamond, out std_diamond, img_mask);
            L_diamond = mean_diamond.Val0 * 100 / 255;
            a_diamond = mean_diamond.Val1 - 128;
            b_diamond = mean_diamond.Val2 - 128;

            Cv2.MeanStdDev(img_Lab_background, out mean_background, out std_background, img_mask);
            L_background = mean_background.Val0 * 100 / 255;
            a_background = mean_background.Val1 - 128;
            b_background = mean_background.Val2 - 128;

            L = _conv_L * (L_diamond / L_background - _shift_L);
            a = _conv_a * (a_diamond - a_background - _shift_a);
            b = _conv_b * (b_diamond - b_background - _shift_b);
            //a = a_diamond - a_background;
            //b = b_diamond - b_background;

            if (hsvList != null)
            {
                //Mat mat_mask2 = new Mat(img_mask2);
                Mat mat_mask2 = img_mask2.Clone();
                mask2_area = Cv2.CountNonZero(mat_mask2);

                Cv2.MeanStdDev(img_Lab_diamond, out mean_diamond, out std_diamond, img_mask2);
                var L_diamond1 = mean_diamond.Val0 * 100 / 255;
                var a_diamond1 = mean_diamond.Val1 - 128;
                var b_diamond1 = mean_diamond.Val2 - 128;

                Cv2.MeanStdDev(img_Lab_background, out mean_background, out std_background, img_mask2);
                var L_background1 = mean_background.Val0 * 100 / 255;
                var a_background1 = mean_background.Val1 - 128;
                var b_background1 = mean_background.Val2 - 128;

                var L1 = _conv_L * (L_diamond1 / L_background1 - _shift_L);
                var a1 = _conv_a * (a_diamond1 - a_background1 - _shift_a);
                var b1 = _conv_b * (b_diamond1 - b_background1 - _shift_b);

                if (hsvList.Count > 0)
                {
                    var tple = hsvList[0];
                    L1 = tple.Item1 + L1;
                    a1 = tple.Item2 + a1;
                    b1 = tple.Item3 + b1;
                    hsvList[0] = new Tuple<double, double, double, double, double, double>(L1, a1, b1, 0, 0, 100);
                }
                else
                {
                    hsvList.Add(new Tuple<double, double, double, double, double, double>(L1, a1, b1, 0, 0, 100));
                }

                if (calcCluster)
                {
                    //Mat mat_diamond = new Mat(img_diamond);
                    Mat mat_diamond = img_diamond.Clone();
                    //Mat mat_bg = new Mat(img_background);
                    Mat mat_bg = img_background.Clone();
                    Mat src2 = new Mat();
                    Mat src = new Mat();
                    Mat bg = new Mat();
                    mat_diamond.CopyTo(src2, mat_mask2);
                    mat_diamond.CopyTo(src, mat_mask);
                    mat_bg.CopyTo(bg, mat_mask2);
                    Mat src_lab = new Mat();
                    Cv2.CvtColor(src2, src_lab, ColorConversionCodes.LBGR2Lab);

                    Cv2.ImWrite(@"C:\gColorFancy\Image\original_image.jpg", mat_diamond);
                    Cv2.ImWrite(@"C:\gColorFancy\Image\original_bg.jpg", mat_bg);
                    Cv2.ImWrite(@"C:\gColorFancy\Image\img_mask2.jpg", mat_mask2);
                    Cv2.ImWrite(@"C:\gColorFancy\Image\img_mask.jpg", mat_mask);
                    Cv2.ImWrite(@"C:\gColorFancy\Image\masked_image.jpg", src);
                    Cv2.ImWrite(@"C:\gColorFancy\Image\masked_image2.jpg", src2);
                    Cv2.ImWrite(@"C:\gColorFancy\Image\masked_bg.jpg", bg);

                    List<int> clusterCounts = new List<int>() { 2, 3, 4, 5 };//+1 to account for masked off dark area
                    Mat points = new Mat();
                    src_lab.ConvertTo(points, MatType.CV_32FC3);
                    points = points.Reshape(3, src_lab.Rows * src_lab.Cols);

                    foreach (int clusterCount in clusterCounts)
                    {
                        int startIndex = hsvList.Count;


                        Mat clusters = Mat.Zeros(points.Size(), MatType.CV_32SC1);
                        Mat centers = new Mat();

                        Cv2.Kmeans(points, clusterCount, clusters, new TermCriteria(CriteriaType.Eps | CriteriaType.MaxIter, 10, 1.0),
                            3, KMeansFlags.PpCenters, centers);

                        Dictionary<int, int[]> data = new Dictionary<int, int[]>();
                        for (int c = 0; c < clusterCount; c++)
                            data.Add(c, new int[4]);

                        MatOfByte3 mat3 = new MatOfByte3(src_lab); // cv::Mat_<cv::Vec3b>
                        var indexer = mat3.GetIndexer();

                        Mat dst_img = Mat.Zeros(src_lab.Size(), src_lab.Type());

                        MatOfByte3 mat3_dst = new MatOfByte3(dst_img); // cv::Mat_<cv::Vec3b>
                        var indexer_dst = mat3_dst.GetIndexer();

                        for (int y = 0, n = 0; y < src_lab.Height; y++)
                        {
                            for (int x = 0; x < src_lab.Width; x++)
                            {
                                n++;
                                int clusterIdx = clusters.At<int>(n);
                                Vec3b lab = indexer[y, x];
                                data[clusterIdx][0] += (lab.Item0 * 100 / 255);
                                data[clusterIdx][1] += lab.Item1 != 0 ? (lab.Item1 - 128) : 0;
                                data[clusterIdx][2] += lab.Item2 != 0 ? (lab.Item2 - 128) : 0;
                                data[clusterIdx][3]++;

                                Vec3b color;
                                color.Item0 = (byte)(centers.At<float>(clusterIdx, 0));
                                color.Item1 = (byte)(centers.At<float>(clusterIdx, 1));
                                color.Item2 = (byte)(centers.At<float>(clusterIdx, 2));    // R <- B
                                indexer_dst[y, x] = color;
                            }
                        }

                        Mat dst_img_bgr = Mat.Zeros(src2.Size(), src2.Type());
                        try
                        {
                            Cv2.CvtColor(dst_img, dst_img_bgr, ColorConversionCodes.Lab2LBGR);
                        }
                        catch (Exception exce)
                        {

                        }

                        Cv2.ImWrite(@"C:\gColorFancy\Image\clusters" + clusterCount + ".jpg", dst_img_bgr);

                        for (int j = 0; j < clusterCount; j++)
                        {
                            var a2 = (double)((double)data[j][1] / data[j][3]);
                            var b2 = (double)((double)data[j][2] / data[j][3]);
                            var C2 = calc_C(ref a2, ref b2);
                            var H2 = calc_H(ref a2, ref b2);

                            hsvList.Add(new Tuple<double, double, double, double, double, double>((data[j][0] / data[j][3]),
                                a2,
                                b2,
                                C2,
                                H2,
                                Math.Round((double)(data[j][3] * 100) / (src_lab.Rows * src_lab.Cols), 1)));
                        }

                    }
                }
            }

            //Cv.ReleaseImage(img_diamond);
            //Cv.ReleaseImage(img_background);
            //if (img_mask2 != null)
            //    Cv.ReleaseImage(img_mask2);
            //Cv.ReleaseImage(img_mask);
            //Cv.ReleaseImage(img_Lab_diamond);
            //Cv.ReleaseImage(img_Lab_background);

            return true;
        }

        private Boolean calcLab_diamond_background_all(ref Bitmap img_Bmp_diamond,
            ref Bitmap img_Bmp_background, ref Bitmap maskBmp, ref double L_diamond, ref double a_diamond,
            ref double b_diamond, ref double L_background, ref double a_background,
            ref double b_background, ref double L, ref double a, ref double b,
            ref double mask_length, ref double mask_area, ref double mask_width, ref double mask_height, ref double mask_pvheight,
            bool useKthresholdLab, ref List<Tuple<double, double, double, double, double, double>> hsvList,
            ref double mask2_area,
            bool sRGB = false,
            int brightAreaThreshold = -1, int darkAreaThreshold = -1, bool calcCluster = false)
        {
            if (img_Bmp_diamond == null | img_Bmp_background == null) return false;
            if (img_Bmp_diamond.Width != img_Bmp_background.Width | img_Bmp_diamond.Height != img_Bmp_background.Height) return false;

            //// Bitmap -> IplImage
            Mat img_diamond;
            Mat img_background;
            Mat img_mask;
            Mat img_mask2 = null;
            img_diamond = BitmapConverter.ToMat(img_Bmp_diamond);
            img_background = BitmapConverter.ToMat(img_Bmp_background);
            img_mask = new Mat(new OpenCvSharp.Size(img_diamond.Width, img_diamond.Height), MatType.CV_8UC1, 0);

            //// Create software mask
            //Cv2.Zero(img_mask);
            // TODO: maskCreate using modified settings, need to fix
            if (maskBmp == null)
            {
                if (maskCreate(ref img_diamond, ref img_mask, ref img_mask2, 3, brightAreaThreshold, darkAreaThreshold) == false)
                {
                    return false;
                }
            } else
            {
                img_mask = BitmapConverter.ToMat(maskBmp);
            }
            //Mat mat_mask = new Mat(img_mask);
            Mat mat_mask = img_mask.Clone();

            if (mask.GetMaskType() == MASK_TYPE.MASK_NEW)
            {
                mask_area = Cv2.CountNonZero(mat_mask);
            }

            Mat img_Lab_diamond = new Mat(new OpenCvSharp.Size(img_diamond.Width, img_diamond.Height), MatType.CV_8UC3, 0);
            Mat img_Lab_background = new Mat(new OpenCvSharp.Size(img_background.Width, img_background.Height), MatType.CV_8UC3, 3);

            if (sRGB == true)
            {
                Cv2.CvtColor(img_diamond, img_Lab_diamond, ColorConversionCodes.BGR2Lab);
                Cv2.CvtColor(img_background, img_Lab_background, ColorConversionCodes.BGR2Lab);
            }
            else
            {
                Cv2.CvtColor(img_diamond, img_Lab_diamond, ColorConversionCodes.LBGR2Lab);
                Cv2.CvtColor(img_background, img_Lab_background, ColorConversionCodes.LBGR2Lab);
            }

            // Calculate Ave of L*a*b* 

            Scalar mean_diamond, mean_background;
            Scalar std_diamond, std_background;

            Cv2.MeanStdDev(img_Lab_diamond, out mean_diamond, out std_diamond, img_mask);
            L_diamond = mean_diamond.Val0 * 100 / 255;
            a_diamond = mean_diamond.Val1 - 128;
            b_diamond = mean_diamond.Val2 - 128;

            Cv2.MeanStdDev(img_Lab_background, out mean_background, out std_background, img_mask);
            L_background = mean_background.Val0 * 100 / 255;
            a_background = mean_background.Val1 - 128;
            b_background = mean_background.Val2 - 128;

            L = _conv_L * (L_diamond / L_background - _shift_L);
            a = _conv_a * (a_diamond - a_background - _shift_a);
            b = _conv_b * (b_diamond - b_background - _shift_b);
            //a = a_diamond - a_background;
            //b = b_diamond - b_background;

            if (hsvList != null)
            {
                //Mat mat_mask2 = new Mat(img_mask2);
                Mat mat_mask2 = img_mask2 != null ? img_mask2.Clone() : new Mat();
                mask2_area = mat_mask2 != null ? Cv2.CountNonZero(mat_mask2) : 0;

                Cv2.MeanStdDev(img_Lab_diamond, out mean_diamond, out std_diamond, img_mask2);
                var L_diamond1 = mean_diamond.Val0 * 100 / 255;
                var a_diamond1 = mean_diamond.Val1 - 128;
                var b_diamond1 = mean_diamond.Val2 - 128;

                Cv2.MeanStdDev(img_Lab_background, out mean_background, out std_background, img_mask2);
                var L_background1 = mean_background.Val0 * 100 / 255;
                var a_background1 = mean_background.Val1 - 128;
                var b_background1 = mean_background.Val2 - 128;

                var L1 = _conv_L * (L_diamond1 / L_background1 - _shift_L);
                var a1 = _conv_a * (a_diamond1 - a_background1 - _shift_a);
                var b1 = _conv_b * (b_diamond1 - b_background1 - _shift_b);

                if (hsvList.Count > 0)
                {
                    var tple = hsvList[0];
                    L1 = tple.Item1 + L1;
                    a1 = tple.Item2 + a1;
                    b1 = tple.Item3 + b1;
                    hsvList[0] = new Tuple<double, double, double, double, double, double>(L1, a1, b1, 0, 0, 100);
                }
                else
                {
                    hsvList.Add(new Tuple<double, double, double, double, double, double>(L1, a1, b1, 0, 0, 100));
                }

                if (calcCluster)
                {
                    //Mat mat_diamond = new Mat(img_diamond);
                    Mat mat_diamond = img_diamond.Clone();
                    //Mat mat_bg = new Mat(img_background);
                    Mat mat_bg = img_background.Clone();
                    Mat src2 = new Mat();
                    Mat src = new Mat();
                    Mat bg = new Mat();
                    mat_diamond.CopyTo(src2, mat_mask2);
                    mat_diamond.CopyTo(src, mat_mask);
                    mat_bg.CopyTo(bg, mat_mask2);
                    Mat src_lab = new Mat();
                    Cv2.CvtColor(src2, src_lab, ColorConversionCodes.LBGR2Lab);

                    Cv2.ImWrite(@"C:\gColorFancy\Image\original_image.jpg", mat_diamond);
                    Cv2.ImWrite(@"C:\gColorFancy\Image\original_bg.jpg", mat_bg);
                    Cv2.ImWrite(@"C:\gColorFancy\Image\img_mask2.jpg", mat_mask2);
                    Cv2.ImWrite(@"C:\gColorFancy\Image\img_mask.jpg", mat_mask);
                    Cv2.ImWrite(@"C:\gColorFancy\Image\masked_image.jpg", src);
                    Cv2.ImWrite(@"C:\gColorFancy\Image\masked_image2.jpg", src2);
                    Cv2.ImWrite(@"C:\gColorFancy\Image\masked_bg.jpg", bg);

                    List<int> clusterCounts = new List<int>() { 2, 3, 4, 5 };//+1 to account for masked off dark area
                    Mat points = new Mat();
                    src_lab.ConvertTo(points, MatType.CV_32FC3);
                    points = points.Reshape(3, src_lab.Rows * src_lab.Cols);

                    foreach (int clusterCount in clusterCounts)
                    {
                        int startIndex = hsvList.Count;


                        Mat clusters = Mat.Zeros(points.Size(), MatType.CV_32SC1);
                        Mat centers = new Mat();

                        Cv2.Kmeans(points, clusterCount, clusters, new TermCriteria(CriteriaType.Eps | CriteriaType.MaxIter, 10, 1.0),
                            3, KMeansFlags.PpCenters, centers);

                        Dictionary<int, int[]> data = new Dictionary<int, int[]>();
                        for (int c = 0; c < clusterCount; c++)
                            data.Add(c, new int[4]);

                        MatOfByte3 mat3 = new MatOfByte3(src_lab); // cv::Mat_<cv::Vec3b>
                        var indexer = mat3.GetIndexer();

                        Mat dst_img = Mat.Zeros(src_lab.Size(), src_lab.Type());

                        MatOfByte3 mat3_dst = new MatOfByte3(dst_img); // cv::Mat_<cv::Vec3b>
                        var indexer_dst = mat3_dst.GetIndexer();

                        for (int y = 0, n = 0; y < src_lab.Height; y++)
                        {
                            for (int x = 0; x < src_lab.Width; x++)
                            {
                                n++;
                                int clusterIdx = clusters.At<int>(n);
                                Vec3b lab = indexer[y, x];
                                data[clusterIdx][0] += (lab.Item0 * 100 / 255);
                                data[clusterIdx][1] += lab.Item1 != 0 ? (lab.Item1 - 128) : 0;
                                data[clusterIdx][2] += lab.Item2 != 0 ? (lab.Item2 - 128) : 0;
                                data[clusterIdx][3]++;

                                Vec3b color;
                                color.Item0 = (byte)(centers.At<float>(clusterIdx, 0));
                                color.Item1 = (byte)(centers.At<float>(clusterIdx, 1));
                                color.Item2 = (byte)(centers.At<float>(clusterIdx, 2));    // R <- B
                                indexer_dst[y, x] = color;
                            }
                        }

                        Mat dst_img_bgr = Mat.Zeros(src2.Size(), src2.Type());
                        try
                        {
                            Cv2.CvtColor(dst_img, dst_img_bgr, ColorConversionCodes.Lab2LBGR);
                        }
                        catch (Exception exce)
                        {

                        }

                        Cv2.ImWrite(@"C:\gColorFancy\Image\clusters" + clusterCount + ".jpg", dst_img_bgr);

                        for (int j = 0; j < clusterCount; j++)
                        {
                            var a2 = (double)((double)data[j][1] / data[j][3]);
                            var b2 = (double)((double)data[j][2] / data[j][3]);
                            var C2 = calc_C(ref a2, ref b2);
                            var H2 = calc_H(ref a2, ref b2);

                            hsvList.Add(new Tuple<double, double, double, double, double, double>((data[j][0] / data[j][3]),
                                a2,
                                b2,
                                C2,
                                H2,
                                Math.Round((double)(data[j][3] * 100) / (src_lab.Rows * src_lab.Cols), 1)));
                        }

                    }
                }
            }

            //Cv.ReleaseImage(img_diamond);
            //Cv.ReleaseImage(img_background);
            //if (img_mask2 != null)
            //    Cv.ReleaseImage(img_mask2);
            //Cv.ReleaseImage(img_mask);
            //Cv.ReleaseImage(img_Lab_diamond);
            //Cv.ReleaseImage(img_Lab_background);

            return true;
        }

        public Boolean maskCreate(ref Mat img, ref Mat img_mask)
        {
            if (img == null | img_mask == null) return false;
            if (img.Width != img_mask.Width | img.Height != img_mask.Height) return false;
            if (mask == null) return false;

            mask.SetSrc(img);
            bool result = mask.Create(out Bitmap m, out Bitmap m2);
            if (result)
            {
                img_mask = BitmapConverter.ToMat(m);
            }
            else
            {
                return false;
            }
            return true;
        }

        public override Boolean maskCreate(ref Bitmap img, out Bitmap img_mask, bool displayMask = false)
        {
            img_mask = null;
            if (img == null) return false;

            mask.SetSrc(img);
            bool result = mask.Create(out img_mask, out Bitmap img_mask2, -1, -1, displayMask);
            if (!result)
            {
                return false;
            }
            return true;
        }

        public Boolean maskCreate(ref Mat img, ref Mat img_mask, ref Mat img_mask_spc, int avenum, int brightAreaThreshold = -1, int darkAreaThreshold = -1)
        {
            if (img == null | img_mask == null) return false;
            if (img.Width != img_mask.Width | img.Height != img_mask.Height) return false;
            if (mask == null) return false;

            // save original avenum
            int num = mask.GetAveNum();
            mask.SetAveNum(avenum);

            mask.SetSrc(img);
            bool result = mask.Create(out Bitmap m, out Bitmap m2, brightAreaThreshold, darkAreaThreshold);

            // reset to original avenum
            mask.SetAveNum(num);

            if (result)
            {
                img_mask = BitmapConverter.ToMat(m);
                if(m2 != null)
                {
                    img_mask_spc = BitmapConverter.ToMat(m2);
                }
            }
            else
            {
                return false;
            }
            return true;
        }

        public override Boolean check_diamond_centered(ref Bitmap img_Bmp_diamond, int x, int y, ref string comment, int maxDistance)
        {
            //// Bitmap -> IplImage
            Mat img_diamond;
            Mat img_mask;
            img_diamond = BitmapConverter.ToMat(img_Bmp_diamond);
            img_mask = new Mat(new OpenCvSharp.Size(img_diamond.Width, img_diamond.Height), MatType.CV_8UC1, 0);

            //// Create software mask
            //Cv.Zero(img_mask);

            _diamond_group = DIAMOND_GROUPING.Default;

            try
            {
                if (maskCreate(ref img_diamond, ref img_mask) == false)
                {
                    comment = "Image processing error";
                    return false;
                }

                if (mask.GetMaskType() == MASK_TYPE.MASK_NEW)
                {
                    //Mat img_mask_mat = new Mat(img_mask);
                    //Mat img_mask_mat = img_mask.Clone();

                    Mat img_mask_mat = new Mat(img_mask.Size(), img_mask.Type());
                    img_mask.CopyTo(img_mask_mat);

                    //img_mask_mat.SaveImage(@"P:\temp\img_mask_mat.jpg");
                    Moments m = Cv2.Moments(img_mask_mat);
                    Point2d center = new Point2d(m.M10 / m.M00, m.M01 / m.M00);

                    double euclidDistanceFromCenter = Math.Sqrt(((x - center.X) * (x - center.X)) + ((y - center.Y) * (y - center.Y)));

                    if (mask.Area == 0)
                    {
                        comment = "No object detected.";
                        return false;
                    }
                    else if (euclidDistanceFromCenter > maxDistance)
                    {
                        comment = "Check diamond position.";
                        return false;
                    }

                }
                else
                {
                    comment = "Bad mask setting.";
                    return false;
                }
            }
            finally
            {
                //Cv.ReleaseImage(img_diamond);
                //Cv.ReleaseImage(img_mask);
            }

            return true;
        }

        public override void check_pearl_max_lumi(ref Bitmap image, ref string comment, out double maxValue, out double shift)
        {
            Mat srcImg = BitmapConverter.ToMat(image);
            Mat src_gray = new Mat();
            Cv2.CvtColor(srcImg, src_gray, ColorConversionCodes.BGR2GRAY);

            Cv2.MinMaxLoc(src_gray, out double min, out maxValue);

            Rect roi = new Rect(0, 0, 50, 50);
            Mat roiImg = new Mat(src_gray, roi);
            Scalar mean, std;
            Cv2.MeanStdDev(roiImg, out mean, out std);
            shift = mean.Val0;
        }

        public override void CalcPearlLusterWidthHeight(List<System.Drawing.Bitmap> imgList, ref double shiftValue, ref double maxValue, out double w, out double h)
        {
            double threshold = (maxValue - shiftValue) * 0.5 + shiftValue;
            Mat src = BitmapConverter.ToMat(imgList[0]);
            Mat src_gray = new Mat();
            Cv2.CvtColor(src, src_gray, ColorConversionCodes.BGR2GRAY);

            Mat img_mask = new Mat(new OpenCvSharp.Size(src_gray.Width, src_gray.Height), MatType.CV_8UC1, 0);
            Cv2.Threshold(src_gray, img_mask, threshold, 255, ThresholdTypes.Binary);

            Mat morph_element = Cv2.GetStructuringElement(MorphShapes.Ellipse, new OpenCvSharp.Size(2, 2), new OpenCvSharp.Point(1, 1));
            Mat mask_morph = img_mask.MorphologyEx(MorphTypes.Open, morph_element);
            Cv2.ImWrite(@"C:\temp\pearlanalyzer\mask.jpg", img_mask);
            Cv2.ImWrite(@"C:\temp\pearlanalyzer\mask_morph.jpg", mask_morph);

            // caluculate width
            Mat resRow = new Mat(new OpenCvSharp.Size(img_mask.Width, 1), MatType.CV_32FC1, 0);
            Cv2.Reduce(mask_morph, resRow, ReduceDimension.Row, ReduceTypes.Sum, MatType.CV_32FC1);
            int init = 0;
            for (int i = 0; i < resRow.Cols; i++)
            {
                float e = resRow.Get<float>(0, i);
                if (e > 0)
                {
                    init = i;
                    break;
                }
            }

            int end = 0;
            for (int i = resRow.Cols - 1; i >= 0; i--)
            {
                float e = resRow.Get<float>(0, i);
                if (e > 0)
                {
                    end = i;
                    break;
                }
            }

            w = end - init;

            // calculate height
            Mat resCol = new Mat(new OpenCvSharp.Size(1, img_mask.Height), MatType.CV_32FC1, 0);
            Cv2.Reduce(mask_morph, resCol, ReduceDimension.Column, ReduceTypes.Sum, MatType.CV_32FC1);
            init = 0;
            for (int i = 0; i < resCol.Rows; i++)
            {
                float e = resCol.Get<float>(i, 0);
                if (e > 0)
                {
                    init = i;
                    break;
                }
            }

            end = 0;
            for (int i = resCol.Rows - 1; i >= 0; i--)
            {
                float e = resCol.Get<float>(i, 0);
                if (e > 0)
                {
                    end = i;
                    break;
                }
            }
            h = end - init;
        }

        public override void setLabAdjustment(double Conv_L, double Conv_a, double Conv_b, double Shift_L, double Shift_a, double Shift_b)
        {
            _shift_L = Shift_L;
            _shift_a = Shift_a;
            _shift_b = Shift_b;
            _conv_L = Conv_L;
            _conv_a = Conv_a;
            _conv_b = Conv_b;
        }


        class FancyColorAnalyzer
        {
            public FancyColorAnalyzer(ImageAnalyzer_FancyColor imageAnalyzer, ManualResetEvent doneEvent, ref Bitmap img_Bmp_diamond, ref Bitmap img_Bmp_background,
                bool useKthresholdLab, 
                ref List<Tuple<double, double, double, double, double, double>> hsvList,
                bool sRGB = false,
                int brightAreaThreshold = -1, int darkAreaThreshold = -1, bool calcCluster = false)
            {
                this.imageAnalyzer = imageAnalyzer;
                this.doneEvent = doneEvent;
                this.img_Bmp_diamond = img_Bmp_diamond;
                this.img_Bmp_background = img_Bmp_background;
                this.useKthresholdLab = useKthresholdLab;
                this.hsvList = hsvList;
                this.sRGB = sRGB;
                this.brightAreaThreshold = brightAreaThreshold;
                this.darkAreaThreshold = darkAreaThreshold;
                this.calcCluster = calcCluster;
                mask = new NewMask();
            }

            static readonly object locker = new object();
            ImageAnalyzer_FancyColor imageAnalyzer;
            ManualResetEvent doneEvent;
            Mask mask;
            Bitmap img_Bmp_diamond;
            Bitmap img_Bmp_background;
            public double L_diamond = 0;
            public double a_diamond = 0;
            public double b_diamond = 0;
            public double L_background = 0;
            public double a_background = 0;
            public double b_background = 0;
            public double L = 0;
            public double a = 0;
            public double b = 0;
            public double mask_length = 0;
            public double mask_area = 0;
            public double mask_width = 0;
            public double mask_height = 0;
            public double mask_pvheight = 0;
            public double mask2_area = 0;
            bool useKthresholdLab;
            List<Tuple<double, double, double, double, double, double>> hsvList;
            bool sRGB;
            int brightAreaThreshold;
            int darkAreaThreshold;
            bool calcCluster;
            public bool result = false;

            public void ThreadPoolCallback(Object threadContext)
            {
                int threadIndex = (int)threadContext;
                Console.WriteLine("Thread {0} started...", threadIndex);
                try
                {
                    if (img_Bmp_diamond == null | img_Bmp_background == null)
                    {
                        doneEvent.Set();
                        return;
                    }
                    if (img_Bmp_diamond.Width != img_Bmp_background.Width | img_Bmp_diamond.Height != img_Bmp_background.Height)
                    {
                        doneEvent.Set();
                        return;
                    }
                    //// Bitmap -> IplImage
                    Mat img_diamond;
                    Mat img_background;
                    Mat img_mask;
                    Mat img_mask2 = null;
                    img_diamond = BitmapConverter.ToMat(img_Bmp_diamond);
                    img_background = BitmapConverter.ToMat(img_Bmp_background);
                    img_mask = new Mat(new OpenCvSharp.Size(img_diamond.Width, img_diamond.Height), MatType.CV_8UC1, 0);

                    //// Create software mask
                    //Cv2.Zero(img_mask);
                    // TODO: maskCreate using modified settings, need to fix
                    if (maskCreate(ref img_diamond, ref img_mask, ref img_mask2, 3, brightAreaThreshold, darkAreaThreshold) == false)
                    {
                        doneEvent.Set();
                        return;
                    }

                    //Mat mat_mask = new Mat(img_mask);
                    Mat mat_mask = img_mask.Clone();

                    if (mask.GetMaskType() == MASK_TYPE.MASK_NEW)
                    {
                        mask_area = Cv2.CountNonZero(mat_mask);
                    }

                    Mat img_Lab_diamond = new Mat(new OpenCvSharp.Size(img_diamond.Width, img_diamond.Height), MatType.CV_8UC3, 0);
                    Mat img_Lab_background = new Mat(new OpenCvSharp.Size(img_background.Width, img_background.Height), MatType.CV_8UC3, 3);

                    if (sRGB == true)
                    {
                        Cv2.CvtColor(img_diamond, img_Lab_diamond, ColorConversionCodes.BGR2Lab);
                        Cv2.CvtColor(img_background, img_Lab_background, ColorConversionCodes.BGR2Lab);
                    }
                    else
                    {
                        Cv2.CvtColor(img_diamond, img_Lab_diamond, ColorConversionCodes.LBGR2Lab);
                        Cv2.CvtColor(img_background, img_Lab_background, ColorConversionCodes.LBGR2Lab);
                    }

                    // Calculate Ave of L*a*b* 

                    Scalar mean_diamond, mean_background;
                    Scalar std_diamond, std_background;

                    Cv2.MeanStdDev(img_Lab_diamond, out mean_diamond, out std_diamond, img_mask);
                    L_diamond = mean_diamond.Val0 * 100 / 255;
                    a_diamond = mean_diamond.Val1 - 128;
                    b_diamond = mean_diamond.Val2 - 128;

                    Cv2.MeanStdDev(img_Lab_background, out mean_background, out std_background, img_mask);
                    L_background = mean_background.Val0 * 100 / 255;
                    a_background = mean_background.Val1 - 128;
                    b_background = mean_background.Val2 - 128;

                    L = imageAnalyzer._conv_L * (L_diamond / L_background - imageAnalyzer._shift_L);
                    a = imageAnalyzer._conv_a * (a_diamond - a_background - imageAnalyzer._shift_a);
                    b = imageAnalyzer._conv_b * (b_diamond - b_background - imageAnalyzer._shift_b);
                    //a = a_diamond - a_background;
                    //b = b_diamond - b_background;

                    if (hsvList != null)
                    {
                        //Mat mat_mask2 = new Mat(img_mask2);
                        Mat mat_mask2 = img_mask2.Clone();
                        mask2_area = Cv2.CountNonZero(mat_mask2);

                        Cv2.MeanStdDev(img_Lab_diamond, out mean_diamond, out std_diamond, img_mask2);
                        var L_diamond1 = mean_diamond.Val0 * 100 / 255;
                        var a_diamond1 = mean_diamond.Val1 - 128;
                        var b_diamond1 = mean_diamond.Val2 - 128;

                        Cv2.MeanStdDev(img_Lab_background, out mean_background, out std_background, img_mask2);
                        var L_background1 = mean_background.Val0 * 100 / 255;
                        var a_background1 = mean_background.Val1 - 128;
                        var b_background1 = mean_background.Val2 - 128;

                        var L1 = imageAnalyzer._conv_L * (L_diamond1 / L_background1 - imageAnalyzer._shift_L);
                        var a1 = imageAnalyzer._conv_a * (a_diamond1 - a_background1 - imageAnalyzer._shift_a);
                        var b1 = imageAnalyzer._conv_b * (b_diamond1 - b_background1 - imageAnalyzer._shift_b);

                        //if (hsvList.Count > 0)
                        //{
                        //    var tple = hsvList[0];
                        //    L1 = tple.Item1 + L1;
                        //    a1 = tple.Item2 + a1;
                        //    b1 = tple.Item3 + b1;
                        //    hsvList[0] = new Tuple<double, double, double, double, double, double>(L1, a1, b1, 0, 0, 100);
                        //}
                        //else
                        //{
                        //    hsvList.Add(new Tuple<double, double, double, double, double, double>(L1, a1, b1, 0, 0, 100));
                        //}

                        lock (locker)
                        {
                            if(hsvList.Count == 0)
                            {
                                Console.WriteLine("hsvList must have one initialized item");
                                doneEvent.Set();
                                return;
                            }
                            var tple = hsvList[0];
                            L1 = tple.Item1 + L1;
                            a1 = tple.Item2 + a1;
                            b1 = tple.Item3 + b1;
                            hsvList[0] = new Tuple<double, double, double, double, double, double>(L1, a1, b1, 0, 0, 100);
                        }

                        if (calcCluster)
                        {
                            Console.WriteLine("Cluster calculation thread {0} index...", threadIndex);
                            //Mat mat_diamond = new Mat(img_diamond);
                            Mat mat_diamond = img_diamond.Clone();
                            //Mat mat_bg = new Mat(img_background);
                            Mat mat_bg = img_background.Clone();
                            Mat src2 = new Mat();
                            Mat src = new Mat();
                            Mat bg = new Mat();
                            mat_diamond.CopyTo(src2, mat_mask2);
                            mat_diamond.CopyTo(src, mat_mask);
                            mat_bg.CopyTo(bg, mat_mask2);
                            Mat src_lab = new Mat();
                            Cv2.CvtColor(src2, src_lab, ColorConversionCodes.LBGR2Lab);

                            Cv2.ImWrite(@"C:\gColorFancy\Image\original_image.jpg", mat_diamond);
                            Cv2.ImWrite(@"C:\gColorFancy\Image\original_bg.jpg", mat_bg);
                            Cv2.ImWrite(@"C:\gColorFancy\Image\img_mask2.jpg", mat_mask2);
                            Cv2.ImWrite(@"C:\gColorFancy\Image\img_mask.jpg", mat_mask);
                            Cv2.ImWrite(@"C:\gColorFancy\Image\masked_image.jpg", src);
                            Cv2.ImWrite(@"C:\gColorFancy\Image\masked_image2.jpg", src2);
                            Cv2.ImWrite(@"C:\gColorFancy\Image\masked_bg.jpg", bg);

                            List<int> clusterCounts = new List<int>() { 2, 3, 4, 5 };//+1 to account for masked off dark area
                            Mat points = new Mat();
                            src_lab.ConvertTo(points, MatType.CV_32FC3);
                            points = points.Reshape(3, src_lab.Rows * src_lab.Cols);

                            foreach (int clusterCount in clusterCounts)
                            {
                                int startIndex = hsvList.Count;


                                Mat clusters = Mat.Zeros(points.Size(), MatType.CV_32SC1);
                                Mat centers = new Mat();

                                Cv2.Kmeans(points, clusterCount, clusters, new TermCriteria(CriteriaType.Eps | CriteriaType.MaxIter, 10, 1.0),
                                    3, KMeansFlags.PpCenters, centers);

                                Dictionary<int, int[]> data = new Dictionary<int, int[]>();
                                for (int c = 0; c < clusterCount; c++)
                                    data.Add(c, new int[4]);

                                MatOfByte3 mat3 = new MatOfByte3(src_lab); // cv::Mat_<cv::Vec3b>
                                var indexer = mat3.GetIndexer();

                                Mat dst_img = Mat.Zeros(src_lab.Size(), src_lab.Type());

                                MatOfByte3 mat3_dst = new MatOfByte3(dst_img); // cv::Mat_<cv::Vec3b>
                                var indexer_dst = mat3_dst.GetIndexer();

                                for (int y = 0, n = 0; y < src_lab.Height; y++)
                                {
                                    for (int x = 0; x < src_lab.Width; x++)
                                    {
                                        n++;
                                        int clusterIdx = clusters.At<int>(n);
                                        Vec3b lab = indexer[y, x];
                                        data[clusterIdx][0] += (lab.Item0 * 100 / 255);
                                        data[clusterIdx][1] += lab.Item1 != 0 ? (lab.Item1 - 128) : 0;
                                        data[clusterIdx][2] += lab.Item2 != 0 ? (lab.Item2 - 128) : 0;
                                        data[clusterIdx][3]++;

                                        Vec3b color;
                                        color.Item0 = (byte)(centers.At<float>(clusterIdx, 0));
                                        color.Item1 = (byte)(centers.At<float>(clusterIdx, 1));
                                        color.Item2 = (byte)(centers.At<float>(clusterIdx, 2));    // R <- B
                                        indexer_dst[y, x] = color;
                                    }
                                }

                                Mat dst_img_bgr = Mat.Zeros(src2.Size(), src2.Type());
                                try
                                {
                                    Cv2.CvtColor(dst_img, dst_img_bgr, ColorConversionCodes.Lab2LBGR);
                                }
                                catch (Exception exce)
                                {
                                    Console.WriteLine("opencv exception: " + exce.Message);
                                    doneEvent.Set();
                                    return;
                                }

                                Cv2.ImWrite(@"C:\gColorFancy\Image\clusters" + clusterCount + ".jpg", dst_img_bgr);

                                for (int j = 0; j < clusterCount; j++)
                                {
                                    var a2 = (double)((double)data[j][1] / data[j][3]);
                                    var b2 = (double)((double)data[j][2] / data[j][3]);
                                    var C2 = imageAnalyzer.calc_C(ref a2, ref b2);
                                    var H2 = imageAnalyzer.calc_H(ref a2, ref b2);
                                    lock (locker)
                                    {
                                        hsvList.Add(new Tuple<double, double, double, double, double, double>((data[j][0] / data[j][3]),
                                            a2, b2, C2, H2, Math.Round((double)(data[j][3] * 100) / (src_lab.Rows * src_lab.Cols), 1)));
                                    }
                                }

                            }
                        }
                    }
                    doneEvent.Set();
                    result = true;
                } catch(Exception e)
                {
                    Console.WriteLine("Exception: " + e.Message);
                    doneEvent.Set();
                    result = false;
                }
            }

            Boolean maskCreate(ref Mat img, ref Mat img_mask, ref Mat img_mask_spc, int avenum, int brightAreaThreshold = -1, int darkAreaThreshold = -1)
            {
                if (img == null | img_mask == null) return false;
                if (img.Width != img_mask.Width | img.Height != img_mask.Height) return false;
                if (mask == null) return false;

                // save original avenum
                int num = mask.GetAveNum();
                mask.SetAveNum(avenum);

                mask.SetSrc(img);
                bool result = mask.Create(out Bitmap m, out Bitmap m2, brightAreaThreshold, darkAreaThreshold);

                // reset to original avenum
                mask.SetAveNum(num);

                if (result)
                {
                    img_mask = BitmapConverter.ToMat(m);
                    if (m2 != null)
                    {
                        img_mask_spc = BitmapConverter.ToMat(m2);
                    }
                }
                else
                {
                    return false;
                }
                return true;
            }
        }



    }

    public class ImageAnalyzer_N3Imager : ImageAnalyzer
    {
        Boundary_N3Imager boundary = new Boundary_N3Imager();
        List<N3ImagerAnalyzer> n3ImagerAnalyzers = new List<N3ImagerAnalyzer>();
        List<ManualResetEvent> doneEvents = new List<ManualResetEvent>();

        public ImageAnalyzer_N3Imager()
        {

        }

        public override bool Get_Description(ref List<Bitmap> imageList, ref List<Bitmap> imgList_background, ref List<Rectangle> maskRect,
            out List<string> descriptions, out List<string> comments, out List<List<N3Results>> n3ResultsListList)
        {
            descriptions = new List<string>();
            comments = new List<string>();

            n3ImagerAnalyzers.Clear();
            doneEvents.Clear();
            for (int i = 0; i < maskRect.Count; i++)
            {
                System.Drawing.Rectangle rect = maskRect[i];
                List<Bitmap> croppedList = new List<Bitmap>();
                List<Bitmap> croppedBGList = new List<Bitmap>();
                for(int m = 0; m < imageList.Count; m++)
                {
                    Bitmap tmp = cropImage(imageList[m], rect);
                    croppedList.Add(tmp);
                }

                for (int m = 0; m < imgList_background.Count; m++)
                {
                    Bitmap tmp = cropImage(imgList_background[m], rect);
                    croppedBGList.Add(tmp);
                }

                // multi threading
                ManualResetEvent resetEvent = new ManualResetEvent(false);
                N3ImagerAnalyzer analyzer = new N3ImagerAnalyzer(croppedList, croppedBGList, resetEvent, ref boundary);
                doneEvents.Add(resetEvent);
                n3ImagerAnalyzers.Add(analyzer);
                ThreadPool.QueueUserWorkItem(analyzer.ThreadPoolCallback, i);
            }

            Console.WriteLine("Analyzing ...");
            WaitHandle.WaitAll(doneEvents.ToArray());
            Console.WriteLine("All calculations are complete.");

            n3ResultsListList = new List<List<N3Results>>();
            for (int i = 0; i < n3ImagerAnalyzers.Count; i++)
            {
                if (n3ImagerAnalyzers[i].result)
                {
                    descriptions.Add(n3ImagerAnalyzers[i].desc);
                    comments.Add(n3ImagerAnalyzers[i].comment);
                    n3ResultsListList.Add(n3ImagerAnalyzers[i].n3ResultsList);
                } else
                {
                    return false;
                }
            }

            return true;
        }

        Bitmap cropImage(Bitmap img, Rectangle cropArea)
        {
            Bitmap bmp = new Bitmap(cropArea.Width, cropArea.Height);
            using (Graphics gph = Graphics.FromImage(bmp))
            {
                gph.DrawImage(img, new Rectangle(0, 0, bmp.Width, bmp.Height), cropArea, GraphicsUnit.Pixel);
            }
            return bmp;
        }

        class N3ImagerAnalyzer
        {
            List<Bitmap> srcList;
            List<Bitmap> bgList;

            Boundary_N3Imager boundary;
            public List<Scalar> hlsList = new List<Scalar>();

            static readonly object locker = new object();
            ManualResetEvent doneEvent;
            public N3ImagerAnalyzer(List<Bitmap>srcList, List<Bitmap> bgList, ManualResetEvent doneEvent, ref Boundary_N3Imager boundary)
            {
                this.srcList = srcList;
                this.bgList = bgList;
                this.doneEvent = doneEvent;
                this.boundary = boundary;
            }

            public bool result = false;
            public string desc;
            public string comment;
            public List<N3Results> n3ResultsList = new List<N3Results>();

            public void ThreadPoolCallback(Object threadContext)
            {
                int threadIndex = (int)threadContext;
                Console.WriteLine("Thread {0} started...", threadIndex);
                try
                {
                    for(int i = 0; i < srcList.Count; i++)
                    {
                        Bitmap s = srcList[i];
                        Mat src = BitmapConverter.ToMat(s);
                        Cv2.ImWrite(@"C:\N3Imager\Image\cropped_image_" + threadIndex.ToString() + "_" + i.ToString() + ".jpg", src);
                        Mat bg = new Mat();
                        if(i < 5)
                        {
                            Bitmap t = bgList[i];
                            bg = BitmapConverter.ToMat(t);
                        }
                        else if(i < 10)
                        {
                            Bitmap t = bgList[3];
                            bg = BitmapConverter.ToMat(t);
                        }
                        else if(i < 13)
                        {
                            Bitmap t = bgList[i-5];
                            bg = BitmapConverter.ToMat(t);
                        } else
                        {
                            Bitmap t = bgList[i-8];
                            bg = BitmapConverter.ToMat(t);
                        }

                        Mat srcFloat = new Mat();
                        src.ConvertTo(srcFloat, MatType.CV_32FC3, 1.0/255);
                        Mat bgFloat = new Mat();
                        bg.ConvertTo(bgFloat, MatType.CV_32FC3, 1.0/255);

                        srcFloat -= bgFloat;
                        srcFloat = srcFloat.Threshold(0, 1.0, ThresholdTypes.Tozero);

                        Mat hls = new Mat();
                        Cv2.CvtColor(srcFloat, hls, ColorConversionCodes.BGR2HLS);

                        Scalar mean;
                        Scalar std;
                        Cv2.MeanStdDev(hls, out mean, out std);
                        mean.Val1 *= 100;
                        mean.Val2 *= 100;
                        Console.WriteLine("average HLS: {0}, {1}, {2} for thread {3}", mean.Val0, mean.Val1, mean.Val2, threadIndex);
                        hlsList.Add(mean);
                    }

                    //analyze hls
                    int indx = 4;
                    for(; indx > 0; indx--)
                    {
                        Scalar hls = hlsList[indx];
                        if(hls.Val1 < 50)
                        {
                            break;
                        }
                    }

                    int indy = 5;
                    for(; indy < 9; indy++)
                    {
                        Scalar hls = hlsList[indy];
                        if (hls.Val1 < 50)
                        {
                            break;
                        }
                    }

                    int indz = 12;
                    for (; indz > 10; indz--)
                    {
                        Scalar hls = hlsList[indz];
                        if (hls.Val1 < 70)
                        {
                            break;
                        }
                    }

                    int indw = 15;
                    for (; indw > 13; indw--)
                    {
                        Scalar hls = hlsList[indw];
                        if (hls.Val1 < 70)
                        {
                            break;
                        }
                    }

                    double[] timeArr = { 0.5, 2, 10, 50, 200 };
                    double[] timeUVArr = { 50, 200, 500 };

                    n3ResultsList.Clear();
                    N3Results fl = new N3Results(hlsList[indx].Val0, hlsList[indx].Val2, hlsList[indx].Val1, timeArr[indx], null, "FL");
                    n3ResultsList.Add(fl);

                    N3Results phos = new N3Results(hlsList[indy].Val0, hlsList[indy].Val2, hlsList[indy].Val1, 0, 
                        new List<double>() { hlsList[5].Val1, hlsList[6].Val1, hlsList[7].Val1, hlsList[8].Val1, hlsList[9].Val1, }, "PHOS");
                    n3ResultsList.Add(phos);

                    N3Results uv = new N3Results(hlsList[indz].Val0, hlsList[indz].Val2, hlsList[indz].Val1, timeUVArr[indz-10], null, "UV");
                    n3ResultsList.Add(uv);

                    N3Results uv2 = new N3Results(hlsList[indw].Val0, hlsList[indw].Val2, hlsList[indw].Val1, timeUVArr[indw - 13], null, "UV2");
                    n3ResultsList.Add(uv2);

                    desc = Natural_Analysis_P3_V2(n3ResultsList, boundary, out comment);

                    doneEvent.Set();
                    result = true;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception: " + e.Message);
                    doneEvent.Set();
                    result = false;
                }
            }

            public string Natural_Analysis_P3_V2(List<N3Results> n3ResultsList, Boundary_N3Imager boundary, out string comment)
            {
                string res = "";
                comment = "";

                double FL_H = n3ResultsList[0].h;
                double FL_S = n3ResultsList[0].s;
                double FL_L = n3ResultsList[0].l;
                double FL_time = n3ResultsList[0].time;

                double PHOS_H = n3ResultsList[1].h;
                double PHOS_L = n3ResultsList[1].l;
                double PHOS_S = n3ResultsList[1].s;
                List<double> PHOS_delay = n3ResultsList[1].delays;

                double UVFL_H = n3ResultsList[2].h;
                double UVFL_S = n3ResultsList[2].s;
                double UVFL_L = n3ResultsList[2].l;
                double UVFL_time = n3ResultsList[2].time;

                double UVFL2_H = n3ResultsList[3].h;
                double UVFL2_S = n3ResultsList[3].s;
                double UVFL2_L = n3ResultsList[3].l;
                double UVFL2_time = n3ResultsList[3].time;

                double L_delay = PHOS_delay[4] / PHOS_delay[0];
                double M_delay = PHOS_delay[2] / PHOS_delay[1];
                double S_delay = PHOS_delay[1] / PHOS_delay[0];

                int Cond_id = 0;
                int output = 0;

                // phos 
                int Phos_color = 0;
                List<List<string>> PHOS_Bound_num = boundary.N3PHOS_data;

                for (; Cond_id < PHOS_Bound_num.Count; Cond_id++)
                {
                    List<string> PHOSBound = PHOS_Bound_num[Cond_id];

                    if (PHOS_L >= double.Parse(PHOSBound[0]) && PHOS_L < double.Parse(PHOSBound[1])
                        && PHOS_H >= double.Parse(PHOSBound[2]) && PHOS_H < double.Parse(PHOSBound[3])
                        && PHOS_S >= double.Parse(PHOSBound[4]) && PHOS_S < double.Parse(PHOSBound[5])
                        && L_delay >= double.Parse(PHOSBound[6]) && L_delay < double.Parse(PHOSBound[7])
                        && M_delay >= double.Parse(PHOSBound[8]) && M_delay < double.Parse(PHOSBound[9])
                        && S_delay >= double.Parse(PHOSBound[10]) && S_delay < double.Parse(PHOSBound[11])
                        && PHOS_delay[0] >= double.Parse(PHOSBound[12]) && PHOS_delay[0] < double.Parse(PHOSBound[13])
                        && PHOS_delay[2] >= double.Parse(PHOSBound[14]) && PHOS_delay[2] < double.Parse(PHOSBound[15])
                        && PHOS_delay[4] >= double.Parse(PHOSBound[16]) && PHOS_delay[4] < double.Parse(PHOSBound[17]))
                    {
                        Phos_color = int.Parse(PHOSBound[18]);
                        output = int.Parse(PHOSBound[19]);
                        break;
                    }
                }

                // fl
                int FL_color = 0;
                Cond_id = 0;
                List<List<string>> FL_Bound_num = boundary.N3FL_data;

                for(; Cond_id < FL_Bound_num.Count; Cond_id++)
                {
                    List<string> FLBound = FL_Bound_num[Cond_id];
                    if (FL_time >= double.Parse(FLBound[0]) && FL_time < double.Parse(FLBound[1])
                        && FL_L >= double.Parse(FLBound[2]) && FL_L < double.Parse(FLBound[3])
                        && FL_H >= double.Parse(FLBound[4]) && FL_H < double.Parse(FLBound[5])
                        && FL_S >= double.Parse(FLBound[6]) && FL_S < double.Parse(FLBound[7]))
                    {
                        FL_color = int.Parse(FLBound[8]);
                        break;
                    }
                }

                if ((FL_color == 1 || FL_color == 3 || FL_color == 4) && PHOS_delay[4] < 10 && (output == 0 || output == 7))
                {
                    output = 1; //based on FL brightness
                }

                if (output == 0 || output == 1 || output == 7) // deep UV analysis
                {
                    int UVFL_color = 0;
                    Cond_id = 0;
                    List<List<string>> UVFL_Bound_num = boundary.N3UVFL_data;
                    for(; Cond_id < UVFL_Bound_num.Count; Cond_id++)
                    {
                        List<string> UVBound = UVFL_Bound_num[Cond_id];

                        if (FL_time >= double.Parse(UVBound[0]) && FL_time < double.Parse(UVBound[1])
                            && UVFL_time >= double.Parse(UVBound[2]) && UVFL_time < double.Parse(UVBound[3])
                            && UVFL_L >= double.Parse(UVBound[4]) && UVFL_L < double.Parse(UVBound[5])
                            && UVFL_H >= double.Parse(UVBound[6]) && UVFL_H < double.Parse(UVBound[7])
                            && UVFL_S >= double.Parse(UVBound[8]) && UVFL_S < double.Parse(UVBound[9])
                            && Phos_color >= double.Parse(UVBound[10]) && Phos_color < double.Parse(UVBound[11])
                            && UVFL2_time >= double.Parse(UVBound[12]) && UVFL2_time < double.Parse(UVBound[13])
                            && UVFL2_L >= double.Parse(UVBound[14]) && UVFL2_L < double.Parse(UVBound[15])
                            && UVFL2_H >= double.Parse(UVBound[16]) && UVFL2_H < double.Parse(UVBound[17])
                            && UVFL2_S >= double.Parse(UVBound[18]) && UVFL2_S < double.Parse(UVBound[19])
                            && (output != double.Parse(UVBound[20])))
                        {
                            UVFL_color = int.Parse(UVBound[21]);
                            output = int.Parse(UVBound[22]);

                            break;
                        }
                    }
                }

                if (output == 0) // still undetermined
                    output = 2; // refer

                switch (output)
                {
                    case 1:
                        res = "Natural Diamond";
                        comment = "Natural";
                        break;
                    case 2:
                        res = "REFER";
                        comment = "Refer";
                        break;
                    case 3:
                        res = "HPHT Synthetic";
                        comment = "HPHT";
                        break;
                    case 4:
                        res = "CVD synthetic";
                        comment = "CVD";
                        break;
                    case 5:
                        res = "CZ";
                        comment = "CZ";
                        break;
                    case 6:
                        res = "Natural Diamond";
                        comment = "Natural(DeepUV)";
                        break;
                    case 7:
                        res = "Natural Diamond";
                        comment = "Natural(shortPHOS)";
                        break;
                    default:
                        res = "Error";
                        comment = "Error";
                        break;
                }

                return res;
            }
        }
    }

    public class ImageAnalyzer_Pearl : ImageAnalyzer
    {
        List<PearlAnalyzer> pearlAnalyzers = new List<PearlAnalyzer>();
        List<ManualResetEvent> doneEvents = new List<ManualResetEvent>();

        public ImageAnalyzer_Pearl()
        {

        }

        public override bool Get_Description(ref List<Bitmap> imageList, ref List<Bitmap> imgList_background, ref List<Rectangle> maskRect,
            out List<string> descriptions, out List<string> comments, out List<List<N3Results>> n3ResultsListList)
        {
            Console.WriteLine("PearlAnalyzer: not implemented");
            descriptions = new List<string>() { "Pearl: No implementation" };
            comments = new List<string>() { "Pearl: No implementation" };
            n3ResultsListList = null;
            return false;
        }

        public override void check_pearl_max_lumi(ref Bitmap image, ref string comment, out double maxValue, out double shift)
        {
            Mat srcImg = BitmapConverter.ToMat(image);
            Mat src_gray = new Mat();
            Cv2.CvtColor(src, src_gray, ColorConversionCodes.BGR2GRAY);

            Cv2.MinMaxLoc(src_gray, out double min, out maxValue);

            Rect roi = new Rect(0, 0, 50, 50);
            Mat roiImg = new Mat(src_gray, roi);
            Scalar mean, std;
            Cv2.MeanStdDev(roiImg, out mean, out std);
            shift = mean.Val0;
        }

        public override void CalcPearlLusterWidthHeight(List<System.Drawing.Bitmap> imgList, ref double shiftValue, ref double maxValue, out double w, out double h)
        {
            double threshold = (maxValue - shiftValue) * 0.5 + shiftValue;
            Mat src = BitmapConverter.ToMat(imgList[0]);
            Mat src_gray = new Mat();
            Cv2.CvtColor(src, src_gray, ColorConversionCodes.BGR2GRAY);

            Mat img_mask = new Mat(new OpenCvSharp.Size(src_gray.Width, src_gray.Height), MatType.CV_8UC1, 0);
            Cv2.Threshold(src_gray, img_mask, threshold, 255, ThresholdTypes.Binary);

            // caluculate width
            Mat resRow = new Mat(new OpenCvSharp.Size(img_mask.Width, 1), MatType.CV_32FC1, 0);
            Cv2.Reduce(img_mask, resRow, ReduceDimension.Row, ReduceTypes.Sum, MatType.CV_32FC1);
            int init = 0;
            for (int i = 0; i < resRow.Cols; i++)
            {
                float e = resRow.Get<float>(0, i);
                if (e > 0)
                {
                    init = i;
                    break;
                }
            }

            int end = 0;
            for (int i = resRow.Cols - 1; i >= 0; i--)
            {
                float e = resRow.Get<float>(0, i);
                if (e > 0)
                {
                    end = i;
                    break;
                }
            }

            w = end - init;

            // calculate height
            Mat resCol = new Mat(new OpenCvSharp.Size(1, img_mask.Height), MatType.CV_32FC1, 0);
            Cv2.Reduce(img_mask, resCol, ReduceDimension.Column, ReduceTypes.Sum, MatType.CV_32FC1);
            init = 0;
            for (int i = 0; i < resCol.Rows; i++)
            {
                float e = resCol.Get<float>(i, 0);
                if (e > 0)
                {
                    init = i;
                    break;
                }
            }

            end = 0;
            for (int i = resCol.Rows - 1; i >= 0; i--)
            {
                float e = resCol.Get<float>(i, 0);
                if (e > 0)
                {
                    end = i;
                    break;
                }
            }
            h = end - init;
        }

        public override bool Get_Description(ref List<Bitmap> imageList, ref List<Bitmap> imgList_background, ref List<Rectangle> maskRect,
            out List<List<double>> descriptions, out List<string> comments)
        {
            bool res = false;
            comments = new List<string>();
            descriptions = new List<List<double>>();
            try
            {
                CalcPearlBodyColor(ref imageList, ref imgList_background, out descriptions);
                res = true;
            }
            catch(Exception ex)
            {
                Console.WriteLine("error: " + ex.Message);
            }
            return res;
        }

        void CalcPearlBodyColor(ref List<System.Drawing.Bitmap> imgList, ref List<System.Drawing.Bitmap> bgImgList, out List<List<double>> colorsList)
        {
            colorsList = new List<List<double>>();

            if (MultiThreading)
            {
                pearlAnalyzers.Clear();
                doneEvents.Clear();
                // todo: multi threading
                for (int m = 0; m < imgList.Count; m++)
                {
                    Bitmap image = imgList[m];
                    Bitmap image_background = bgImgList == null ? null : bgImgList[m];

                    ManualResetEvent resetEvent = new ManualResetEvent(false);
                    PearlAnalyzer analyzer = new PearlAnalyzer(this, resetEvent, ref image, ref image_background);

                    doneEvents.Add(resetEvent);
                    pearlAnalyzers.Add(analyzer);
                    ThreadPool.QueueUserWorkItem(analyzer.ThreadPoolCallback, m);
                }
                Console.WriteLine("Analyzing ...");
                WaitHandle.WaitAll(doneEvents.ToArray());
                Console.WriteLine("All calculations are complete.");
                for (int m = 0; m < pearlAnalyzers.Count; m++)
                {
                    PearlAnalyzer analyzer = pearlAnalyzers[m];

                }
            }
        }

        class PearlAnalyzer
        {
            public PearlAnalyzer(ImageAnalyzer_Pearl imageAnalyzer, ManualResetEvent doneEvent, ref Bitmap img_Bmp_diamond, ref Bitmap img_Bmp_background)
            {
                this.imageAnalyzer = imageAnalyzer;
                this.doneEvent = doneEvent;
                this.img_Bmp_diamond = img_Bmp_diamond;
                this.img_Bmp_background = img_Bmp_background;
                mask = new NewMask();
            }

            static readonly object locker = new object();
            ImageAnalyzer_Pearl imageAnalyzer;
            ManualResetEvent doneEvent;
            Mask mask;
            Bitmap img_Bmp_diamond;
            Bitmap img_Bmp_background;

            public double mask_length = 0;
            public double mask_area = 0;

            public bool result = false;
            public List<double> colors = new List<double>();

            public void ThreadPoolCallback(Object threadContext)
            {
                int threadIndex = (int)threadContext;
                Console.WriteLine("Thread {0} started...", threadIndex);
                try
                {
                    if (img_Bmp_diamond == null | img_Bmp_background == null)
                    {
                        doneEvent.Set();
                        return;
                    }
                    if (img_Bmp_diamond.Width != img_Bmp_background.Width | img_Bmp_diamond.Height != img_Bmp_background.Height)
                    {
                        doneEvent.Set();
                        return;
                    }
                    //// Bitmap -> IplImage
                    Mat img_diamond;
                    Mat img_background;
                    Mat img_mask;
                    Mat img_mask2 = null;
                    img_diamond = BitmapConverter.ToMat(img_Bmp_diamond);
                    img_background = BitmapConverter.ToMat(img_Bmp_background);
                    img_mask = new Mat(new OpenCvSharp.Size(img_diamond.Width, img_diamond.Height), MatType.CV_8UC1, 0);

                    //// Create software mask
                    //Cv2.Zero(img_mask);
                    // TODO: maskCreate using modified settings, need to fix
                    if (maskCreate(ref img_diamond, ref img_mask, ref img_mask2, 3) == false)
                    {
                        doneEvent.Set();
                        return;
                    }

                    //Mat mat_mask = new Mat(img_mask);
                    Mat mat_mask = img_mask.Clone();

                    if (mask.GetMaskType() == MASK_TYPE.MASK_NEW)
                    {
                        mask_area = Cv2.CountNonZero(mat_mask);
                    }

                    Mat img_Lab_diamond = new Mat(new OpenCvSharp.Size(img_diamond.Width, img_diamond.Height), MatType.CV_8UC3, 0);
                    Mat img_Lab_background = new Mat(new OpenCvSharp.Size(img_background.Width, img_background.Height), MatType.CV_8UC3, 3);

                    Mat srcFloat = new Mat();
                    img_Lab_diamond.ConvertTo(srcFloat, MatType.CV_32FC3, 1.0 / 255);

                    Mat hls = new Mat();
                    Cv2.CvtColor(srcFloat, hls, ColorConversionCodes.BGR2HLS);

                    Mat hue, ch2, ch3;
                    // "channels" is a vector of 3 Mat arrays:
                    
                    // split img:
                    Mat[] channels = Cv2.Split(hls);
                    // get the channels (dont forget they follow BGR order in OpenCV)
                    hue = channels[0];
                    ch2 = channels[1];
                    ch3 = channels[2];

                    Mat samples = new Mat(new OpenCvSharp.Size(1, mask_area), MatType.CV_32FC2, 0);
                    Mat cos = new Mat(new OpenCvSharp.Size(1, mask_area), MatType.CV_32FC1, 0);
                    Mat sin = new Mat(new OpenCvSharp.Size(1, mask_area), MatType.CV_32FC1, 0);
                    int m = 0;
                    for(int i = 0; i < mat_mask.Cols; i++)
                    {
                        for(int j = 0; j < mat_mask.Rows; j++)
                        {
                            byte v = mat_mask.Get<byte>(j, i);
                            if(v != 0)
                            {
                                float h = (float)(hue.Get<float>(j, i) * 3.1415926 / 180);
                                float cosV = (float)Math.Cos(h);
                                float sinV = (float)Math.Sin(h);
                                cos.Set<float>(1, m, cosV);
                                sin.Set<float>(1, m, sinV);
                                m++;
                            }
                        }
                    }

                    List<Mat> mList = new List<Mat>() { cos, sin };
                    Cv2.Merge(mList.ToArray(), samples);

                    var bestLabels = new Mat();
                    var centers = new Mat();

                    Cv2.Kmeans(samples, 3, bestLabels, new TermCriteria(CriteriaType.Eps | CriteriaType.MaxIter, 10, 1.0), 3, KMeansFlags.PpCenters, centers);

                    doneEvent.Set();
                    result = true;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception: " + e.Message);
                    doneEvent.Set();
                    result = false;
                }
            }

            Boolean maskCreate(ref Mat img, ref Mat img_mask, ref Mat img_mask_spc, int avenum, int brightAreaThreshold = -1, int darkAreaThreshold = -1)
            {
                if (img == null | img_mask == null) return false;
                if (img.Width != img_mask.Width | img.Height != img_mask.Height) return false;
                if (mask == null) return false;

                // save original avenum
                int num = mask.GetAveNum();
                mask.SetAveNum(avenum);

                mask.SetSrc(img);
                bool result = mask.Create(out Bitmap m, out Bitmap m2, brightAreaThreshold, darkAreaThreshold);

                // reset to original avenum
                mask.SetAveNum(num);

                if (result)
                {
                    img_mask = BitmapConverter.ToMat(m);
                    if (m2 != null)
                    {
                        img_mask_spc = BitmapConverter.ToMat(m2);
                    }
                }
                else
                {
                    return false;
                }
                return true;
            }
        }
    }
}
