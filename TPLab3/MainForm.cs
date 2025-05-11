using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TPLab3
{
    public partial class MainForm : Form
    {
        private string currentFilePath;

        public MainForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "CSV files (*.csv)|*.csv";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                currentFilePath = openFileDialog.FileName;
                string[] lines = File.ReadAllLines(currentFilePath);
                LoadDataToGrid(lines);
                PlotChart(lines);
                AnalyzeTrends(lines);
            }
        }

        private void LoadDataToGrid(string[] lines)
        {
            dataGridView1.Columns.Clear();
            string[] headers = lines[0].Split(',');
            foreach (string header in headers)
            {
                dataGridView1.Columns.Add(header, header);
            }

            for (int i = 1; i < lines.Length; i++)
            {
                dataGridView1.Rows.Add(lines[i].Split(','));
            }
        }

        private void PlotChart(string[] lines)
        {
            chart1.Series.Clear();
            string[] headers = lines[0].Split(',');

            for (int col = 1; col < headers.Length; col++)
            {
                var series = chart1.Series.Add(headers[col]);
                series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;

                for (int row = 1; row < lines.Length; row++)
                {
                    string[] values = lines[row].Split(',');
                    int year = int.Parse(values[0]);
                    double price = double.Parse(values[col]);
                    series.Points.AddXY(year, price);
                }
            }
        }

        private void AnalyzeTrends(string[] lines)
        {
            listBox1.Items.Clear();
            string[] headers = lines[0].Split(',');
            string[] first = lines[1].Split(',');
            string[] last = lines[lines.Length - 1].Split(',');

            double maxDiff = double.MinValue;
            double minDiff = double.MaxValue;
            string mostIncreased = "";
            string mostDecreased = "";

            for (int i = 1; i < headers.Length; i++)
            {
                double diff = double.Parse(last[i]) - double.Parse(first[i]);

                if (diff > maxDiff)
                {
                    maxDiff = diff;
                    mostIncreased = headers[i];
                }

                if (diff < minDiff)
                {
                    minDiff = diff;
                    mostDecreased = headers[i];
                }
            }

            listBox1.Items.Add($"Сильнее всего подорожали: {mostIncreased} (+{maxDiff})");
            listBox1.Items.Add($"Сильнее всего подешевели: {mostDecreased} ({minDiff})");
        }

        private void ForecastPrices(string[] lines, int yearsToForecast)
        {
            string[] headers = lines[0].Split(',');
            int years = lines.Length - 1;
            int windowSize = 3;

            for (int col = 1; col < headers.Length; col++)
            {
                var forecastSeries = chart1.Series.Add(headers[col] + " (прогноз)");
                forecastSeries.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
                forecastSeries.BorderDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dash;

                List<double> prices = lines.Skip(1).Select(l => double.Parse(l.Split(',')[col])).ToList();

                for (int i = 0; i < prices.Count; i++)
                {
                    forecastSeries.Points.AddXY(int.Parse(lines[i + 1].Split(',')[0]), prices[i]);
                }

                for (int i = 0; i < yearsToForecast; i++)
                {
                    double avg = prices.Skip(Math.Max(0, prices.Count - windowSize)).Take(windowSize).Average();
                    int lastYear = int.Parse(lines[lines.Length - 1].Split(',')[0]);
                    int nextYear = lastYear + i + 1;
                    forecastSeries.Points.AddXY(nextYear, avg);
                    prices.Add(avg);
                }
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem != null && !string.IsNullOrEmpty(currentFilePath))
            {
                int forecastYears = int.Parse(comboBox1.SelectedItem.ToString());
                string[] lines = File.ReadAllLines(currentFilePath);
                ForecastPrices(lines, forecastYears);
            }
        }
    }
}
