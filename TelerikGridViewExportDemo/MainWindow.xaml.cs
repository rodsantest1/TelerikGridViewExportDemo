using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.GridView;
using Telerik.Windows.Data;
using Telerik.Windows.Documents.Spreadsheet.FormatProviders.OpenXml.Xlsx;
using Telerik.Windows.Documents.Spreadsheet.FormatProviders.Pdf;
using Telerik.Windows.Documents.Spreadsheet.Model;

namespace TelerikGridViewExportDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            GridView1.ElementExportingToDocument += GridView1_ElementExportingToDocument;

            //GridView1.SelectionChanged += GridView1_SelectionChanged;
            //GridView1.SelectionChanging += GridView1_SelectionChanging;
            //GridView1.RowActivated += GridView1_RowActivated;

            GridView1.AddHandler(RadGridView.MouseLeftButtonUpEvent, new MouseButtonEventHandler(GridView1_MouseLeftButtonUp), true);
        }

        private void GridView1_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var clickedElement = (FrameworkElement)e.OriginalSource;
            var clickedRow = clickedElement.ParentOfType<GridViewRow>();
            if (clickedRow != null)
            {
                WeatherForecast weather = clickedRow.Item as WeatherForecast;
                MessageBox.Show(weather.Summary);
            }
        }

        private void GridView1_RowActivated(object sender, Telerik.Windows.Controls.GridView.RowEventArgs e) { TestHandler(); }
        private void GridView1_SelectionChanging(object sender, SelectionChangingEventArgs e) { TestHandler(); }
        private void GridView1_SelectionChanged(object sender, SelectionChangeEventArgs e) { TestHandler(); }

        private void TestHandler([CallerMemberName] string caller = "")
        {
            MessageBox.Show(caller);
        }
        private void UnselectButton_Click(object sender, RoutedEventArgs e)
        {
            GridView1.SelectedItem = null;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            IEnumerable<WeatherForecast> data =  GetJsonStringSampleData(); //GetSampleData();

            GridView1.ItemsSource = data;
        }
        private static IEnumerable<WeatherForecast> GetSampleData()
        {
            var list = new List<WeatherForecast>()
            {
                new WeatherForecast() { Summary="rschilly"}
            };
            return list;
        }

        private static IEnumerable<WeatherForecast> GetJsonStringSampleData()
        {
            var content = "[" +
                "{ \"break\":\"true\", \"lunch\":\"2:00:00\", \"temperatureC\":11,\"temperatureF\":51,\"summary\":\"Chilly\"}," +
                "{ \"break\":\"false\", \"lunch\":\"5:00:00\", \"temperatureC\":20,\"temperatureF\":67,\"summary\":\"Scorching\"}," +
                "{ \"break\":\"false\", \"lunch\":\"4:00:00\", \"temperatureC\":-4,\"temperatureF\":25,\"summary\":\"Freezing\"}," +
                "{ \"break\":\"true\", \"lunch\":\"3:00:00\", \"temperatureC\":-10,\"temperatureF\":15,\"summary\":\"Warm\"}," +
                "{ \"break\":\"true\", \"lunch\":\"1:00:00\", \"temperatureC\":-10,\"temperatureF\":15,\"summary\":\"Cool\"}]";
            var data = JsonConvert.DeserializeObject<IEnumerable<WeatherForecast>>(content);
            return data;
        }

        //https://docs.telerik.com/devtools/wpf/controls/radgridview/export/how-to/column-of-cell
        private void GridView1_ElementExportingToDocument(object sender, GridViewElementExportingToDocumentEventArgs e)
        {
            if (e.Element == ExportElement.Cell)
            {
                var cellExportingArgs = e as GridViewCellExportingEventArgs;
                if (cellExportingArgs.Column == this.GridView1.Columns[2])
                {
                    (cellExportingArgs.VisualParameters as GridViewDocumentVisualExportParameters).Style = new Telerik.Windows.Controls.GridView.CellSelectionStyle() { IsBold = true };
                }
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var workbook = DesignWorkbook();

            var xlsFormatProvider = new XlsxFormatProvider();
            var pdfFormatProvider = new PdfFormatProvider();

            var pdfFileName = @"C:\temp\test.pdf";
            var xlsFileName = @"C:\temp\test.xlsx";

            using (Stream output = new FileStream(xlsFileName, FileMode.Create))
            {
                xlsFormatProvider.Export(workbook, output);
            }

            using (Stream output = new FileStream(pdfFileName, FileMode.Create))
            {
                pdfFormatProvider.Export(workbook, output);
            }

            MessageBox.Show($"Exports completed normally on {DateTime.Now}");
        }

        private Workbook DesignWorkbook()
        {
            var workbook = GridView1.ExportToWorkbook();
            // start
            var worksheet = workbook.ActiveWorksheet;


            worksheet.Rows[0].SetIsWrapped(true);

            worksheet.Columns[0, 6].AutoFitWidth();

            // finish
            return workbook;
        }
    }

    public class WeatherForecast
    {
        public string Break { get; set; }

        public TimeSpan Lunch { get; set; }

        public int TemperatureC { get; set; }

        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        public string Summary { get; set; }
    }

    public class CustomFunction : AggregateFunction<WeatherForecast, double>
    {
        public CustomFunction()
        {
            this.AggregationExpression = items => Count(items);
        }

        private double Count(IEnumerable<WeatherForecast> source)
        {
            var itemCount = source.Count();
            if (itemCount > 0)
            {
                var values = source.Where(i => i.Break?.ToLower() == "true");

                return values.Count();
            }

            return 0;
        }
    }

    public class CustomTsAvgFunction : AggregateFunction<WeatherForecast, TimeSpan>
    {
        public CustomTsAvgFunction()
        {
            this.AggregationExpression = items => Average(items);
        }

        private TimeSpan Average(IEnumerable<WeatherForecast> source)
        {
            var intRes = source.Average(x => x.Lunch.TotalMinutes);
            return TimeSpan.FromMinutes(intRes);
        }
    }

    [ValueConversion(typeof(bool), typeof(string))]
    public class BooleanToYesNo : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToBoolean(value) ? "Yes" : "No";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //return null;
            throw new NotImplementedException();
            //return FilterDescriptor.UnsetValue;

        }
    }
}
