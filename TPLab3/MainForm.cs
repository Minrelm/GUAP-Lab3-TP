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
using System.Windows.Forms.DataVisualization.Charting;

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
            chart1.ChartAreas[0].AxisX.Interval = 1;
            chart1.ChartAreas[0].AxisX.Title = "Год";
            chart1.ChartAreas[0].AxisY.Title = "Цена за 1 кв.м";
            chart1.ChartAreas[0].AxisX.LabelStyle.Angle = -45;

            string[] headers = lines[0].Split(',');

            for (int col = 1; col < headers.Length; col++)
            {
                var series = chart1.Series.Add(headers[col]);
                series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
                series.BorderWidth = 2;
                series.LegendText = headers[col];

                for (int row = 1; row < lines.Length; row++)
                {
                    string[] values = lines[row].Split(',');
                    if (int.TryParse(values[0], out int year) && double.TryParse(values[col], out double price))
                    {
                        series.Points.AddXY(year, price);
                    }
                }
            }
        }

        private void AnalyzeTrends(string[] lines)
        {
            listBox1.Items.Clear();
            string[] headers = lines[0].Split(',');
            string[] first = lines[1].Split(',');
            string[] last = lines[lines.Length - 1].Split(',');

            double maxPercent = double.MinValue;
            double minPercent = double.MaxValue;
            string mostIncreased = "";
            string mostDecreased = "";

            for (int i = 1; i < headers.Length; i++)
            {
                if (double.TryParse(first[i], out double firstVal) && double.TryParse(last[i], out double lastVal) && firstVal != 0)
                {
                    double absDiff = lastVal - firstVal;
                    double percentDiff = absDiff / firstVal * 100;

                    if (percentDiff > maxPercent)
                    {
                        maxPercent = percentDiff;
                        mostIncreased = $"{headers[i]} (+{absDiff:F2}, +{percentDiff:F1}%)";
                    }

                    if (percentDiff < minPercent)
                    {
                        minPercent = percentDiff;
                        mostDecreased = $"{headers[i]} ({absDiff:F2}, {percentDiff:F1}%)";
                    }
                }
            }

            listBox1.Items.Add($"Сильнее всего подорожали: {mostIncreased}");
            listBox1.Items.Add($"Сильнее всего подешевели: {mostDecreased}");
        }

        private void ForecastPrices(string[] lines, int yearsToForecast)
        {
            // Удаляем старые прогнозные серии
            foreach (var s in chart1.Series.Cast<Series>().Where(s => s.Name.Contains("(прогноз)")).ToList())
            {
                chart1.Series.Remove(s);
            }

            string[] headers = lines[0].Split(',');
            int smoothing = (int)numericUpDown1.Value;

            chart1.ChartAreas[0].RecalculateAxesScale();
            chart1.ChartAreas[0].AxisX.Title = "Год";
            chart1.ChartAreas[0].AxisY.Title = "Цена за 1 кв.м";

            double globalMin = double.MaxValue;
            double globalMax = double.MinValue;

            for (int col = 1; col < headers.Length; col++)
            {
                List<double> prices = lines.Skip(1).Select(l => double.Parse(l.Split(',')[col])).ToList();
                List<int> years = lines.Skip(1).Select(l => int.Parse(l.Split(',')[0])).ToList();

                if (smoothing <= 0 || smoothing > prices.Count)
                {
                    MessageBox.Show($"Некорректное значение сглаживания для {headers[col]}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    continue;
                }

                var forecastSeries = new Series
                {
                    Name = $"Прогноз {col}",
                    ChartType = SeriesChartType.Line,
                    BorderDashStyle = ChartDashStyle.Dash,
                    Color = Color.Red,
                    BorderWidth = 2,
                    LegendText = $"Прогноз {col}"
                };

                // Добавим последнюю известную точку
                forecastSeries.Points.AddXY(years.Last(), prices.Last());

                // Прогнозирование
                int lastYear = years.Last();
                for (int i = 0; i < yearsToForecast; i++)
                {
                    double avg = prices.Skip(prices.Count - smoothing).Take(smoothing).Average();
                    prices.Add(avg);
                    int nextYear = lastYear + i + 1;
                    forecastSeries.Points.AddXY(nextYear, avg);
                }

                chart1.Series.Add(forecastSeries);

                // Обновим глобальные мин/макс для масштабирования осей
                globalMin = Math.Min(globalMin, prices.Min());
                globalMax = Math.Max(globalMax, prices.Max());
            }

            // Красивое масштабирование оси Y
            double padding = (globalMax - globalMin) * 0.1;
            chart1.ChartAreas[0].AxisY.Minimum = Math.Floor(globalMin - padding);
            chart1.ChartAreas[0].AxisY.Maximum = Math.Ceiling(globalMax + padding);
            chart1.ChartAreas[0].AxisY.Interval = Math.Ceiling((chart1.ChartAreas[0].AxisY.Maximum - chart1.ChartAreas[0].AxisY.Minimum) / 10);
            chart1.ChartAreas[0].AxisY.IsStartedFromZero = false;

            // Повернём подписи оси X
            chart1.ChartAreas[0].AxisX.Interval = 1;
            chart1.ChartAreas[0].AxisX.LabelStyle.Angle = -45;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentFilePath) || string.IsNullOrWhiteSpace(numericUpDown1.Text))
            {
                MessageBox.Show("Сначала выберите файл и введите количество лет для прогноза.");
                return;
            }

            if (!int.TryParse(numericUpDown1.Text, out int yearsToForecast) || yearsToForecast <= 0)
            {
                MessageBox.Show("Введите корректное количество лет (положительное число).");
                return;
            }

            string[] lines = File.ReadAllLines(currentFilePath);
            ForecastPrices(lines, yearsToForecast);
        }
    }
}
