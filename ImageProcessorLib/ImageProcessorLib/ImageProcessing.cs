using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic.FileIO;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace ImageProcessorLib
{
    public enum LineType
    {
        Horizontal = 0,
        Vertical
    }

    public static class CamCalibrationUtil
    {

        public static Boolean calcBGR_wholeimage(ref Bitmap img_Bmp, ref double B, ref double G, ref double R)
        {
            if (img_Bmp == null) return false;

            //// Bitmap -> IplImage
            Mat img;
            img = BitmapConverter.ToMat(img_Bmp);

            // Calculate Ave and Sdv of BGR
            Scalar mean;
            Scalar std_dev;
            Cv2.MeanStdDev(img, out mean, out std_dev);
            B = mean.Val0;
            G = mean.Val1;
            R = mean.Val2;

            //Cv.ReleaseImage(img);
            return true;
        }
    }

    public static class ImageProcessing
    {
        static byte[] lut = new byte[256];
        static bool _initLUT = false;
        static double _gamma = 1.5, _fmin = 5, _fmax = 200, _gmin = 0, _gmax = 255;

        //static ArrayList _ColorGrade = new ArrayList();

        //static string _fileName = "Ini.txt";
        //static string _deviceName = "CV";

        //// Mask parameter
        ////static int _avenum = 1;
        ////static int _contrast = 30;
        ////static int _erode = 1;
        //static MASK_TYPE _mask_type = MASK_TYPE.MASK_NEW;
        //static Mask _mask = null;

        //static DIAMOND_GROUPING _diamond_group = DIAMOND_GROUPING.RBC;

        //// Diamond grouping parameter
        //static double _widthratio_RBC = 1.1;
        //static double _aspect_min_RBC = 0.58; //0.58;
        //static double _aspect_max_RBC = 0.76;

        //static double _widthratio_Fancy_L = 1.35;
        //static double _widthratio_Fancy_H = 1.70;

        //static double _aspect_min_FancyL = 0.44; //0.44;
        //static double _aspect_max_FancyL = 0.7; //0.7;

        //static double _aspect_min_FancyH = 0.34; //0.34;
        //static double _aspect_max_FancyH = 0.7; //0.7;

        //static double _aspect_min_FancyHH = 0.24; //0.24;
        //static double _aspect_max_FancyHH = 0.5; //0.5;

        //// Boundary shifting
        ////static double _shiftC_RBC = 0.0;
        ////static double _shiftC_FancyL = 0.1;
        ////static double _shiftC_FancyH = 0.1;
        ////static double _shiftC_FancyHH = 0.3;

        //// Calibration parameter
        //static double _shift_L = 0.0, _shift_a = 0.0, _shift_b = 0.0;
        //static double _conv_L = 1.0, _conv_a = 1.0, _conv_b = 1.0;

        //// Fluorescence parameter for Chroma and Hue calculation
        //static double _darkLevel = 3.0;

        //static ImageProcessing()
        //{
        //    Read_file();
        //    instantiateMask();
        //}

        //private static void instantiateMask()
        //{
        //    switch (_mask_type)
        //    {
        //        case MASK_TYPE.MASK_DEFAULT:
        //            _mask = new Mask();
        //            break;
        //        case MASK_TYPE.MASK_MELEE:
        //            _mask = new MeleeMask();
        //            break;
        //        case MASK_TYPE.MASK_NEW:
        //            _mask = new NewMask();
        //            break;
        //    }
        //}

        //private static void Read_file()
        //{
        //    //int i, j;
        //    string currentDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
        //    _fileName = currentDirectory + "\\" + _fileName;

        //    if (System.IO.File.Exists(_fileName))
        //    {
        //        TextFieldParser parser = new TextFieldParser(_fileName, System.Text.Encoding.GetEncoding("Shift_JIS"));
        //        parser.TextFieldType = FieldType.Delimited;
        //        parser.SetDelimiters(",");

        //        string[] column = parser.ReadFields();

        //        // 1st line: Mask parameter erode, average, contrast
        //        //_erode = Convert.ToInt16(column[1]);
        //        //_avenum = Convert.ToInt16(column[2]);
        //        //_contrast = Convert.ToInt16(column[3]);
        //        _mask_type = (MASK_TYPE)Convert.ToInt16(column[1]);

        //        //2nd line: Grouping parameter of RBC
        //        column = parser.ReadFields();
        //        _widthratio_RBC = Convert.ToDouble(column[1]);
        //        _aspect_min_RBC = Convert.ToDouble(column[2]);
        //        _aspect_max_RBC = Convert.ToDouble(column[3]);

        //        //3rd line: Grouping parameter of Fancy shape diamond
        //        column = parser.ReadFields();
        //        _widthratio_Fancy_L = Convert.ToDouble(column[1]);
        //        _widthratio_Fancy_H = Convert.ToDouble(column[2]);
        //        _aspect_min_FancyL = Convert.ToDouble(column[3]);
        //        _aspect_min_FancyH = Convert.ToDouble(column[4]);
        //        _aspect_min_FancyHH = Convert.ToDouble(column[5]);

        //        //4th line: Boundary parameter
        //        //column = parser.ReadFields();
        //        //_shiftC_RBC = Convert.ToDouble(column[1]);
        //        //_shiftC_FancyL = Convert.ToDouble(column[2]);
        //        //_shiftC_FancyH = Convert.ToDouble(column[3]);
        //        //_shiftC_FancyHH = Convert.ToDouble(column[4]);

        //        //4th line: Device parameter
        //        column = parser.ReadFields();
        //        _deviceName = column[1].ToString();

        //        try
        //        {
        //            //5th line: FL parameter
        //            column = parser.ReadFields();
        //            _darkLevel = Convert.ToDouble(column[1]);
        //        }
        //        catch
        //        {
        //        }

        //    }
        //}
        //public static void setLabAdjustment(double Conv_L, double Conv_a, double Conv_b, double Shift_L, double Shift_a, double Shift_b)
        //{
        //    _shift_L = Shift_L;
        //    _shift_a = Shift_a;
        //    _shift_b = Shift_b;
        //    _conv_L = Conv_L;
        //    _conv_a = Conv_a;
        //    _conv_b = Conv_b;
        //}
        //public static void setConvL(double Conv_L)
        //{
        //    _conv_L = Conv_L;
        //}
        //public static void setConvA(double Conv_a)
        //{
        //    _conv_a = Conv_a;
        //}
        //public static void setConvB(double Conv_b)
        //{
        //    _conv_b = Conv_b;
        //}
        //public static void setShiftL(double Shift_L)
        //{
        //    _shift_L = Shift_L;
        //}
        //public static void setShiftA(double Shift_a)
        //{
        //    _shift_a = Shift_a;
        //}
        //public static void setShiftB(double Shift_b)
        //{
        //    _shift_b = Shift_b;
        //}

        //public static string getDevicename()
        //{
        //    return _deviceName;
        //}

        //private static string getColorGrade(double L, double C, double H)
        //{
        //    int i;
        //    string str = "Go to visual grade";
        //    double Lmin, Lmax, Cmin, Cmax, Hmin, Hmax;
        //    //Should use different table depending on the Hue range

        //    for (i = 0; i < _ColorGrade.Count; i++)
        //    {
        //        Lmin = double.Parse(((string[])_ColorGrade[i])[1]);
        //        Lmax = double.Parse(((string[])_ColorGrade[i])[2]);
        //        Cmin = double.Parse(((string[])_ColorGrade[i])[3]);
        //        Cmax = double.Parse(((string[])_ColorGrade[i])[4]);
        //        Hmin = double.Parse(((string[])_ColorGrade[i])[5]);
        //        Hmax = double.Parse(((string[])_ColorGrade[i])[6]);

        //        if (Lmin <= L && L <= Lmax && Cmin < C && C <= Cmax && Hmin <= H && H <= Hmax)
        //        {
        //            str = ((string[])_ColorGrade[i])[0];
        //            break;
        //        }
        //    }

        //    return str;
        //}

        //private static void getColorGrade_description(double L, double C, double H, ref string L_description, ref string C_description, ref string H_description, ref string comment)
        //{
        //    int i;
        //    double Lmin, Lmax, Cmin, Cmax, Hmin, Hmax;
        //    double delta_C;

        //    L_description = "";
        //    C_description = "";
        //    H_description = "";
        //    comment = "";

        //    for (i = 0; i < _ColorGrade.Count; i++)
        //    {
        //        Lmin = double.Parse(((string[])_ColorGrade[i])[1]);
        //        Lmax = double.Parse(((string[])_ColorGrade[i])[2]);
        //        Cmin = double.Parse(((string[])_ColorGrade[i])[3]);
        //        Cmax = double.Parse(((string[])_ColorGrade[i])[4]);
        //        Hmin = double.Parse(((string[])_ColorGrade[i])[5]);
        //        Hmax = double.Parse(((string[])_ColorGrade[i])[6]);

        //        if (Cmin < C && C <= Cmax && Hmin < H && H <= Hmax)
        //        {
        //            //Color grade
        //            C_description = ((string[])_ColorGrade[i])[0];

        //            delta_C = (Cmax - Cmin) / 3.0;
        //            if (C < (Cmin + delta_C)) C_description += "+";
        //            if (C > (Cmin+2.0*delta_C)) C_description +="-";
        //            if (Math.Abs(C - Cmin) < 0.03 || Math.Abs(C - Cmax) < 0.03) comment += "-Chroma boundary-"; 

        //            //Hue description
        //            H_description = ((string[])_ColorGrade[i])[7];

        //            //if (Math.Abs(H - Hmin) < 2 || Math.Abs(H - Hmax) < 2) comment += "-Hue boundary-"; 

        //            //Tone description
        //            if (L <= Lmin)
        //            {
        //                L_description = "Low tone";
        //                //C_description = "VIS";
        //                C_description += "(VIS)";
        //            }
        //            else if (L > Lmax)
        //            {
        //                L_description = "High tone";
        //                C_description += "(VIS)";
        //            }

        //            break;
        //        }
        //        else if (Hmin < H && H <= Hmax)
        //        {
        //            if (Math.Abs(H - Hmin) < 2 || Math.Abs(H - Hmax) < 2) comment = "-Hue boundary-"; 

        //            H_description = ((string[])_ColorGrade[i])[7];
        //            C_description = "Fancy";
        //        }

        //    }


        //}
        //public static Boolean check_diamond_exist(ref Bitmap img_Bmp_diamond, ref string comment)
        //{
        //    //// Bitmap -> IplImage
        //    IplImage img_diamond;
        //    IplImage img_mask;
        //    img_diamond = BitmapConverter.ToIplImage(img_Bmp_diamond);
        //    img_mask = Cv.CreateImage(new CvSize(img_diamond.Width, img_diamond.Height), BitDepth.U8, 1);

        //    //// Create software mask
        //    Cv.Zero(img_mask);

        //    _diamond_group = DIAMOND_GROUPING.Default;

        //    if (maskCreate(ref img_diamond, ref img_mask, ref _mask_type) == false)
        //    {
        //        Cv.ReleaseImage(img_diamond);
        //        Cv.ReleaseImage(img_mask);
        //        comment = "Image processing error";
        //        return false;
        //    }

        //    Cv.ReleaseImage(img_diamond);
        //    Cv.ReleaseImage(img_mask);

        //    if (_mask.Length < 30)
        //    {
        //        comment = "No diamond.";
        //        return false;
        //    }
        //    else
        //    {
        //        return true;
        //    }
        //}


        //static bool check_object_touch_border(Mat object_mask)
        //{
        //    Mat border = Mat.Zeros(object_mask.Size(), object_mask.Type());
        //    Cv2.Rectangle(border, new OpenCvSharp.CPlusPlus.Point(10, 10),
        //        new OpenCvSharp.CPlusPlus.Point(object_mask.Width - 10, object_mask.Height - 10),
        //       new Scalar(255, 255, 255, 255), 5);

        //    Mat intersectionMat = Mat.Zeros(object_mask.Size(), object_mask.Type());
        //    Cv2.BitwiseAnd(object_mask, border, intersectionMat);
        //    //Cv2.ImWrite(@"P:\Temp\gUV_Test\intersection.jpg", intersectionMat);
        //    Scalar intersectionArea = Cv2.Sum(intersectionMat);

        //    return intersectionArea[0] > 0;
        //}

        //public static Boolean check_diamond_position(ref Bitmap img_Bmp_diamond, int x, int y, ref string comment)
        //{
        //    //// Bitmap -> IplImage
        //    IplImage img_diamond;
        //    IplImage img_mask;
        //    img_diamond = BitmapConverter.ToIplImage(img_Bmp_diamond);
        //    img_mask = Cv.CreateImage(new CvSize(img_diamond.Width, img_diamond.Height), BitDepth.U8, 1);

        //    //// Create software mask
        //    Cv.Zero(img_mask);

        //    _diamond_group = DIAMOND_GROUPING.Default;

        //    if (maskCreate(ref img_diamond, ref img_mask, ref _mask_type) == false)
        //    {
        //        Cv.ReleaseImage(img_diamond);
        //        Cv.ReleaseImage(img_mask);
        //        comment = "Image processing error";
        //        return false;
        //    }

        //    if (_mask_type == MASK_TYPE.MASK_NEW)
        //    {
        //        //img_mask.SaveImage(@"P:\temp\gUV_test\img_mask_mat.jpg");
        //        Mat img_mask_mat = new Mat(img_mask);
        //        bool object_touches_boundary = check_object_touch_border(img_mask_mat);

        //        Color centerColor = BitmapConverter.ToBitmap(img_mask).GetPixel(x, y);
        //        bool centered = (centerColor.R != 0) || (centerColor.G != 0) || (centerColor.B != 0);

        //        Cv.ReleaseImage(img_diamond);
        //        Cv.ReleaseImage(img_mask);

        //        if (_mask.Area == 0)
        //        {
        //            comment = "No object detected.";
        //            return false;
        //        }
        //        else if (centered == false || object_touches_boundary == true)
        //        {
        //            comment = "Check diamond position.";
        //            return false;
        //        }
        //    }
        //    else
        //    {
        //        IntPtr ptr;
        //        Byte pixel;

        //        ptr = img_mask.ImageData;
        //        pixel = Marshal.ReadByte(ptr, (img_mask.WidthStep * y) + (x));

        //        Cv.ReleaseImage(img_diamond);
        //        Cv.ReleaseImage(img_mask);

        //        if (pixel == 0)
        //        {
        //            comment = "Check diamond position.";
        //            return false;
        //        }
        //    }

        //    return true;
        //}


        //public static Boolean check_diamond_centered(ref Bitmap img_Bmp_diamond, int x, int y, ref string comment, int maxDistance)
        //{
        //    //// Bitmap -> IplImage
        //    IplImage img_diamond;
        //    IplImage img_mask;
        //    img_diamond = BitmapConverter.ToIplImage(img_Bmp_diamond);
        //    img_mask = Cv.CreateImage(new CvSize(img_diamond.Width, img_diamond.Height), BitDepth.U8, 1);

        //    //// Create software mask
        //    Cv.Zero(img_mask);

        //    _diamond_group = DIAMOND_GROUPING.Default;

        //    try
        //    {
        //        if (maskCreate(ref img_diamond, ref img_mask, ref _mask_type) == false)
        //        {
        //            comment = "Image processing error";
        //            return false;
        //        }

        //        if (_mask_type == MASK_TYPE.MASK_NEW)
        //        {
        //            Mat img_mask_mat = new Mat(img_mask);
        //            //img_mask_mat.SaveImage(@"P:\temp\img_mask_mat.jpg");
        //            Moments m = Cv2.Moments(img_mask_mat);
        //            Point2d center = new Point2d(m.M10 / m.M00, m.M01 / m.M00);

        //            double euclidDistanceFromCenter = Math.Sqrt(((x - center.X) * (x - center.X)) + ((y - center.Y) * (y - center.Y)));

        //            if (_mask.Area == 0)
        //            {
        //                comment = "No object detected.";
        //                return false;
        //            }
        //            else if (euclidDistanceFromCenter > maxDistance)
        //            {
        //                comment = "Check diamond position.";
        //                return false;
        //            }

        //        }
        //        else
        //        {
        //            comment = "Bad mask setting.";
        //            return false;
        //        }
        //    }
        //    finally
        //    {
        //        Cv.ReleaseImage(img_diamond);
        //        Cv.ReleaseImage(img_mask);
        //    }

        //    return true;
        //}


        //public static Boolean check_NO_measurementDust(ref Bitmap img_Bmp_diamond, ref string message)
        //{
        //    try
        //    {
        //        double mask_length = 0;
        //        double mask_length2 = 0;

        //        IplImage img_diamond;
        //        IplImage img_mask;

        //        img_diamond = BitmapConverter.ToIplImage(img_Bmp_diamond);
        //        img_mask = Cv.CreateImage(new CvSize(img_diamond.Width, img_diamond.Height), BitDepth.U8, 1);
        //        Cv.Zero(img_mask);

        //        if (maskCreate(ref img_diamond, ref img_mask, ref _mask_type) == false)
        //        {
        //            message = "Unexpected error.";
        //            Cv.ReleaseImage(img_diamond);
        //            Cv.ReleaseImage(img_mask);
        //            return false;
        //        }

        //        // get the length
        //        mask_length = _mask.Length;

        //        IplImage img_diamond2;
        //        IplImage img_mask2;

        //        img_diamond2 = BitmapConverter.ToIplImage(img_Bmp_diamond);
        //        img_mask2 = Cv.CreateImage(new CvSize(img_diamond.Width, img_diamond.Height), BitDepth.U8, 1);
        //        Cv.Zero(img_mask2);

        //        if (maskCreate(ref img_diamond2, ref img_mask2, ref _mask_type, 3) == false)
        //        {
        //            message = "Unexpected error.";
        //            Cv.ReleaseImage(img_diamond2);
        //            Cv.ReleaseImage(img_mask2);
        //            return false;
        //        }

        //        // get the length
        //        mask_length2 = _mask.Length;

        //        Cv.ReleaseImage(img_diamond);
        //        Cv.ReleaseImage(img_mask);
        //        Cv.ReleaseImage(img_diamond2);
        //        Cv.ReleaseImage(img_mask2);

        //        if (Math.Abs(mask_length - mask_length2) / mask_length > 0.07)
        //        {
        //            message = "Check dust in the image.";
        //            return false;
        //        }

        //    }
        //    catch (Exception exc)
        //    {
        //        message = "Unexpected error.";
        //        return false;
        //    }

        //    return true;
        //}

        //public static Boolean check_NO_backgroundDust(ref List<Bitmap> imgList_background, ref string message, bool check_lightness = true)
        //{
        //    try
        //    {
        //        foreach (Bitmap bm in imgList_background)
        //        {
        //            double L = 0, a = 0, b = 0;
        //            Bitmap bm_copy = bm;
        //            IplImage img_background;
        //            IplImage img_mask;
        //            img_background = BitmapConverter.ToIplImage(bm);
        //            img_mask = Cv.CreateImage(new CvSize(img_background.Width, img_background.Height), BitDepth.U8, 1);

        //            Cv.Zero(img_mask);

        //            // Check dust on the stage
        //            if (maskCreate(ref img_background, ref img_mask, ref _mask_type))
        //            {
        //                Cv.ReleaseImage(img_background);
        //                Cv.ReleaseImage(img_mask);
        //                message = "Check dust on stage.";
        //                return false;
        //            }
        //            else
        //            {
        //                if (_mask.Area > 0)
        //                {
        //                    Cv.ReleaseImage(img_background);
        //                    Cv.ReleaseImage(img_mask);
        //                    message = "Check dust on stage.";
        //                    return false;
        //                }
        //            }

        //            // Check brightness of image
        //            if (check_lightness == true)
        //            {
        //                calcLab_wholeimage(ref bm_copy, ref L, ref a, ref b);
        //                if (L < 88)
        //                {
        //                    Cv.ReleaseImage(img_background);
        //                    Cv.ReleaseImage(img_mask);
        //                    message = "Brightness is low.";
        //                    return false;
        //                }
        //            }
        //            Cv.ReleaseImage(img_background);
        //            Cv.ReleaseImage(img_mask);
        //        }
        //    }
        //    catch (Exception exc)
        //    {
        //        message = "Unexpected error. Check dust on stage.";
        //        return false;
        //    }

        //    return true;
        //}


        //private static Boolean calcLab_diamond_background_all(ref Bitmap img_Bmp_diamond,
        //    ref Bitmap img_Bmp_background, ref double L_diamond, ref double a_diamond,
        //    ref double b_diamond, ref double L_background, ref double a_background,
        //    ref double b_background, ref double L, ref double a, ref double b,
        //    ref double mask_length, ref double mask_area, ref double mask_width, ref double mask_height, ref double mask_pvheight,
        //    bool useKthresholdLab, ref List<Tuple<double, double, double, double, double, double>> hsvList,
        //    ref double mask2_area,
        //    bool sRGB = false,
        //    int brightAreaThreshold = -1, int darkAreaThreshold = -1, bool calcCluster = false)
        //{
        //    if (img_Bmp_diamond == null | img_Bmp_background == null) return false;
        //    if (img_Bmp_diamond.Width != img_Bmp_background.Width | img_Bmp_diamond.Height != img_Bmp_background.Height) return false;

        //    //// Bitmap -> IplImage
        //    IplImage img_diamond;
        //    IplImage img_background;
        //    IplImage img_mask;
        //    IplImage img_mask2 = null;
        //    img_diamond = BitmapConverter.ToIplImage(img_Bmp_diamond);
        //    img_background = BitmapConverter.ToIplImage(img_Bmp_background);
        //    img_mask = Cv.CreateImage(new CvSize(img_diamond.Width, img_diamond.Height), BitDepth.U8, 1);

        //    //// Create software mask
        //    Cv.Zero(img_mask);
        //    // TODO: maskCreate using modified settings, need to fix
        //    if (maskCreate(ref img_diamond, ref img_mask, ref _mask_type, 3) == false)
        //    {
        //        return false;
        //    }

        //    Mat mat_mask = new Mat(img_mask);

        //    if (_mask_type == MASK_TYPE.MASK_NEW)
        //    {
        //        mask_area = Cv2.CountNonZero(mat_mask);
        //    }

        //    IplImage img_Lab_diamond = Cv.CreateImage(new CvSize(img_diamond.Width, img_diamond.Height), BitDepth.U8, 3);
        //    IplImage img_Lab_background = Cv.CreateImage(new CvSize(img_background.Width, img_background.Height), BitDepth.U8, 3);

        //    if (sRGB == true)
        //    {
        //        Cv.CvtColor(img_diamond, img_Lab_diamond, ColorConversion.BgrToLab);
        //        Cv.CvtColor(img_background, img_Lab_background, ColorConversion.BgrToLab);
        //    }
        //    else
        //    {
        //        Cv.CvtColor(img_diamond, img_Lab_diamond, ColorConversion.LbgrToLab);
        //        Cv.CvtColor(img_background, img_Lab_background, ColorConversion.LbgrToLab);
        //    }

        //    // Calculate Ave of L*a*b* 

        //    CvScalar mean_diamond, mean_background;
        //    CvScalar std_diamond, std_background;

        //    Cv.AvgSdv(img_Lab_diamond, out mean_diamond, out std_diamond, img_mask);
        //    L_diamond = mean_diamond.Val0 * 100 / 255;
        //    a_diamond = mean_diamond.Val1 - 128;
        //    b_diamond = mean_diamond.Val2 - 128;

        //    Cv.AvgSdv(img_Lab_background, out mean_background, out std_background, img_mask);
        //    L_background = mean_background.Val0 * 100 / 255;
        //    a_background = mean_background.Val1 - 128;
        //    b_background = mean_background.Val2 - 128;

        //    L = _conv_L * (L_diamond / L_background - _shift_L);
        //    a = _conv_a * (a_diamond - a_background - _shift_a);
        //    b = _conv_b * (b_diamond - b_background - _shift_b);
        //    //a = a_diamond - a_background;
        //    //b = b_diamond - b_background;

        //    if (hsvList != null)
        //    {
        //        Mat mat_mask2 = new Mat(img_mask2);
        //        mask2_area = Cv2.CountNonZero(mat_mask2);

        //        Cv.AvgSdv(img_Lab_diamond, out mean_diamond, out std_diamond, img_mask2);
        //        var L_diamond1 = mean_diamond.Val0 * 100 / 255;
        //        var a_diamond1 = mean_diamond.Val1 - 128;
        //        var b_diamond1 = mean_diamond.Val2 - 128;

        //        Cv.AvgSdv(img_Lab_background, out mean_background, out std_background, img_mask2);
        //        var L_background1 = mean_background.Val0 * 100 / 255;
        //        var a_background1 = mean_background.Val1 - 128;
        //        var b_background1 = mean_background.Val2 - 128;

        //        var L1 = _conv_L * (L_diamond1 / L_background1 - _shift_L);
        //        var a1 = _conv_a * (a_diamond1 - a_background1 - _shift_a);
        //        var b1 = _conv_b * (b_diamond1 - b_background1 - _shift_b);

        //        if (hsvList.Count > 0)
        //        {
        //            var tple = hsvList[0];
        //            L1 = tple.Item1 + L1;
        //            a1 = tple.Item2 + a1;
        //            b1 = tple.Item3 + b1;
        //            hsvList[0] = new Tuple<double, double, double, double, double, double>(L1, a1, b1, 0, 0, 100);
        //        }
        //        else
        //        {
        //            hsvList.Add(new Tuple<double, double, double, double, double, double>(L1, a1, b1, 0, 0, 100));
        //        }

        //        if (calcCluster)
        //        {
        //            Mat mat_diamond = new Mat(img_diamond);
        //            Mat mat_bg = new Mat(img_background);
        //            Mat src2 = new Mat();
        //            Mat src = new Mat();
        //            Mat bg = new Mat();
        //            mat_diamond.CopyTo(src2, mat_mask2);
        //            mat_diamond.CopyTo(src, mat_mask);
        //            mat_bg.CopyTo(bg, mat_mask2);
        //            Mat src_lab = new Mat();
        //            Cv2.CvtColor(src2, src_lab, ColorConversion.LbgrToLab);

        //            Cv2.ImWrite(@"C:\gColorFancy\Image\original_image.jpg", mat_diamond);
        //            Cv2.ImWrite(@"C:\gColorFancy\Image\original_bg.jpg", mat_bg);
        //            Cv2.ImWrite(@"C:\gColorFancy\Image\img_mask2.jpg", mat_mask2);
        //            Cv2.ImWrite(@"C:\gColorFancy\Image\img_mask.jpg", mat_mask);
        //            Cv2.ImWrite(@"C:\gColorFancy\Image\masked_image.jpg", src);
        //            Cv2.ImWrite(@"C:\gColorFancy\Image\masked_image2.jpg", src2);
        //            Cv2.ImWrite(@"C:\gColorFancy\Image\masked_bg.jpg", bg);

        //            List<int> clusterCounts = new List<int>() { 2, 3, 4, 5 };//+1 to account for masked off dark area
        //            Mat points = new Mat();
        //            src_lab.ConvertTo(points, MatType.CV_32FC3);
        //            points = points.Reshape(3, src_lab.Rows * src_lab.Cols);

        //            foreach (int clusterCount in clusterCounts)
        //            {
        //                int startIndex = hsvList.Count;


        //                Mat clusters = Mat.Zeros(points.Size(), MatType.CV_32SC1);
        //                Mat centers = new Mat();

        //                Cv2.Kmeans(points, clusterCount, clusters, new TermCriteria(CriteriaType.Epsilon | CriteriaType.Iteration, 10, 1.0),
        //                    3, KMeansFlag.PpCenters, centers);

        //                Dictionary<int, int[]> data = new Dictionary<int, int[]>();
        //                for (int c = 0; c < clusterCount; c++)
        //                    data.Add(c, new int[4]);

        //                MatOfByte3 mat3 = new MatOfByte3(src_lab); // cv::Mat_<cv::Vec3b>
        //                var indexer = mat3.GetIndexer();

        //                Mat dst_img = Mat.Zeros(src_lab.Size(), src_lab.Type());

        //                MatOfByte3 mat3_dst = new MatOfByte3(dst_img); // cv::Mat_<cv::Vec3b>
        //                var indexer_dst = mat3_dst.GetIndexer();

        //                for (int y = 0, n = 0; y < src_lab.Height; y++)
        //                {
        //                    for (int x = 0; x < src_lab.Width; x++)
        //                    {
        //                        n++;
        //                        int clusterIdx = clusters.At<int>(n);
        //                        Vec3b lab = indexer[y, x];
        //                        data[clusterIdx][0] += (lab.Item0 * 100 / 255);
        //                        data[clusterIdx][1] += lab.Item1 != 0 ? (lab.Item1 - 128) : 0;
        //                        data[clusterIdx][2] += lab.Item2 != 0 ? (lab.Item2 - 128) : 0;
        //                        data[clusterIdx][3]++;

        //                        Vec3b color;
        //                        color.Item0 = (byte)(centers.At<float>(clusterIdx, 0));
        //                        color.Item1 = (byte)(centers.At<float>(clusterIdx, 1));
        //                        color.Item2 = (byte)(centers.At<float>(clusterIdx, 2));    // R <- B
        //                        indexer_dst[y, x] = color;
        //                    }
        //                }

        //                Mat dst_img_bgr = Mat.Zeros(src2.Size(), src2.Type());
        //                try
        //                {
        //                    Cv2.CvtColor(dst_img, dst_img_bgr, ColorConversion.LabToLbgr);
        //                }
        //                catch (Exception exce)
        //                {

        //                }

        //                Cv2.ImWrite(@"C:\gColorFancy\Image\clusters" + clusterCount + ".jpg", dst_img_bgr);

        //                for (int j = 0; j < clusterCount; j++)
        //                {
        //                    var a2 = (double)(data[j][1] / data[j][3]);
        //                    var b2 = (double)(data[j][2] / data[j][3]);
        //                    var C2 = calc_C(ref a2, ref b2);
        //                    var H2 = calc_H(ref a2, ref b2);

        //                    hsvList.Add(new Tuple<double, double, double, double, double, double>((data[j][0] / data[j][3]),
        //                        a2,
        //                        b2,
        //                        C2,
        //                        H2,
        //                        Math.Round((double)(data[j][3] * 100) / (src_lab.Rows * src_lab.Cols), 1)));
        //                }

        //            }
        //        }
        //    }

        //    Cv.ReleaseImage(img_diamond);
        //    Cv.ReleaseImage(img_background);
        //    if (img_mask2 != null)
        //        Cv.ReleaseImage(img_mask2);
        //    Cv.ReleaseImage(img_mask);
        //    Cv.ReleaseImage(img_Lab_diamond);
        //    Cv.ReleaseImage(img_Lab_background);

        //    return true;
        //}

        //public static Boolean GetColor_description(ref List<Bitmap> imageList,
        //    ref List<Bitmap> imgList_background, ref double L, ref double a,
        //    ref double b, ref double C, ref double H, ref string L_description,
        //    ref string C_description, ref string H_description, ref double mask_L,
        //    ref double mask_A, ref string comment1, ref string comment2, ref string comment3,
        //    bool useKthresholdLab = false,
        //    double photochromaL = -1)
        //{
        //    List<Tuple<double, double, double, double, double, double>> hsvList = null;
        //    double mask2_A = 0;
        //    return GetColor_description(ref imageList,
        //        ref imgList_background, ref L, ref a,
        //        ref b, ref C, ref H, ref L_description,
        //        ref C_description, ref H_description, ref mask_L,
        //        ref mask_A, ref comment1, ref comment2, ref comment3, ref mask2_A, ref hsvList,
        //        useKthresholdLab, photochromaL);
        //}

        //public static Boolean GetColor_description(ref List<Bitmap> imageList,
        //    ref List<Bitmap> imgList_background, ref double L, ref double a,
        //    ref double b, ref double C, ref double H, ref string L_description,
        //    ref string C_description, ref string H_description, ref double mask_L,
        //    ref double mask_A, ref string comment1, ref string comment2, ref string comment3,
        //    ref double mask2_A, ref List<Tuple<double, double, double, double, double, double>> hsvList1,
        //    bool useKthresholdLab = false,
        //    double photochromaL = -1,
        //    int brightAreaThreshold = -1, int darkAreaThreshold = -1)
        //{
        //    try
        //    {
        //        Dictionary<string, string> result_RBC;
        //        Dictionary<string, string> result_Fancy;
        //        //double shift_C;
        //        string boundary_version = "";
        //        string refer_stone = "";

        //        //RBC
        //        List<double> list_L_diamond = new List<double>();
        //        List<double> list_a_diamond = new List<double>();
        //        List<double> list_b_diamond = new List<double>();
        //        List<double> list_L_background = new List<double>();
        //        List<double> list_a_background = new List<double>();
        //        List<double> list_b_background = new List<double>();
        //        List<double> list_L = new List<double>();
        //        List<double> list_a = new List<double>();
        //        List<double> list_b = new List<double>();
        //        List<double> list_C = new List<double>();
        //        List<double> list_H = new List<double>();
        //        List<double> list_masklength = new List<double>();
        //        List<double> list_maskarea = new List<double>();
        //        List<double> list_mask2area = new List<double>();
        //        List<double> list_maskwidth = new List<double>();
        //        List<double> list_maskheight = new List<double>();
        //        List<double> list_maskpvheight = new List<double>();
        //        List<double> list_maskaspectratio = new List<double>();

        //        double avg_L = 0, avg_a = 0, avg_b = 0, avg_C = 0, avg_H = 0;
        //        double avg_L_diamond = 0, avg_a_diamond = 0, avg_b_diamond = 0;
        //        double avg_L_background = 0, avg_a_background = 0, avg_b_background = 0;
        //        double avg_masklength = 0, avg_maskarea = 0;
        //        double avg_maskheight = 0;
        //        double avg_maskpvheight = 0;
        //        double maxmin_widthratio = 0, max_aspectratio = 0, min_aspectratio = 0, avg_aspectratio = 0;
        //        //string comment_RBC = "";

        //        //FANCY
        //        List<double> list_L_diamond_Fancy = new List<double>();
        //        List<double> list_a_diamond_Fancy = new List<double>();
        //        List<double> list_b_diamond_Fancy = new List<double>();
        //        List<double> list_L_background_Fancy = new List<double>();
        //        List<double> list_a_background_Fancy = new List<double>();
        //        List<double> list_b_background_Fancy = new List<double>();
        //        List<double> list_L_Fancy = new List<double>();
        //        List<double> list_a_Fancy = new List<double>();
        //        List<double> list_b_Fancy = new List<double>();
        //        List<double> list_C_Fancy = new List<double>();
        //        List<double> list_H_Fancy = new List<double>();
        //        List<double> list_masklength_Fancy = new List<double>();
        //        List<double> list_maskarea_Fancy = new List<double>();
        //        List<double> list_mask2area_Fancy = new List<double>();
        //        List<double> list_maskwidth_Fancy = new List<double>();
        //        List<double> list_maskheight_Fancy = new List<double>();
        //        List<double> list_maskpvheigth_Fancy = new List<double>();
        //        List<double> list_maskaspectratio_Fancy = new List<double>();

        //        double avg_L_Fancy = 0, avg_a_Fancy = 0, avg_b_Fancy = 0, avg_C_Fancy = 0, avg_H_Fancy = 0;

        //        double avg_L_diamond_Fancy = 0, avg_a_diamond_Fancy = 0, avg_b_diamond_Fancy = 0;
        //        double avg_L_background_Fancy = 0, avg_a_background_Fancy = 0, avg_b_background_Fancy = 0;
        //        double avg_masklength_Fancy = 0, avg_maskarea_Fancy = 0;
        //        double maxmin_widthratio_Fancy = 0, max_aspectratio_Fancy = 0, min_aspectratio_Fancy = 0, avg_aspectratio_Fancy = 0;
        //        string L_description_Fancy = "", C_description_Fancy = "", H_description_Fancy = "";
        //        //string comment_Fancy = "";
        //        double lFirst = 0, lLast = 0;

        //        double volume = 0.0; //This is used for the calc of mask area: Fancy shape diamond and special RBC

        //        int i, j;

        //        // For icecream cone shape
        //        _diamond_group = DIAMOND_GROUPING.Default;
        //        comment3 = "FINALIZED";

        //        i = 0;
        //        foreach (Bitmap bm in imageList)
        //        {
        //            double LL = 0, aa = 0, bb = 0;
        //            double LL_diamond = 0, aa_diamond = 0, bb_diamond = 0;
        //            double LL_background = 0, aa_background = 0, bb_background = 0;
        //            double mask_length = 0, mask_area = 0, mask_width = 0, mask_height = 0, mask_pvheight = 0, mask2_area = 0;
        //            Bitmap image = bm;
        //            Bitmap image_background = imgList_background[i];

        //            i++;
        //            bool calculateClusters = false;
        //            if (i >= imgList_background.Count)
        //            {
        //                i = 0;
        //                if (hsvList1 != null)
        //                    calculateClusters = true;
        //            }

        //            //_erode = 1;

        //            if (calcLab_diamond_background_all(ref image, ref image_background, ref LL_diamond,
        //                ref aa_diamond, ref bb_diamond, ref LL_background, ref aa_background, ref bb_background,
        //                ref LL, ref aa, ref bb, ref mask_length, ref mask_area, ref mask_width,
        //                ref mask_height, ref mask_pvheight, useKthresholdLab, ref hsvList1, ref mask2_area, false,
        //                    brightAreaThreshold, darkAreaThreshold, calculateClusters) == true)
        //            {
        //                list_L.Add(LL);
        //                list_a.Add(aa);
        //                list_b.Add(bb);
        //                list_C.Add(calc_C(ref aa, ref bb));
        //                list_H.Add(calc_H(ref aa, ref bb));
        //                list_L_diamond.Add(LL_diamond);
        //                list_a_diamond.Add(aa_diamond);
        //                list_b_diamond.Add(bb_diamond);
        //                list_L_background.Add(LL_background);
        //                list_a_background.Add(aa_background);
        //                list_b_background.Add(bb_background);
        //                list_masklength.Add(mask_length);
        //                list_maskarea.Add(mask_area);
        //                list_mask2area.Add(mask2_area);
        //                list_maskwidth.Add(mask_width);
        //                list_maskheight.Add(mask_height);
        //                list_maskpvheight.Add(mask_pvheight);
        //                list_maskaspectratio.Add(mask_height / mask_width);
        //            }
        //        }

        //        avg_L = list_L.Average();
        //        avg_a = list_a.Average();
        //        avg_b = list_b.Average();
        //        avg_L_diamond = list_L_diamond.Average();
        //        avg_a_diamond = list_a_diamond.Average();
        //        avg_b_diamond = list_b_diamond.Average();
        //        avg_L_background = list_L_background.Average();
        //        avg_a_background = list_a_background.Average();
        //        avg_b_background = list_b_background.Average();
        //        avg_masklength = list_masklength.Average();
        //        avg_maskarea = list_maskarea.Average();
        //        avg_C = calc_C(ref avg_a, ref avg_b);
        //        avg_H = calc_H(ref avg_a, ref avg_b);
        //        maxmin_widthratio = list_maskwidth.Max() / list_maskwidth.Min();
        //        max_aspectratio = list_maskaspectratio.Max();
        //        min_aspectratio = list_maskaspectratio.Min();
        //        avg_aspectratio = list_maskaspectratio.Average();

        //        avg_maskheight = list_maskheight.Average();
        //        avg_maskpvheight = list_maskpvheight.Average();

        //        L = avg_L;
        //        a = avg_a;
        //        b = avg_b;
        //        C = avg_C;
        //        H = avg_H;
        //        mask_L = avg_masklength;
        //        mask_A = avg_maskarea;
        //        mask2_A = list_mask2area.Average();

        //        lFirst = list_L[0];
        //        lLast = list_L[list_L.Count - 1];

        //        //Grouping the diamond
        //        String diamond_proportion = "";

        //        //if (maxmin_widthratio < _widthratio_RBC && min_aspectratio / max_aspectratio >= _aspect_min_max_RBC)
        //        if (maxmin_widthratio <= _widthratio_RBC)
        //        {
        //            //RBC
        //            //shift_C = _shiftC_RBC;

        //            if (avg_aspectratio >= _aspect_min_RBC && avg_aspectratio <= _aspect_max_RBC)
        //            {
        //                diamond_proportion = "RBC: Normal ";
        //                _diamond_group = DIAMOND_GROUPING.RBC;
        //                volume = 1.0;
        //            }
        //            else if (avg_aspectratio > _aspect_max_RBC)
        //            {
        //                // Deep pavilion, High crown, Thick girdle
        //                diamond_proportion = "RBC: High depth ";
        //                _diamond_group = DIAMOND_GROUPING.RBC_HighDepth;
        //                volume = 0.5; //For triangle mask
        //            }
        //            else
        //            {
        //                // Shallow pavilion
        //                diamond_proportion = "RBC: Low depth ";
        //                _diamond_group = DIAMOND_GROUPING.RBC_LowDepth;
        //                volume = 1.0;
        //            }
        //        }
        //        else if (maxmin_widthratio <= _widthratio_Fancy_L)
        //        {
        //            //Fancy_L: HT, CU
        //            //shift_C = _shiftC_FancyL;

        //            if (min_aspectratio >= _aspect_min_FancyL && min_aspectratio <= _aspect_max_FancyL)
        //            {
        //                diamond_proportion = "FANCY_L: Normal ";
        //                _diamond_group = DIAMOND_GROUPING.FANCY_L;
        //                volume = 1.0;
        //            }
        //            else if (min_aspectratio < _aspect_min_FancyL)
        //            {
        //                diamond_proportion = "FANCY_L: Low depth ";
        //                _diamond_group = DIAMOND_GROUPING.FANCY_L_LowDepth;
        //                volume = 1.0;
        //            }
        //            else
        //            {
        //                diamond_proportion = "FANCY_L: High depth ";
        //                _diamond_group = DIAMOND_GROUPING.FANCY_L_HighDepth;
        //                volume = 1.0;
        //            }
        //        }
        //        else if (maxmin_widthratio <= _widthratio_Fancy_H)
        //        {
        //            //Fancy_H: REC, SQ, OV, PR
        //            //shift_C = _shiftC_FancyH;

        //            if (min_aspectratio >= _aspect_min_FancyH && min_aspectratio <= _aspect_max_FancyH)
        //            {
        //                diamond_proportion = "FANCY_H: Normal ";
        //                _diamond_group = DIAMOND_GROUPING.FNACY_H;
        //                volume = 1.0;
        //            }
        //            else if (min_aspectratio < _aspect_min_FancyH)
        //            {
        //                diamond_proportion = "FANCY_H: Low depth ";
        //                _diamond_group = DIAMOND_GROUPING.FANCY_H_LowDepth;
        //                volume = 1.0;
        //            }
        //            else
        //            {
        //                diamond_proportion = "FANCY_H: High depth ";
        //                _diamond_group = DIAMOND_GROUPING.FANCY_H_HighDepth;
        //                volume = 1.0;
        //            }
        //        }
        //        else
        //        {
        //            //Fancy_HH: mosly MQ
        //            //shift_C = _shiftC_FancyHH;

        //            if (min_aspectratio >= _aspect_min_FancyHH && min_aspectratio <= _aspect_max_FancyHH)
        //            {
        //                diamond_proportion = "FANCY_HH: Normal ";
        //                _diamond_group = DIAMOND_GROUPING.FANCY_HH;
        //                volume = 1.0;
        //            }
        //            else if (min_aspectratio < _aspect_min_FancyH)
        //            {
        //                diamond_proportion = "FANCY_HH: Low depth ";
        //                _diamond_group = DIAMOND_GROUPING.FANCY_HH_LowDepth;
        //                volume = 1.0;
        //            }
        //            else
        //            {
        //                diamond_proportion = "FANCY_HH: High depth ";
        //                _diamond_group = DIAMOND_GROUPING.FANCY_HH_HighDepth;
        //                volume = 1.0;
        //            }
        //        }

        //        //debug

        //        //result_RBC = Boundary.GetGrade_shifting(H, C, L, shift_C);
        //        Boundary boundary = new Boundary();
        //        result_RBC = boundary.GetGrade(H, C, L, (int)_diamond_group);
        //        L_description = result_RBC["L_description"];
        //        C_description = result_RBC["C_description"];
        //        H_description = result_RBC["H_description"];
        //        boundary_version = result_RBC["Version"];
        //        refer_stone = result_RBC["Refer"];

        //        //getColorGrade_description(L, C, H, ref L_description, ref C_description, ref H_description, ref comment_RBC);

        //        comment1 = avg_L_diamond.ToString() + ", " + avg_a_diamond.ToString() + ", " + avg_b_diamond.ToString() + ", "
        //            + avg_L_background.ToString() + ", " + avg_a_background.ToString() + ", " + avg_b_background.ToString() + ", "
        //            + avg_L.ToString() + ", " + avg_a.ToString() + ", " + avg_b.ToString() + ", "
        //            + avg_C.ToString() + ", " + avg_H.ToString() + ", "
        //            + L_description + ", " + C_description + ", " + H_description + ", " + boundary_version + ", "
        //            + avg_masklength.ToString() + ", " + avg_maskarea.ToString();

        //        comment1 = _deviceName + ", 1.0, " + comment1 + ", " + avg_maskheight.ToString() +
        //                        ", " + avg_maskpvheight.ToString() + ", " + maxmin_widthratio.ToString() + ", " +
        //                        min_aspectratio.ToString() + ", " + diamond_proportion;

        //        //if (_diamond_group == DIAMOND_GROUPING.RBC_LowDepth || _diamond_group == DIAMOND_GROUPING.RBC_HighDepth ||
        //        //    _diamond_group == DIAMOND_GROUPING.FANCY_L_LowDepth || _diamond_group == DIAMOND_GROUPING.FANCY_L_HighDepth ||
        //        //    _diamond_group == DIAMOND_GROUPING.FANCY_H_LowDepth || _diamond_group == DIAMOND_GROUPING.FANCY_H_HighDepth ||
        //        //    _diamond_group == DIAMOND_GROUPING.FANCY_HH_LowDepth || _diamond_group == DIAMOND_GROUPING.FANCY_HH_HighDepth)
        //        if (_diamond_group == DIAMOND_GROUPING.RBC_HighDepth)
        //        {
        //            comment3 = "GO TO VISUAL - DEPTH";
        //        }
        //        else
        //        {
        //            if (C_description == "N/A")
        //            {
        //                comment3 = "GO TO VISUAL";
        //            }
        //            else
        //            {
        //                if (refer_stone != "FALSE")
        //                {
        //                    comment3 = "GO TO VISUAL - ANALYZE";
        //                }

        //            }
        //        }
        //        ////////////////////////////////////////////////////////////
        //        // If the diamond is high depth RBC (ice cream cone shape)
        //        // calcurate LCH with triangle mask
        //        ////////////////////////////////////////////////////////////

        //        if (_diamond_group == DIAMOND_GROUPING.RBC_HighDepth)
        //        {
        //            i = 0;
        //            j = 0;
        //            if (hsvList1 != null)
        //                hsvList1 = new List<Tuple<double, double, double, double, double, double>>();

        //            foreach (Bitmap bm in imageList)
        //            {
        //                double LL = 0, aa = 0, bb = 0;
        //                double LL_diamond = 0, aa_diamond = 0, bb_diamond = 0;
        //                double LL_background = 0, aa_background = 0, bb_background = 0;
        //                double mask_length = 0, mask_area = 0, mask_width = 0, mask_height = 0, mask_pvheight = 0, mask2_area = 0;
        //                Bitmap image = bm;
        //                Bitmap image_background = imgList_background[i];

        //                i++;
        //                bool calculateClusters = false;
        //                if (i >= imgList_background.Count)
        //                {
        //                    i = 0;
        //                    if (hsvList1 != null)
        //                        calculateClusters = true;
        //                }

        //                ////////////////////////////////////////////////
        //                // Need to calc appropriate erode for each image
        //                ////////////////////////////////////////////////

        //                //double _a = list_maskheight[j];
        //                //double _b = list_maskwidth[j];
        //                //double temp = (_a + _b) / 4.0 - 1 / 2.0 * Math.Sqrt(((_a + _b) / 2) * ((_a + _b) / 2) - _a * _b * (1 - volume));
        //                //temp = temp / 1.6;

        //                //if (volume < 1.0) _erode = (int)temp;
        //                //else _erode = 1;

        //                ////////////////////////////////////////////////
        //                ////////////////////////////////////////////////
        //                //_erode = 1;

        //                if (calcLab_diamond_background_all(ref image, ref image_background, ref LL_diamond,
        //                    ref aa_diamond, ref bb_diamond, ref LL_background, ref aa_background,
        //                    ref bb_background, ref LL, ref aa, ref bb, ref mask_length, ref mask_area,
        //                    ref mask_width, ref mask_height, ref mask_pvheight, useKthresholdLab, ref hsvList1, ref mask2_area, false,
        //                        brightAreaThreshold, darkAreaThreshold, calculateClusters) == true)
        //                {
        //                    list_L_Fancy.Add(LL);
        //                    list_a_Fancy.Add(aa);
        //                    list_b_Fancy.Add(bb);
        //                    list_C_Fancy.Add(calc_C(ref aa, ref bb));
        //                    list_H_Fancy.Add(calc_H(ref aa, ref bb));
        //                    list_L_diamond_Fancy.Add(LL_diamond);
        //                    list_a_diamond_Fancy.Add(aa_diamond);
        //                    list_b_diamond_Fancy.Add(bb_diamond);
        //                    list_L_background_Fancy.Add(LL_background);
        //                    list_a_background_Fancy.Add(aa_background);
        //                    list_b_background_Fancy.Add(bb_background);
        //                    list_masklength_Fancy.Add(mask_length);
        //                    list_maskarea_Fancy.Add(mask_area);
        //                    list_mask2area_Fancy.Add(mask2_area);
        //                    list_maskwidth_Fancy.Add(mask_width);
        //                    list_maskheight_Fancy.Add(mask_height);
        //                    list_maskpvheigth_Fancy.Add(mask_pvheight);
        //                    list_maskaspectratio_Fancy.Add(mask_height / mask_width);
        //                }
        //                j++;
        //            }

        //            avg_L_Fancy = list_L_Fancy.Average();
        //            avg_a_Fancy = list_a_Fancy.Average();
        //            avg_b_Fancy = list_b_Fancy.Average();
        //            avg_L_diamond_Fancy = list_L_diamond_Fancy.Average();
        //            avg_a_diamond_Fancy = list_a_diamond_Fancy.Average();
        //            avg_b_diamond_Fancy = list_b_diamond_Fancy.Average();
        //            avg_L_background_Fancy = list_L_background_Fancy.Average();
        //            avg_a_background_Fancy = list_a_background_Fancy.Average();
        //            avg_b_background_Fancy = list_b_background_Fancy.Average();
        //            avg_masklength_Fancy = list_masklength_Fancy.Average();
        //            avg_maskarea_Fancy = list_maskarea_Fancy.Average();
        //            avg_C_Fancy = calc_C(ref avg_a_Fancy, ref avg_b_Fancy);
        //            avg_H_Fancy = calc_H(ref avg_a_Fancy, ref avg_b_Fancy);
        //            maxmin_widthratio_Fancy = list_maskwidth_Fancy.Max() / list_maskwidth_Fancy.Min();
        //            max_aspectratio_Fancy = list_maskaspectratio_Fancy.Max();
        //            min_aspectratio_Fancy = list_maskaspectratio_Fancy.Min();
        //            avg_aspectratio_Fancy = list_maskaspectratio_Fancy.Average();
        //            mask2_A = list_mask2area_Fancy.Average();

        //            //result_Fancy = Boundary.GetGrade_shifting(H, C, L, shift_C);
        //            result_Fancy = boundary.GetGrade(avg_H_Fancy, avg_C_Fancy, avg_L_Fancy, (int)_diamond_group);
        //            L_description_Fancy = result_Fancy["L_description"];
        //            C_description_Fancy = result_Fancy["C_description"];
        //            H_description_Fancy = result_Fancy["H_description"];
        //            boundary_version = result_Fancy["Version"];
        //            refer_stone = result_Fancy["Refer"];

        //            //getColorGrade_description(avg_L_Fancy, avg_C_Fancy, avg_H_Fancy, ref L_description_Fancy, ref C_description_Fancy, ref H_description_Fancy, ref comment_Fancy);

        //            comment2 = avg_L_diamond_Fancy.ToString() + ", " + avg_a_diamond_Fancy.ToString() + ", " + avg_b_diamond_Fancy.ToString() + ", "
        //                           + avg_L_background_Fancy.ToString() + ", " + avg_a_background_Fancy.ToString() + ", " + avg_b_background_Fancy.ToString() + ", "
        //                           + avg_L_Fancy.ToString() + ", " + avg_a_Fancy.ToString() + ", " + avg_b_Fancy.ToString() + ", "
        //                           + avg_C_Fancy.ToString() + ", " + avg_H_Fancy.ToString() + ", "
        //                           + L_description_Fancy + ", " + C_description_Fancy + ", " + H_description_Fancy + ", " + boundary_version + ", "
        //                           + avg_masklength.ToString() + ", " + avg_maskarea.ToString();

        //            comment2 = _deviceName + ", " + volume.ToString() + ", " + comment2 + ", " + avg_maskheight.ToString() +
        //                        ", " + avg_maskpvheight.ToString() + ", " + maxmin_widthratio.ToString() + ", " +
        //                        max_aspectratio.ToString() + ", " + diamond_proportion;

        //            comment1 = comment2;
        //            comment2 = "";
        //            comment3 = "GO TO VISUAL - DEPTH";

        //            L = avg_L_Fancy;
        //            C = avg_C_Fancy;
        //            H = avg_H_Fancy;
        //            L_description = L_description_Fancy;
        //            C_description = C_description_Fancy;
        //            H_description = H_description_Fancy;

        //            lFirst = list_L_Fancy[0];
        //            lLast = list_L_Fancy[list_L_Fancy.Count - 1];
        //        }

        //        //Compare image count and mask count 
        //        if (list_C.Count < imageList.Count)
        //        {
        //            comment3 = "Measure again: failed image ="
        //             + list_C.Count.ToString() + "/" + imageList.Count.ToString();

        //            return false;
        //        }

        //        if (Math.Abs(imageList.Count - imgList_background.Count) > 3)
        //        {
        //            comment3 = "Measure again: diamond/bg =" + imageList.Count.ToString() +
        //            "/" + imgList_background.Count.ToString();

        //            return false;
        //        }

        //        if (photochromaL >= 0)
        //        {
        //            double lDiff = Math.Abs(lLast - lFirst);
        //            if (lDiff > photochromaL)
        //                comment3 = "Color change[" + lDiff + "]";
        //            else
        //                comment3 = "";
        //        }

        //    }
        //    catch (Exception exc)
        //    {
        //        comment3 = "Measure again: process failure";
        //        return false;
        //    }

        //    return true;
        //}


        //private static Boolean calcLab_Fluorescence(ref Bitmap img_Bmp_outline, ref Bitmap img_Bmp_pl, ref double L_pl, ref double a_pl, ref double b_pl, ref double mask_length, ref double mask_area, ref double mask_width, ref double mask_height, ref double mask_pvheight, ref string comment, double threshold_darklevel = 5, bool sRGB = false)
        //{
        //    if (img_Bmp_outline == null | img_Bmp_pl == null) return false;
        //    if (img_Bmp_outline.Width != img_Bmp_pl.Width | img_Bmp_outline.Height != img_Bmp_pl.Height) return false;

        //    //// Bitmap -> IplImage
        //    IplImage img_outline;
        //    IplImage img_pl;
        //    IplImage img_pl_gray;
        //    IplImage img_mask_L;
        //    IplImage img_mask_CH;
        //    IplImage img_mask_and;

        //    img_outline = BitmapConverter.ToIplImage(img_Bmp_outline);
        //    img_pl = BitmapConverter.ToIplImage(img_Bmp_pl);
        //    img_pl_gray = Cv.CreateImage(new CvSize(img_outline.Width, img_outline.Height), BitDepth.U8, 1);
        //    img_mask_L = Cv.CreateImage(new CvSize(img_outline.Width, img_outline.Height), BitDepth.U8, 1);
        //    img_mask_CH = Cv.CreateImage(new CvSize(img_outline.Width, img_outline.Height), BitDepth.U8, 1);
        //    img_mask_and = Cv.CreateImage(new CvSize(img_outline.Width, img_outline.Height), BitDepth.U8, 1);

        //    //// Create software mask
        //    Cv.Zero(img_mask_L);
        //    Cv.Zero(img_mask_CH);
        //    Cv.Zero(img_mask_and);

        //    if ((maskCreate(ref img_outline, ref img_mask_L, ref _mask_type) == false) ||
        //        (check_object_touch_border(new Mat(img_mask_L)) == true))
        //    {
        //        Cv.ReleaseImage(img_outline);
        //        Cv.ReleaseImage(img_pl);
        //        Cv.ReleaseImage(img_pl_gray);
        //        Cv.ReleaseImage(img_mask_L);
        //        Cv.ReleaseImage(img_mask_CH);
        //        Cv.ReleaseImage(img_mask_and);
        //        return false;
        //    }

        //    Cv.CvtColor(img_pl, img_pl_gray, ColorConversion.BgrToGray);
        //    Cv.Threshold(img_pl_gray, img_mask_CH, threshold_darklevel, 255, ThresholdType.Binary);
        //    Cv.Copy(img_mask_CH, img_mask_and, img_mask_L);

        //    //debug
        //    //Cv.SaveImage("C:\\Users\\htakahas\\Desktop\\temp_outline.bmp", img_outline);
        //    //Cv.SaveImage("C:\\Users\\htakahas\\Desktop\\temp_pl.bmp", img_pl);
        //    //Cv.SaveImage("C:\\Users\\htakahas\\Desktop\\temp_maskL.bmp", img_mask_L);
        //    //Cv.SaveImage("C:\\Users\\htakahas\\Desktop\\temp_maskCH1.bmp", img_mask_CH);
        //    //Cv.SaveImage("C:\\Users\\htakahas\\Desktop\\temp_maskAnd.bmp", img_mask_and);

        //    IplImage img_Lab_pl = Cv.CreateImage(new CvSize(img_pl.Width, img_pl.Height), BitDepth.U8, 3);

        //    if (sRGB == true)
        //    {
        //        //Cv.CvtColor(img_outline, img_Lab_outline, ColorConversion.BgrToLab);
        //        Cv.CvtColor(img_pl, img_Lab_pl, ColorConversion.BgrToLab);
        //    }
        //    else
        //    {
        //        //Cv.CvtColor(img_outline, img_Lab_outline, ColorConversion.LbgrToLab);
        //        Cv.CvtColor(img_pl, img_Lab_pl, ColorConversion.LbgrToLab);
        //    }

        //    // Calculate Ave of L*a*b* 

        //    CvScalar mean_background;
        //    CvScalar std_background;

        //    Cv.AvgSdv(img_Lab_pl, out mean_background, out std_background, img_mask_L);
        //    L_pl = mean_background.Val0 * 100 / 255;

        //    Cv.AvgSdv(img_Lab_pl, out mean_background, out std_background, img_mask_and);
        //    a_pl = mean_background.Val1 - 128;
        //    b_pl = mean_background.Val2 - 128;

        //    ///////////////////////////////////////////////////////
        //    //Comment -> identify multicolor fluorescence diamond 
        //    comment = "";

        //    //Not yet implemented
        //    ////////////////////////////////////////////////////////

        //    Cv.ReleaseImage(img_outline);
        //    Cv.ReleaseImage(img_pl);
        //    Cv.ReleaseImage(img_pl_gray);
        //    Cv.ReleaseImage(img_mask_L);
        //    Cv.ReleaseImage(img_mask_CH);
        //    Cv.ReleaseImage(img_mask_and);
        //    Cv.ReleaseImage(img_Lab_pl);

        //    return true;
        //}

        //public static Boolean GetPL_description(ref List<Bitmap> imageList_outline, ref List<Bitmap> imgList_PL,
        //    ref double L, ref double C, ref double H, ref string L_description,
        //    ref string C_description, ref string H_description, ref string comment, ref string instruction,
        //    string gain, string shutter, out string version, double L_override, out bool checkMultiColor)
        //{
        //    version = "";
        //    checkMultiColor = false;

        //    if (imageList_outline.Count < imgList_PL.Count)
        //    {
        //        comment = "Image discrepancy" + "BG:" + imageList_outline.Count.ToString() + " < PL:" + imgList_PL.Count.ToString();
        //        return false;
        //    }

        //    instruction = "FINALIZED";

        //    List<double> list_L_pl = new List<double>();
        //    List<double> list_a_pl = new List<double>();
        //    List<double> list_b_pl = new List<double>();
        //    List<double> list_masklength = new List<double>();
        //    List<double> list_maskarea = new List<double>();
        //    List<double> list_maskwidth = new List<double>();
        //    List<double> list_maskheight = new List<double>();
        //    List<double> list_maskpvheight = new List<double>();
        //    List<string> list_comment = new List<string>();
        //    int i = 0;

        //    //foreach (Bitmap bm in imageList_outline)
        //    foreach (Bitmap bm in imgList_PL)
        //    {
        //        double LL_pl = 0, aa_pl = 0, bb_pl = 0;
        //        double mask_length = 0, mask_area = 0;
        //        double mask_width = 0, mask_height = 0, mask_pvheight = 0;
        //        string comment_pl = "";
        //        //Bitmap image_outline = bm;
        //        //Bitmap image_pl = imgList_PL[i];
        //        Bitmap image_pl = bm;
        //        Bitmap image_outline = imageList_outline[i];

        //        i++;

        //        if (calcLab_Fluorescence(ref image_outline, ref image_pl, ref LL_pl, ref aa_pl, ref bb_pl, ref mask_length, ref mask_area, ref mask_width, ref mask_height, ref mask_pvheight, ref comment_pl, _darkLevel))
        //        {
        //            list_L_pl.Add(LL_pl);
        //            list_a_pl.Add(aa_pl);
        //            list_b_pl.Add(bb_pl);
        //            list_masklength.Add(mask_length);
        //            list_maskarea.Add(mask_area);
        //            list_maskwidth.Add(mask_width);
        //            list_maskheight.Add(mask_height);
        //            list_maskpvheight.Add(mask_pvheight);
        //            list_comment.Add(comment_pl);
        //        }
        //    }

        //    //Compare image count and mask count 
        //    if (list_L_pl.Count < imgList_PL.Count)
        //    {
        //        comment = "Measure again: failed mask";
        //        return false;
        //    }

        //    double avg_L_pl = list_L_pl.Average();
        //    double avg_a_pl = list_a_pl.Average();
        //    double avg_b_pl = list_b_pl.Average();
        //    double avg_masklength = list_masklength.Average();
        //    double avg_maskarea = list_maskarea.Average();

        //    double avg_C_pl = calc_C(ref avg_a_pl, ref avg_b_pl);
        //    double avg_H_pl = calc_H(ref avg_a_pl, ref avg_b_pl);
        //    L = avg_L_pl;
        //    C = avg_C_pl;
        //    H = avg_H_pl;

        //    ////////////////////////////////////////////////////
        //    //Here is the place of fluorescence grading function
        //    //L_description = "PL grade";
        //    //H_description = "PL color";
        //    Boundary boundary = new Boundary_FL();
        //    Dictionary<string, string> result = null;
        //    if (L_override < 0)
        //        result = boundary.GetGrade(H, C, L);
        //    else
        //        result = boundary.GetGrade(H, C, L_override);
        //    L_description = result["L_description"];
        //    C_description = result["C_description"];
        //    H_description = result["H_description"];
        //    version = result["Version"];
        //    checkMultiColor = result["Multi_Color"].ToUpper() == "TRUE";

        //    ////////////////////////////////////////////////////

        //    comment =
        //        avg_L_pl.ToString() + ", " + avg_a_pl.ToString() + ", " + avg_b_pl.ToString() + ", "
        //        + avg_C_pl.ToString() + ", " + avg_H_pl.ToString() + ", "
        //        + L_description + ", " + C_description + ", " + H_description + ", "
        //        + avg_masklength.ToString() + ", " + avg_maskarea.ToString() + ", " + gain + ", " + shutter;

        //    if (L_description == "N/A")
        //    {
        //        instruction = "GO TO VISUAL";
        //    }
        //    else
        //    {
        //        if (result["Refer"] != "FALSE")
        //        {
        //            instruction = "GO TO VISUAL - ANALYZE";
        //        }

        //    }

        //    return true;
        //}


        //public static string GetColor_test2(ref List<Bitmap> imageList, ref List<Bitmap> imgList_background, ref double L, ref double C, ref double H)
        //{
        //    List<double> list_L = new List<double>();
        //    List<double> list_a = new List<double>();
        //    List<double> list_b = new List<double>();
        //    List<double> list_masklength = new List<double>();
        //    List<double> list_maskarea = new List<double>();
        //    int i = 0;

        //    foreach (Bitmap bm in imageList)
        //    {
        //        double LL = 0, aa = 0, bb = 0;
        //        double mask_length = 0, mask_area = 0;
        //        double mask_width = 0, mask_height = 0, mask_pvheight = 0;
        //        Bitmap image = bm;
        //        Bitmap image_background = imgList_background[i];

        //        i++;
        //        if (i >= imgList_background.Count) i = 0;

        //        if (calcLab_diamond_background(ref image, ref image_background, ref LL, ref aa, ref bb, ref mask_length,
        //            ref mask_area, ref mask_width, ref mask_height, ref mask_pvheight, false) == true)
        //        {
        //            list_L.Add(LL);
        //            list_a.Add(aa);
        //            list_b.Add(bb);
        //            list_masklength.Add(mask_length);
        //            list_maskarea.Add(mask_area);
        //        }

        //    }

        //    double avg_L = list_L.Average();
        //    double avg_a = list_a.Average();
        //    double avg_b = list_b.Average();
        //    double avg_C = calc_C(ref avg_a, ref avg_b);
        //    double avg_H = calc_H(ref avg_a, ref avg_b);

        //    L = avg_L;
        //    C = avg_C;
        //    H = avg_H;

        //    //return "D";
        //    Boundary boundary = new Boundary();
        //    Dictionary<string, string> result = boundary.GetGrade_shifting(H, C, L, 0);
        //    return result["C_description"];
        //}

        //private static Boolean calcLab_diamond_background(ref Bitmap img_Bmp_diamond, ref Bitmap img_Bmp_background,
        //    ref double L, ref double a, ref double b,
        //    ref double mask_length, ref double mask_area, ref double mask_width,
        //    ref double mask_height, ref double mask_pvheight, bool sRGB = false)
        //{
        //    if (img_Bmp_diamond == null | img_Bmp_background == null) return false;
        //    if (img_Bmp_diamond.Width != img_Bmp_background.Width | img_Bmp_diamond.Height != img_Bmp_background.Height) return false;

        //    //// Bitmap -> IplImage
        //    IplImage img_diamond;
        //    IplImage img_background;
        //    IplImage img_mask;
        //    img_diamond = BitmapConverter.ToIplImage(img_Bmp_diamond);
        //    img_background = BitmapConverter.ToIplImage(img_Bmp_background);
        //    img_mask = Cv.CreateImage(new CvSize(img_diamond.Width, img_diamond.Height), BitDepth.U8, 1);

        //    //// Create software mask
        //    Cv.Zero(img_mask);
        //    if (maskCreate(ref img_diamond, ref img_mask, ref _mask_type) == false)
        //    {
        //        return false;
        //    }

        //    IplImage img_Lab_diamond = Cv.CreateImage(new CvSize(img_diamond.Width, img_diamond.Height), BitDepth.U8, 3);
        //    IplImage img_Lab_background = Cv.CreateImage(new CvSize(img_background.Width, img_background.Height), BitDepth.U8, 3);

        //    if (sRGB == true)
        //    {
        //        Cv.CvtColor(img_diamond, img_Lab_diamond, ColorConversion.BgrToLab);
        //        Cv.CvtColor(img_background, img_Lab_background, ColorConversion.BgrToLab);
        //    }
        //    else
        //    {
        //        Cv.CvtColor(img_diamond, img_Lab_diamond, ColorConversion.LbgrToLab);
        //        Cv.CvtColor(img_background, img_Lab_background, ColorConversion.LbgrToLab);
        //    }

        //    // Calculate Ave of L*a*b* 
        //    double L_diamond = 0.0, a_diamond = 0.0, b_diamond = 0.0;
        //    double L_background = 0.0, a_background = 0.0, b_background = 0.0;
        //    CvScalar mean_diamond, mean_background;
        //    CvScalar std_diamond, std_background;

        //    Cv.AvgSdv(img_Lab_diamond, out mean_diamond, out std_diamond, img_mask);
        //    L_diamond = mean_diamond.Val0 * 100 / 255;
        //    a_diamond = mean_diamond.Val1 - 128;
        //    b_diamond = mean_diamond.Val2 - 128;

        //    Cv.AvgSdv(img_Lab_background, out mean_background, out std_background, img_mask);
        //    L_background = mean_background.Val0 * 100 / 255;
        //    a_background = mean_background.Val1 - 128;
        //    b_background = mean_background.Val2 - 128;

        //    L = L_diamond / L_background;
        //    a = _conv_a * (a_diamond - a_background - _shift_a);
        //    b = _conv_b * (b_diamond - b_background - _shift_b);
        //    //a = a_diamond - a_background;
        //    //b = b_diamond - b_background;

        //    Cv.ReleaseImage(img_diamond);
        //    Cv.ReleaseImage(img_background);
        //    Cv.ReleaseImage(img_mask);
        //    Cv.ReleaseImage(img_Lab_diamond);
        //    Cv.ReleaseImage(img_Lab_background);

        //    return true;
        //}

        //public static Boolean calcLab_ROI(ref Bitmap img_Bmp, ref double L, ref double a, ref double b, bool sRGB = false)
        //{
        //    if (img_Bmp == null) return false;
        //    //// Bitmap -> IplImage
        //    IplImage img;
        //    img = BitmapConverter.ToIplImage(img_Bmp);
        //    IplImage img_Lab = Cv.CreateImage(new CvSize(img.Width, img.Height), BitDepth.U8, 3);

        //    if (sRGB == true)
        //    {
        //        Cv.CvtColor(img, img_Lab, ColorConversion.BgrToLab);
        //    }
        //    else
        //    {
        //        Cv.CvtColor(img, img_Lab, ColorConversion.LbgrToLab);
        //    }

        //    //ROI
        //    CvRect bgmonitor_ROI = new CvRect(Convert.ToInt16(img.Width / 3), Convert.ToInt16(img.Height / 3), Convert.ToInt16(img.Width / 3), Convert.ToInt16(img.Height / 3));

        //    Cv.SetImageROI(img_Lab, bgmonitor_ROI);

        //    // Calculate Ave and Sdv of L*a*b*
        //    CvScalar mean;
        //    CvScalar std_dev;
        //    Cv.AvgSdv(img_Lab, out mean, out std_dev);
        //    L = mean.Val0 * 100 / 255;
        //    a = mean.Val1 - 128;
        //    b = mean.Val2 - 128;

        //    Cv.ResetImageROI(img_Lab);
        //    Cv.ReleaseImage(img_Lab);
        //    Cv.ReleaseImage(img);

        //    return true;
        //}

        //public static Boolean calcLab_wholeimage(ref Bitmap img_Bmp, ref double L, ref double a, ref double b, bool sRGB = false)
        //{
        //    if (img_Bmp == null) return false;

        //    //// Bitmap -> IplImage
        //    IplImage img;
        //    img = BitmapConverter.ToIplImage(img_Bmp);
        //    IplImage img_Lab = Cv.CreateImage(new CvSize(img.Width, img.Height), BitDepth.U8, 3);

        //    if (sRGB == true)
        //    {
        //        Cv.CvtColor(img, img_Lab, ColorConversion.BgrToLab);
        //    }
        //    else
        //    {
        //        Cv.CvtColor(img, img_Lab, ColorConversion.LbgrToLab);
        //    }

        //    // Calculate Ave and Sdv of L*a*b*
        //    CvScalar mean;
        //    CvScalar std_dev;
        //    Cv.AvgSdv(img_Lab, out mean, out std_dev);
        //    L = mean.Val0 * 100 / 255;
        //    a = mean.Val1 - 128;
        //    b = mean.Val2 - 128;

        //    Cv.ReleaseImage(img_Lab);
        //    Cv.ReleaseImage(img);
        //    return true;
        //}

        //public static Boolean calcBGR_wholeimage(ref Bitmap img_Bmp, ref double B, ref double G, ref double R)
        //{
        //    if (img_Bmp == null) return false;

        //    //// Bitmap -> IplImage
        //    IplImage img;
        //    img = BitmapConverter.ToIplImage(img_Bmp);

        //    // Calculate Ave and Sdv of BGR
        //    CvScalar mean;
        //    CvScalar std_dev;
        //    Cv.AvgSdv(img, out mean, out std_dev);
        //    B = mean.Val0;
        //    G = mean.Val1;
        //    R = mean.Val2;

        //    Cv.ReleaseImage(img);
        //    return true;
        //}

        //public static double calc_C(ref double a, ref double b)
        //{
        //    return Math.Sqrt(a * a + b * b);
        //}

        //public static double calc_H(ref double a, ref double b)
        //{
        //    return Math.Atan2(b, a) * 180 / Math.PI;
        //}

        //public static double calc_deltaE(ref double L, ref double a, ref double b)
        //{
        //    return Math.Sqrt((100 - L) * (100 - L) + a * a + b * b);
        //}


        //public static Boolean maskCreate(ref IplImage img, ref IplImage img_mask, ref MASK_TYPE mask_type)
        //{
        //    if (img == null | img_mask == null) return false;
        //    if (img.Width != img_mask.Width | img.Height != img_mask.Height) return false;
        //    if (_mask == null) return false;

        //    _mask.SetSrc(img);
        //    bool result = _mask.Create(out Bitmap m);
        //    if(result)
        //    {
        //        img_mask = BitmapConverter.ToIplImage(m);
        //    } else
        //    {
        //        return false;
        //    }
        //    return true;
        //}

        //public static Boolean maskCreate(ref IplImage img, ref IplImage img_mask, ref MASK_TYPE mask_type, int avenum)
        //{
        //    if (img == null | img_mask == null) return false;
        //    if (img.Width != img_mask.Width | img.Height != img_mask.Height) return false;
        //    if (_mask == null) return false;

        //    // save original avenum
        //    int num = _mask.GetAveNum();
        //    _mask.SetAveNum(avenum);

        //    _mask.SetSrc(img);
        //    bool result = _mask.Create(out Bitmap m);

        //    // reset to original avenum
        //    _mask.SetAveNum(num);

        //    if (result)
        //    {
        //        img_mask = BitmapConverter.ToIplImage(m);
        //    }
        //    else
        //    {
        //        return false;
        //    }
        //    return true;
        //}

        //public static byte[] CalcLut(int contrast, int brightness)
        //{
        //    byte[] lut = new byte[256];
        //    if (contrast > 0)
        //    {
        //        double delta = 127.0 * contrast / 100;
        //        double a = 255.0 / (255.0 - delta * 2);
        //        double b = a * (brightness - delta);
        //        for (int i = 0; i < 256; i++)
        //        {
        //            int v = Cv.Round(a * i + b);
        //            if (v < 0) v = 0;
        //            if (v > 255) v = 255;
        //            lut[i] = (byte)v;
        //        }
        //    }
        //    else
        //    {
        //        double delta = -128.0 * contrast / 100;
        //        double a = (256.0 - delta * 2) / 255.0;
        //        double b = a * brightness + delta;

        //        for (int i = 0; i < 256; i++)
        //        {
        //            int v = Cv.Round(a * i + b);

        //            if (v < 0) v = 0;
        //            if (v > 255) v = 255;
        //            lut[i] = (byte)v;
        //        }
        //    }
        //    return lut;
        //}

        public static Boolean check_Hash_Boundary(string hash_key, int boundary_table_type = 1)
        {
            try
            {
                string hash_boundary = null;
                Boundary boundary = null;
                if (boundary_table_type == 1)
                    boundary = new Boundary();
                else if (boundary_table_type == 2)
                    boundary = new Boundary_FL();

                hash_boundary = boundary.GetMD5HashFromFile();
                if (hash_key == hash_boundary)
                {
                    return true;
                }
                return false;

            }
            catch (Exception exc)
            {
                return false;
            }
        }

        //public static bool IsImageLightStable(ref List<Bitmap> imageList, ref List<Bitmap> bgImageList, int imageDiffLimit)
        //{
        //    bool result = false;
        //    int top = 10;
        //    int left = 10;
        //    int height = 10;
        //    int width = 10;

        //    int badCountLimit = 1;

        //    try
        //    {
        //        int count = imageList.Count;
        //        if (bgImageList.Count < count)
        //            count = bgImageList.Count;

        //        for (int i = 0; i < count; i++)
        //        {
        //            Mat image = BitmapConverter.ToMat(imageList[i]);
        //            Mat bgImage = BitmapConverter.ToMat(bgImageList[i]);

        //            Mat srcTopLeft = new Mat(image, new Rect(left, top, width, height));
        //            Mat backgroundTopLeft = new Mat(bgImage, new Rect(left, top, width, height));
        //            Mat srcTopRight = new Mat(image, new Rect(image.Width - left - width, top, width, height));
        //            Mat backgroundTopRight = new Mat(bgImage, new Rect(image.Width - left - width, top, width, height));

        //            if (Cv2.Norm(srcTopLeft, backgroundTopLeft) >= imageDiffLimit
        //                    || Cv2.Norm(srcTopRight, backgroundTopRight) >= imageDiffLimit)
        //            {
        //                if (--badCountLimit == 0)
        //                    return false;
        //            }
        //        }

        //        return true;
        //    }
        //    catch
        //    {
        //    }

        //    return result;
        //}

        public static void ContrastEnhancement(ref Bitmap src, out Bitmap dst, double brightness = 3, double constrast = 50)
        {
            dst = null;
            Mat img = BitmapConverter.ToMat(src);

            Mat imgLab = new Mat(new OpenCvSharp.Size(img.Width, img.Height), MatType.CV_8UC3, 0);

            Cv2.CvtColor(img, imgLab, ColorConversionCodes.LBGR2Lab);

            Scalar mean, std;
            Cv2.MeanStdDev(imgLab, out mean, out std);
            //Console.WriteLine("average L: {0}", mean.Val0);

            Mat[] channels = Cv2.Split(imgLab);

            //L_Img = (LAB_Img(:,:, 1) - F_avg + B_value)./ (1 - C_value / 100) + F_avg;
            channels[0] = ((channels[0] /255.0 * 100 - mean.Val0 /255.0 * 100 + brightness) / (1.0 - constrast / 100) + mean.Val0 / 255.0 * 100 ) /100 * 255;
            Cv2.Merge(channels, imgLab);
            Cv2.CvtColor(imgLab, img, ColorConversionCodes.Lab2LBGR);
            dst = BitmapConverter.ToBitmap(img);
        }

        public static void LevelAdjustment(ref Bitmap src, out Bitmap dst, double gmin = 0, double gmax = 255, double gamma = 1.5, double fmin = 5, double fmax = 200)
        {
            dst = null;
            double alpha = Math.Pow((gmax - gmin), (1.0 - 1.0 / gamma));

            Mat img = BitmapConverter.ToMat(src);

            Mat imgLab = new Mat(new OpenCvSharp.Size(img.Width, img.Height), MatType.CV_8UC3, 0);

            Cv2.CvtColor(img, imgLab, ColorConversionCodes.LBGR2Lab);

            Mat[] channels = Cv2.Split(imgLab);

            CorrectGamma(gmin, gmax, gamma, fmin, fmax);

            channels[0] = channels[0] / 255.0 * 100;
            Cv2.LUT(channels[0], lut, channels[0]);
            channels[0] = channels[0] / 100.0 * 255;
            Cv2.Merge(channels, imgLab);
            Cv2.CvtColor(imgLab, img, ColorConversionCodes.Lab2LBGR);
            dst = BitmapConverter.ToBitmap(img);

        }

        public static void CorrectGamma(double gmin = 0, double gmax = 255, double gamma = 1.5, double fmin = 5, double fmax = 200)
        {
            if(_initLUT)
            {
                if(gmin != _gmin || gmax != _gmax || fmin != _fmin || fmax != _fmax || gamma != _gamma)
                {
                    _gmin = gmin;
                    _gmax = gmax;
                    _fmin = fmin;
                    _fmax = fmax;
                    _gamma = gamma;

                } else
                {
                    return;
                }
            }
            double alpha = Math.Pow((gmax - gmin), (1.0 - 1.0 / gamma));
            for (int i = 0; i < lut.Length; i++)
            {
                //L_Img = abs(G_min + alaph.*(LAB_Img(:,:,1)-F_min*(G_max-G_min)/(F_max-F_min)).^(1/gamma));
                //channels[0] = Math.Abs(gmin + Math.Pow(alpha * ((channels[0] / 255.0 * 100) - (fmin * (gmax - gmin) / (fmax - fmin))), 1.0 / gamma));
                lut[i] = (byte)Math.Abs(gmin + alpha * Math.Pow((i - fmin) * (gmax - gmin) / (fmax - fmin), 1.0 / gamma));
            }
            _initLUT = true;
        }

        public static void CalBlobCenter(ref Bitmap src, out double x, out double y, bool invert)
        {
            x = -1;
            y = -1;
            Mat img = BitmapConverter.ToMat(src);
            Mat imgGray = new Mat(new OpenCvSharp.Size(img.Width, img.Height), MatType.CV_8UC1, 0);
            if (invert)
            {
                imgGray = Scalar.All(255) - imgGray;
            }
            Mat imgThr = new Mat(new OpenCvSharp.Size(img.Width, img.Height), MatType.CV_8UC1, 0); 

            Cv2.CvtColor(img, imgGray, ColorConversionCodes.BGR2GRAY);
            Cv2.Threshold(imgGray, imgThr, 180, 255, ThresholdTypes.Binary);

            Moments m = Cv2.Moments(imgThr, true);
            x = m.M10 / m.M00;
            y = m.M01 / m.M00;
        }

        public static void CalVerticalHorizontalLinePosition(ref Bitmap src, LineType lineType, out double position, double threshold = 200)
        {
            position = 0;
            Mat img = BitmapConverter.ToMat(src);
            Mat imgGray = new Mat(new OpenCvSharp.Size(lineType == LineType.Horizontal ? img.Width : 1, lineType == LineType.Horizontal ? 1 : img.Height), MatType.CV_8UC1, 0);
            Cv2.CvtColor(img, imgGray, ColorConversionCodes.BGR2GRAY);

            Mat res = new Mat(new OpenCvSharp.Size(lineType == LineType.Horizontal ? img.Width : 1, lineType == LineType.Horizontal ? 1 : img.Height), MatType.CV_8UC1, 0);
            Cv2.Reduce(imgGray, res, ReduceDimension.Row, ReduceTypes.Avg, -1);
            if(lineType == LineType.Horizontal)
            {
                for(int i = 50; i < res.Cols; i++)
                {
                    int count = 0;
                    byte e = res.Get<byte>(0, i);
                    if(e <= 255 - threshold)
                    {
                        for(int k = 1; k <= 5; k++)
                        {
                            if (i + k < res.Cols && res.Get<byte>(0, i + k) <= 255 - threshold)
                            {
                                count++;
                            }
                        }

                        if (count >= 5)
                        {
                            position = i;
                            break;
                        }
                    }
                }
            }
        }

        //public static void CalVerticalDiamondTopPosition(ref Bitmap src, LineType lineType, out double position, double threshold1 = 200, double threshold2 = 250)
        //{
        //    position = 0;
        //    Mat img = BitmapConverter.ToMat(src);
        //    Mat imgGray = new Mat(new OpenCvSharp.Size(lineType == LineType.Horizontal ? img.Width : 1, lineType == LineType.Horizontal ? 1 : img.Height), MatType.CV_8UC1, 0);
        //    Cv2.CvtColor(img, imgGray, ColorConversionCodes.BGR2GRAY);
        //    //Cv2.Threshold(imgGray, imgGray, 200, threshold1, ThresholdTypes.Binary);
        //    Mat res = new Mat(new OpenCvSharp.Size(lineType == LineType.Horizontal ? img.Width : 1, lineType == LineType.Horizontal ? 1 : img.Height), MatType.CV_8UC1, 0);
        //    Cv2.Reduce(imgGray, res, ReduceDimension.Row, ReduceTypes.Avg, -1);
        //    if (lineType == LineType.Horizontal)
        //    {
        //        for (int i = 0; i < res.Cols; i++)
        //        {
        //            int count = 0;
        //            byte e = res.Get<byte>(0, i);
        //            if (e >= threshold2)
        //            {
        //                for(int k = 1; k <= 5; k++)
        //                {
        //                    if(i+k < res.Cols && res.Get<byte>(0, i+k) >= threshold2)
        //                    {
        //                        count++;
        //                    }
        //                }
        //                if (count >= 5)
        //                {
        //                    position = i;
        //                    break;
        //                }
        //            }
        //        }
        //    }
        //}

        public static void CalVerticalDiamondTopPosition(ref Bitmap src, LineType lineType, out double top, out double left, out double right, bool calSide = false, double threshold1 = 200, double threshold2 = 250)
        {
            top = 0;
            right = 0;
            left = 0;
            Mat img = BitmapConverter.ToMat(src);
            
            Mat imgGray = new Mat(new OpenCvSharp.Size(img.Width, img.Height), MatType.CV_8UC1, 0);
            Cv2.CvtColor(img, imgGray, ColorConversionCodes.BGR2GRAY);
            Rect myROI = calSide ? new Rect(50, 0, (int)(img.Width * 0.75), img.Height) : new Rect(0, 0, (int)(img.Width * 0.75), img.Height);
            Mat imgCrop = new Mat(imgGray, myROI);

            Cv2.Threshold(imgCrop, imgCrop, 178, 255, ThresholdTypes.Binary);
            if (!calSide)
            {
                Mat resRow = new Mat(new OpenCvSharp.Size(imgCrop.Width, 1), MatType.CV_32FC1, 0);
                Cv2.Reduce(imgCrop, resRow, ReduceDimension.Row, ReduceTypes.Sum, MatType.CV_32FC1);
                for (int i = 0; i < resRow.Cols; i++)
                {
                    int count = 0;
                    float e = resRow.Get<float>(0, i);
                    if (e >= 255 * (imgCrop.Height -3))
                    {
                        for (int k = 1; k <= 5; k++)
                        {
                            if (i + k < resRow.Cols && resRow.Get<float>(0, i + k) >= 255 * (imgCrop.Height - 3))
                            {
                                count++;
                            }
                        }
                        if (count >= 5)
                        {
                            top = i;
                            break;
                        }
                    }
                }
            }
            else
            {
                Mat resCol = new Mat(new OpenCvSharp.Size(1, imgCrop.Height), MatType.CV_32FC1, 0);
                Cv2.Reduce(imgCrop, resCol, ReduceDimension.Column, ReduceTypes.Sum, MatType.CV_32FC1);
                for (int i = 0; i < resCol.Rows; i++)
                {
                    int count = 0;
                    float e = resCol.Get<float>(i, 0);
                    if (e <= 255 * (imgCrop.Width - 3))
                    {
                        for (int k = 1; k <= 5; k++)
                        {
                            if (i + k < resCol.Rows && resCol.Get<float>(i + k, 0) <= 255 * (imgCrop.Width - 3))
                            {
                                count++;
                            }
                        }
                        if (count >= 5)
                        {
                            left = i;
                            break;
                        }
                    }
                }

                for (int i = (int)left; i < resCol.Rows; i++)
                {
                    int count = 0;
                    float e = resCol.Get<float>(i, 0);
                    if (e >= 255 * (imgCrop.Width - 3))
                    {
                        for (int k = 1; k <= 5; k++)
                        {
                            if (i + k < resCol.Rows && resCol.Get<float>(i + k, 0) >= 255 * (imgCrop.Width - 3))
                            {
                                count++;
                            }
                        }
                        if (count >= 5)
                        {
                            right = i;
                            break;
                        }
                    }
                }
            }
        }

        static public bool CreateGrabCut(ref System.Drawing.Bitmap src, ref System.Drawing.Bitmap srcBG, out System.Drawing.Bitmap dst, bool displayWindows = false)
        {
            dst = null;
            Mat srcImg = BitmapConverter.ToMat(src);
            Mat srcRef = BitmapConverter.ToMat(srcBG);
            Cv2.CvtColor(srcImg, srcImg, ColorConversionCodes.BGRA2BGR);
            Cv2.CvtColor(srcRef, srcRef, ColorConversionCodes.BGRA2BGR);

            Mat s = new Mat();
            Cv2.Subtract(srcImg, srcRef, s);
            Cv2.Threshold(s, s, 10, 255, ThresholdTypes.Binary);
            Mat mask = new Mat(new OpenCvSharp.Size(src.Width, src.Height), MatType.CV_8UC1, 0);

            //dilate process 
            //Cv2.Dilate(srcImg, dstImg, new Mat());

            //grabcut
            //Mat bgdModel = new Mat(new OpenCvSharp.Size(65, 1), MatType.CV_64FC1);
            //Mat fgdModel = new Mat(new OpenCvSharp.Size(65, 1), MatType.CV_64FC1);

            Mat bgdModel = new Mat();
            Mat fgdModel = new Mat();

            OpenCvSharp.Rect r = new OpenCvSharp.Rect((src.Width - 1080) / 2, (src.Height - 1080) / 2, 1080, 1080);
            Cv2.GrabCut(s, mask, r, bgdModel, fgdModel, 2, GrabCutModes.InitWithRect);

            //for (int i = mask.Cols / 2 - 50; i < mask.Cols / 2 + 50; i++)
            //{
            //    for (int j = mask.Rows / 2 - 25; j < mask.Rows / 2 + 75; j++)
            //    {
            //        mask.Set<byte>(j, i, 1);
            //    }
            //}

            //Cv2.GrabCut(srcImg, mask, r, bgdModel, fgdModel, 1, GrabCutModes.InitWithMask);

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
            Mat res = new Mat();

            Cv2.CvtColor(mask, res, ColorConversionCodes.GRAY2BGR);

            dst = BitmapConverter.ToBitmap(res);

            return true;
        }

        public static void AdjustMask(ref System.Drawing.Bitmap mask)
        {
            Mat img = BitmapConverter.ToMat(mask);

            Mat imgGray = new Mat(new OpenCvSharp.Size(img.Width, img.Height), MatType.CV_8UC1, 0);
            Cv2.CvtColor(img, imgGray, ColorConversionCodes.BGR2GRAY);

            Cv2.Threshold(imgGray, imgGray, 60, 255, ThresholdTypes.Binary);
            mask = BitmapConverter.ToBitmap(imgGray);
        }
    }

}