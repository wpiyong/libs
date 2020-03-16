using Microsoft.Research.DynamicDataDisplay.DataSources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PlotterLib
{
    public class PlotViewModel
    {
        public ObservableDataSource<Point> Data { get; set; }
        public ObservableDataSource<Point> SmoothData { get; set; }

        public PlotViewModel(List<Point> points)
        {
            Data = new ObservableDataSource<Point>(points);
            SmoothData = new ObservableDataSource<Point>();
        }

        public void AddSmoothData(List<Point> pts)
        {
            SmoothData.Collection.Clear();
            foreach(var pt in pts)
                SmoothData.Collection.Add(pt);
        }
    }
}
