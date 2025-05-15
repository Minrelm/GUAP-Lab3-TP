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
using System.Diagnostics;

namespace TPLab3
{
    public partial class MainForm : Form
    {
        // Путь к текущему открытому CSV-файлу
        private string currentFilePath;

        private enum DataType
        {
            Housing,
            Inflation
        }

        private DataType selectedDataType;

        public MainForm()
        {
            InitializeComponent();
            numericPrice.Minimum = 0;
            numericPrice.Maximum = 1000000;
            numericPrice.Enabled = false;
            numericPrice.Visible = false;
            labelPrice.Enabled = false;
            labelPrice.Visible = false;
            comboMode.Items.Add("Цены на жильё");
            comboMode.Items.Add("Инфляция");
            comboMode.SelectedIndex = 0;
            selectedDataType = DataType.Housing;
            comboMode.SelectedIndexChanged += (s, e) =>
            {
                selectedDataType = (DataType)comboMode.SelectedIndex;
                bool IsInflation = selectedDataType == DataType.Inflation;
                numericPrice.Enabled = IsInflation;
                numericPrice.Value = 0;
                numericPrice.Visible = IsInflation;
                labelPrice.Enabled = IsInflation;
                labelPrice.Visible = IsInflation;
            };
        }

        // Обработчик кнопки "Загрузить файл"
        // Загружает CSV-файл, отображает его в таблице и строит график с анализом
        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv"
            };
            if (openFileDialog.ShowDialog() != DialogResult.OK)
                return;
            currentFilePath = openFileDialog.FileName;
            var lines = File.ReadAllLines(currentFilePath);
            switch (selectedDataType)
            {
                case DataType.Housing:
                    LoadDataToGrid(lines);
                    PlotChart(lines); AnalyzeTrends(lines);
                    break;
                case DataType.Inflation:
                    LoadDataToGrid(lines);
                    PlotChart(lines);
                    break;
            }
        }

        // Заполняет DataGridView значениями из CSV-файла
        // Предполагается, что первая строка содержит заголовки
        private void LoadDataToGrid(string[] lines)
        {
            dataGridView1.Columns.Clear();

            string[] headers = lines[0].Split(',');

            // Создание колонок по заголовкам
            foreach (string header in headers)
            {
                dataGridView1.Columns.Add(header, header);
            }

            // Добавление строк с данными, начиная со второй строки
            for (int i = 1; i < lines.Length; i++)
            {
                dataGridView1.Rows.Add(lines[i].Split(','));
            }
        }

        // Строит линейный график на основе значений из CSV
        // Ось X — годы, ось Y — цены
        private void PlotChart(string[] lines)
        {
            chart1.Series.Clear(); // Очищаем старые данные графика
            chart1.ChartAreas[0].AxisX.Interval = 1;
            chart1.ChartAreas[0].AxisX.Title = "Год";
            chart1.ChartAreas[0].AxisY.Title = "Цена за 1 кв.м";
            chart1.ChartAreas[0].AxisX.LabelStyle.Angle = -45;

            string[] headers = lines[0].Split(',');

            // Для каждого региона/категории создаём отдельную линию
            for (int col = 1; col < headers.Length; col++)
            {
                var series = chart1.Series.Add(headers[col]);
                series.ChartType = SeriesChartType.Line;
                series.BorderWidth = 2;
                series.LegendText = headers[col];

                // Добавляем значения по годам
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

        // Анализирует рост/падение цен между первым и последним годом
        // Результат — самый большой рост и самое большое падение
        private void AnalyzeTrends(string[] lines)
        {
            listBox1.Items.Clear();

            string[] headers = lines[0].Split(',');
            string[] first = lines[1].Split(',');                     // Первая запись (начальный год)
            string[] last = lines[lines.Length - 1].Split(',');      // Последняя запись (последний год)

            double maxPercent = double.MinValue;
            double minPercent = double.MaxValue;
            string mostIncreased = "";
            string mostDecreased = "";

            for (int i = 1; i < headers.Length; i++)
            {
                if (double.TryParse(first[i], out double firstVal) &&
                    double.TryParse(last[i], out double lastVal) &&
                    firstVal != 0)
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

            // Отображаем результат анализа
            listBox1.Items.Add($"Сильнее всего подорожали: {mostIncreased}");
            listBox1.Items.Add($"Сильнее всего подешевели: {mostDecreased}");
        }

        // Выполняет прогноз цен на заданное количество лет вперёд
        // Используется метод скользящего среднего
        private void ForecastPrices(string[] lines, int yearsToForecast)
        {
            // Удаляем предыдущие прогнозы (поиск по названию)
            foreach (var s in chart1.Series.Cast<Series>().Where(s => s.Name.Contains("(прогноз)")).ToList())
            {
                chart1.Series.Remove(s);
            }

            string[] headers = lines[0].Split(',');
            int smoothing = (int)numericUpDown1.Value; // Период сглаживания

            double globalMin = double.MaxValue;
            double globalMax = double.MinValue;

            for (int col = 1; col < headers.Length; col++)
            {
                // Получаем значения цен и соответствующие годы
                List<double> prices = lines.Skip(1).Select(l => double.Parse(l.Split(',')[col])).ToList();
                List<int> years = lines.Skip(1).Select(l => int.Parse(l.Split(',')[0])).ToList();

                // Проверка корректности параметра сглаживания
                if (smoothing <= 0 || smoothing > prices.Count)
                {
                    MessageBox.Show($"Некорректное значение сглаживания для {headers[col]}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    continue;
                }

                // Создание серии для прогноза
                var forecastSeries = new Series
                {
                    Name = $"Прогноз {col} (прогноз)",
                    ChartType = SeriesChartType.Line,
                    BorderDashStyle = ChartDashStyle.Dash, // Линия прогноза — пунктир
                    Color = Color.Red,
                    BorderWidth = 2,
                    LegendText = $"Прогноз {headers[col]}"
                };

                // Добавляем последнюю известную точку
                forecastSeries.Points.AddXY(years.Last(), prices.Last());

                int lastYear = years.Last();

                // Прогноз на N лет вперёд
                for (int i = 0; i < yearsToForecast; i++)
                {
                    double avg = prices.Skip(prices.Count - smoothing).Take(smoothing).Average(); // Скользящее среднее
                    prices.Add(avg);
                    int nextYear = lastYear + i + 1;
                    forecastSeries.Points.AddXY(nextYear, avg);
                }

                chart1.Series.Add(forecastSeries);

                // Обновляем диапазон значений оси Y
                globalMin = Math.Min(globalMin, prices.Min());
                globalMax = Math.Max(globalMax, prices.Max());
            }

            // Устанавливаем оси графика с небольшим отступом
            double padding = (globalMax - globalMin) * 0.1;
            chart1.ChartAreas[0].AxisY.Minimum = Math.Floor(globalMin - padding);
            chart1.ChartAreas[0].AxisY.Maximum = Math.Ceiling(globalMax + padding);
            chart1.ChartAreas[0].AxisY.Interval = Math.Ceiling((chart1.ChartAreas[0].AxisY.Maximum - chart1.ChartAreas[0].AxisY.Minimum) / 10);
            chart1.ChartAreas[0].AxisY.IsStartedFromZero = false;
            chart1.ChartAreas[0].AxisX.Interval = 1;
            chart1.ChartAreas[0].AxisX.LabelStyle.Angle = -45;
        }

        private void ForecastInflat(string[] lines, int yearsToForecast)
        {
            // Удаляем предыдущие прогнозы (поиск по названию)
            foreach (var s in chart1.Series.Cast<Series>().Where(s => s.Name.Contains("(прогноз)")).ToList())
            {
                chart1.Series.Remove(s);
            }

            string[] headers = lines[0].Split(',');
            int smoothing = (int)numericUpDown1.Value; // Период сглаживания

            double globalMin = double.MaxValue;
            double globalMax = double.MinValue;

            for (int col = 1; col < headers.Length; col++)
            {
                // Получаем значения цен и соответствующие годы
                List<double> prices = lines.Skip(1).Select(l => double.Parse(l.Split(',')[col])).ToList();
                List<int> years = lines.Skip(1).Select(l => int.Parse(l.Split(',')[0])).ToList();

                // Проверка корректности параметра сглаживания
                if (smoothing <= 0 || smoothing > prices.Count)
                {
                    MessageBox.Show($"Некорректное значение сглаживания для {headers[col]}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    continue;
                }

                // Создание серии для прогноза
                var forecastSeries = new Series
                {
                    Name = $"Прогноз {col} (прогноз)",
                    ChartType = SeriesChartType.Line,
                    BorderDashStyle = ChartDashStyle.Dash, // Линия прогноза — пунктир
                    Color = Color.Red,
                    BorderWidth = 2,
                    LegendText = $"Прогноз {headers[col]}"
                };

                // Добавляем последнюю известную точку
                forecastSeries.Points.AddXY(years.Last(), prices.Last());

                int lastYear = years.Last();

                // Прогноз на N лет вперёд
                for (int i = 0; i < yearsToForecast; i++)
                {
                    double avg = prices.Skip(prices.Count - smoothing).Take(smoothing).Average(); // Скользящее среднее
                    prices.Add(avg);
                    int nextYear = lastYear + i + 1;
                    forecastSeries.Points.AddXY(nextYear, avg);
                }

                chart1.Series.Add(forecastSeries);

                // Обновляем диапазон значений оси Y
                globalMin = Math.Min(globalMin, prices.Min());
                globalMax = Math.Max(globalMax, prices.Max());

                listBox1.Items.Clear();
                double initialPrice = (int)numericPrice.Value;
                double adjustedPrice = initialPrice;
                foreach (var inflat in prices.Skip(prices.Count - smoothing))
                {
                    double rate = inflat / 100; // переводим проценты в десятичное значение
                    adjustedPrice *= (1 + rate);
                }
                adjustedPrice = Math.Round(adjustedPrice, 2);
                listBox1.Items.Add($"Цена услуги в {years.Last()} с учетом инфляции: {adjustedPrice}");
            }

            // Устанавливаем оси графика с небольшим отступом
            double padding = (globalMax - globalMin) * 0.1;
            chart1.ChartAreas[0].AxisY.Minimum = Math.Floor(globalMin - padding);
            chart1.ChartAreas[0].AxisY.Maximum = Math.Ceiling(globalMax + padding);
            chart1.ChartAreas[0].AxisY.Interval = Math.Ceiling((chart1.ChartAreas[0].AxisY.Maximum - chart1.ChartAreas[0].AxisY.Minimum) / 10);
            chart1.ChartAreas[0].AxisY.IsStartedFromZero = false;
            chart1.ChartAreas[0].AxisX.Interval = 1;
            chart1.ChartAreas[0].AxisX.LabelStyle.Angle = -45;
        }

        // Обработчик кнопки "Прогнозировать"
        // Проверяет корректность ввода и запускает прогноз
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
            switch (selectedDataType)
            {
                case DataType.Housing:
                    ForecastPrices(lines, yearsToForecast); // Запуск прогноза
                    break;
                case DataType.Inflation:
                    ForecastInflat(lines, yearsToForecast);
                    break;
            }
            
        }
    }
}
