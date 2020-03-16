using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace ImageProcessorLib
{
    public class Boundary
    {
        protected string[][] data { get; set; }
        private string[][] round_data { get; set; }
        private string[][] fancy_data { get; set; }
        private string[][] fancyHH_data { get; set; }

        protected string FileName { get; set; }
        protected string _fileName = "Boundary.csv";

        public Boundary()
        {
            FileName = _fileName;
            //var lines = new List<string[]>();
            //StreamReader BoundaryReader = new StreamReader(File.OpenRead(_fileName));
            //string[] line = BoundaryReader.ReadLine().Replace("\"", "").Split(',');
            //while (!BoundaryReader.EndOfStream)
            //{
            //    line = BoundaryReader.ReadLine().Replace("\"", "").Split(',');
            //    lines.Add(line);
            //}
            //data = lines.ToArray();

            string currentDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);

            _fileName = currentDirectory + "\\" + FileName;

            var round_lines = new List<string[]>();
            var fancy_lines = new List<string[]>();
            var fancyHH_lines = new List<string[]>();

            StreamReader BoundaryReader = new StreamReader(File.OpenRead(_fileName));
            string[] line = BoundaryReader.ReadLine().Replace("\"", "").Split(',');
            while (!BoundaryReader.EndOfStream)
            {
                line = BoundaryReader.ReadLine().Replace("\"", "").Split(',');

                if (line[9] == "Round")
                {
                    round_lines.Add(line);
                }
                else if (line[9] == "Fancy")
                {
                    fancy_lines.Add(line);
                }
                else if (line[9] == "FancyHH")
                {
                    fancyHH_lines.Add(line);
                }
            }
            round_data = round_lines.ToArray();
            fancy_data = fancy_lines.ToArray();
            fancyHH_data = fancyHH_lines.ToArray();
        }

        public Boundary( string file)
        {
            FileName = file;
        }

        public string GetMD5HashFromFile()
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(_fileName))
                {
                    return BitConverter.ToString(md5.ComputeHash(stream));
                }
            }
        }

        public virtual Dictionary<string, string> GetGrade_shifting(double H, double C, double L, double shift_C)
        {
            string grade, refer, hue;
            double hue_min, hue_max, chroma_min, chroma_max, lightness_min, lightness_max;
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            dictionary.Add("C_description", "N/A");
            dictionary.Add("Refer", null);
            dictionary.Add("Version", data[0][8]);
            dictionary.Add("H_description", null);
            dictionary.Add("L_description", null);
            for (int k = 0; k < data.GetLength(0); k++)
            {
                hue_min = double.Parse(data[k][0]);
                hue_max = double.Parse(data[k][1]);
                chroma_min = double.Parse(data[k][2]);
                chroma_max = double.Parse(data[k][3]);
                lightness_min = double.Parse(data[k][4]);
                lightness_max = double.Parse(data[k][5]);

                if (data[k][6] == "D")
                {
                    if (H >= hue_min & H < hue_max & C >= (chroma_min) & C < (chroma_max + shift_C) & L >= lightness_min & L < lightness_max)
                    {
                        grade = data[k][6];
                        refer = data[k][7];
                        hue = data[k][10];
                        dictionary["C_description"] = grade;
                        dictionary["Refer"] = refer;
                        dictionary["H_description"] = hue;
                        return dictionary;
                    }
                }
                else
                {
                    if (H >= hue_min & H < hue_max & C >= (chroma_min + shift_C) & C < (chroma_max + shift_C) & L >= lightness_min & L < lightness_max)
                    {
                        grade = data[k][6];
                        refer = data[k][7];
                        hue = data[k][10];
                        dictionary["C_description"] = grade;
                        dictionary["Refer"] = refer;
                        dictionary["H_description"] = hue;
                        return dictionary;
                    }
                }
            }
            return dictionary;
        }

        public virtual Dictionary<string, string> GetGrade(double H, double C, double L, int diamond_grouping = -1)
        {
            string[][] data;

            if (diamond_grouping >= 0 && diamond_grouping <= 2)
            {
                data = round_data;
            }
            else
            {
                data = fancy_data;
            }

            string grade, refer, hue;
            double hue_min, hue_max, chroma_min, chroma_max, lightness_min, lightness_max;
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            dictionary.Add("C_description", "N/A");
            dictionary.Add("Refer", null);
            dictionary.Add("Version", null);
            if (data.Length != 0)
            {
                dictionary["Version"] = data[0][8];
            }
            dictionary.Add("H_description", null);
            dictionary.Add("L_description", null);

            for (int k = 0; k < data.GetLength(0); k++)
            {
                hue_min = double.Parse(data[k][0]);
                hue_max = double.Parse(data[k][1]);
                chroma_min = double.Parse(data[k][2]);
                chroma_max = double.Parse(data[k][3]);
                lightness_min = double.Parse(data[k][4]);
                lightness_max = double.Parse(data[k][5]);
                if (H >= hue_min & H < hue_max & C >= chroma_min & C < chroma_max & L >= lightness_min & L < lightness_max)
                {
                    grade = data[k][6];
                    refer = data[k][7];
                    hue = data[k][10];
                    dictionary["C_description"] = grade;
                    dictionary["Refer"] = refer;
                    dictionary["H_description"] = hue;
                    return dictionary;
                }
            }
            return dictionary;
        }
    }

    public class Boundary_FL : Boundary
    {

        public Boundary_FL(string file = "Boundary_FL.csv") : base(file)
        {
            string currentDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);

            _fileName = currentDirectory + "\\" + FileName;

            var data_lines = new List<string[]>();

            StreamReader BoundaryReader = new StreamReader(File.OpenRead(_fileName));
            string[] line = BoundaryReader.ReadLine().Replace("\"", "").Split(',');
            while (!BoundaryReader.EndOfStream)
            {
                line = BoundaryReader.ReadLine().Replace("\"", "").Split(',');

                data_lines.Add(line);
            }
            data = data_lines.ToArray();
        }

        public override Dictionary<string, string> GetGrade(double H, double C, double L, int diamond_grouping = -1)
        {
            string grade, refer, hue;
            double hue_min, hue_max, chroma_min, chroma_max, lightness_min, lightness_max;
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            dictionary.Add("C_description", null);
            dictionary.Add("Refer", null);
            dictionary.Add("Version", null);
            if (data.Length != 0)
            {
                dictionary["Version"] = data[0][9];
            }
            dictionary.Add("H_description", null);
            dictionary.Add("L_description", "N/A");
            dictionary.Add("Multi_Color", "FALSE");

            for (int k = 0; k < data.GetLength(0); k++)
            {
                hue_min = double.Parse(data[k][0]);
                hue_max = double.Parse(data[k][1]);
                chroma_min = double.Parse(data[k][2]);
                chroma_max = double.Parse(data[k][3]);
                lightness_min = double.Parse(data[k][4]);
                lightness_max = double.Parse(data[k][5]);
                if ((H >= hue_min) && (H < hue_max)
                     && (C >= chroma_min) && (C < chroma_max) && (L >= lightness_min) && (L < lightness_max))
                {
                    grade = data[k][6];
                    refer = data[k][8];
                    hue = data[k][7];
                    if (data[k].Length > 11)
                        dictionary["Multi_Color"] = data[k][11];

                    dictionary["L_description"] = grade;
                    dictionary["Refer"] = refer;
                    dictionary["H_description"] = hue;
                    return dictionary;
                }
            }
            return dictionary;
        }
    }

    public class Boundary_N3Imager : Boundary
    {
        public List<List<string>> N3FL_data = new List<List<string>>();
        public List<List<string>> N3PHOS_data = new List<List<string>>();
        public List<List<string>> N3UVFL_data = new List<List<string>>();
        public Boundary_N3Imager()
        {
            readN3FL("N3FL_Boundary.csv");
            readN3PHOS("N3PHOS_Boundary.csv");
            readN3UVFL("N3UVFL_Boundary.csv");

        }

        void readN3FL(string fileName)
        {
            string currentDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);

            fileName = currentDirectory + "\\" + fileName;

            var line_data = new List<string[]>();

            StreamReader BoundaryReader = new StreamReader(File.OpenRead(fileName));
            string[] line = BoundaryReader.ReadLine().Replace("\"", "").Split(',');
            while (!BoundaryReader.EndOfStream)
            {
                line = BoundaryReader.ReadLine().Replace("\"", "").Split(',');

                N3FL_data.Add(new List<string>(line));
            }
        }

        void readN3PHOS(string fileName)
        {
            string currentDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);

            fileName = currentDirectory + "\\" + fileName;

            var line_data = new List<string[]>();

            StreamReader BoundaryReader = new StreamReader(File.OpenRead(fileName));
            string[] line = BoundaryReader.ReadLine().Replace("\"", "").Split(',');
            while (!BoundaryReader.EndOfStream)
            {
                line = BoundaryReader.ReadLine().Replace("\"", "").Split(',');

                N3PHOS_data.Add(new List<string>(line));
            }
        }

        void readN3UVFL(string fileName)
        {
            string currentDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);

            fileName = currentDirectory + "\\" + fileName;

            var line_data = new List<string[]>();

            StreamReader BoundaryReader = new StreamReader(File.OpenRead(fileName));
            string[] line = BoundaryReader.ReadLine().Replace("\"", "").Split(',');
            while (!BoundaryReader.EndOfStream)
            {
                line = BoundaryReader.ReadLine().Replace("\"", "").Split(',');

                N3UVFL_data.Add(new List<string>(line));
            }
        }
    }
}