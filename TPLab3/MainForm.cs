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
            Inflation,
            Marriage
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
            comboMode.Items.Add("Браки и Разводы");
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

                listBox1.Items.Clear();    
                chart1.Series.Clear();   
                dataGridView1.Rows.Clear(); 
                dataGridView1.Columns.Clear();
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
            string headers = lines[0];
            if ((selectedDataType == DataType.Housing && headers == "Год,1-комн/цена за 1 кв.м,2-комн/цена за 1 кв.м,3-комн/цена за 1 кв.м") ||
     (selectedDataType == DataType.Inflation && headers == "Год,Уровень инфляции") ||
     (selectedDataType == DataType.Marriage && headers == "Год,Мужчины 20-29,Мужчины 30-39,Мужчины 40-49,Женщины 20-29,Женщины 30-39,Женщины 40-49"))
            {
                switch (selectedDataType)
                {
                    case DataType.Housing:
                        LoadDataToGrid(lines);
                        PlotChart(lines);
                        AnalyzeTrends(lines);
                        break;
                    case DataType.Inflation:
                        LoadDataToGrid(lines);
                        PlotChart(lines);
                        break;
                    case DataType.Marriage:
                        LoadDataToGrid(lines);
                        PlotChart(lines);
                        AnalyzeMarriageTrends(lines); 
                        break;
                }
            }
            else
            {
                MessageBox.Show("Загружены неверные данные", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lines = null;
                currentFilePath = null;
                listBox1.Items.Clear();
                chart1.Series.Clear();
                dataGridView1.Rows.Clear();
                dataGridView1.Columns.Clear();
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
            chart1.ChartAreas[0].AxisY.Minimum = Double.NaN;
            chart1.ChartAreas[0].AxisY.Maximum = Double.NaN;
            chart1.ChartAreas[0].AxisY.Interval = Double.NaN;
            chart1.ChartAreas[0].AxisX.Interval = 1;
            chart1.ChartAreas[0].AxisX.Title = "Год";
            chart1.ChartAreas[0].AxisY.Title = "Значение";
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
            // Удаляем предыдущие прогнозы
            foreach (var s in chart1.Series.Cast<Series>().Where(s => s.Name.Contains("(прогноз)")).ToList())
            {
                chart1.Series.Remove(s);
            }

            if (lines == null || lines.Length < 2)
            {
                MessageBox.Show("Недостаточно данных для построения прогноза.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string[] headers = lines[0].Split(',');
            int smoothing;

            try
            {
                smoothing = (int)numericUpDown1.Value;
            }
            catch
            {
                MessageBox.Show("Некорректное значение сглаживания.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (smoothing <= 0)
            {
                MessageBox.Show("Сглаживание должно быть положительным числом.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            double globalMin = double.MaxValue;
            double globalMax = double.MinValue;

            for (int col = 1; col < headers.Length; col++)
            {
                List<double> prices;
                List<int> years;

                try
                {
                    prices = lines.Skip(1).Select(l => double.Parse(l.Split(',')[col])).ToList();
                    years = lines.Skip(1).Select(l => int.Parse(l.Split(',')[0])).ToList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при чтении данных для {headers[col]}: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    continue;
                }

                if (smoothing > prices.Count)
                {
                    MessageBox.Show($"Сглаживание превышает количество данных для {headers[col]}.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    continue;
                }

                // Создаём прогнозную серию
                var forecastSeries = new Series
                {
                    Name = $"Прогноз {col} (прогноз)",
                    ChartType = SeriesChartType.Line,
                    BorderDashStyle = ChartDashStyle.Dash,
                    Color = Color.Red,
                    BorderWidth = 2,
                    LegendText = $"Прогноз {headers[col]}"
                };

                forecastSeries.Points.AddXY(years.Last(), prices.Last());
                int lastYear = years.Last();

                for (int i = 0; i < yearsToForecast; i++)
                {
                    double avg = prices.Skip(prices.Count - smoothing).Take(smoothing).Average();
                    prices.Add(avg);
                    forecastSeries.Points.AddXY(lastYear + i + 1, avg);
                }

                chart1.Series.Add(forecastSeries);

                globalMin = Math.Min(globalMin, prices.Min());
                globalMax = Math.Max(globalMax, prices.Max());
            }

            if (globalMin == double.MaxValue || globalMax == double.MinValue)
                return;

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

            if (lines == null || lines.Length < 2)
            {
                MessageBox.Show("Недостаточно данных для построения прогноза.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string[] headers = lines[0].Split(',');
            int smoothing;

            try
            {
                smoothing = (int)numericUpDown1.Value;
            }
            catch
            {
                MessageBox.Show("Некорректное значение сглаживания.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (smoothing <= 0)
            {
                MessageBox.Show("Сглаживание должно быть положительным числом.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            double globalMin = double.MaxValue;
            double globalMax = double.MinValue;

            for (int col = 1; col < headers.Length; col++)
            {
                List<double> prices;
                List<int> years;

                try
                {
                    prices = lines.Skip(1).Select(l => double.Parse(l.Split(',')[col])).ToList();
                    years = lines.Skip(1).Select(l => int.Parse(l.Split(',')[0])).ToList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при чтении данных для {headers[col]}: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    continue;
                }

                if (smoothing > prices.Count)
                {
                    MessageBox.Show($"Сглаживание превышает количество данных для {headers[col]}.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    continue;
                }

                // Создание серии для прогноза
                var forecastSeries = new Series
                {
                    Name = $"Прогноз {col} (прогноз)",
                    ChartType = SeriesChartType.Line,
                    BorderDashStyle = ChartDashStyle.Dash,
                    Color = Color.Red,
                    BorderWidth = 2,
                    LegendText = $"Прогноз {headers[col]}"
                };

                forecastSeries.Points.AddXY(years.Last(), prices.Last());
                int lastYear = years.Last();

                for (int i = 0; i < yearsToForecast; i++)
                {
                    double avg = prices.Skip(prices.Count - smoothing).Take(smoothing).Average();
                    prices.Add(avg);
                    forecastSeries.Points.AddXY(lastYear + i + 1, avg);
                }

                chart1.Series.Add(forecastSeries);

                globalMin = Math.Min(globalMin, prices.Min());
                globalMax = Math.Max(globalMax, prices.Max());

                try
                {
                    listBox1.Items.Clear();
                    double initialPrice = (double)numericPrice.Value;
                    double adjustedPrice = initialPrice;
                    foreach (var inflat in prices.Skip(prices.Count - smoothing))
                    {
                        double rate = inflat / 100;
                        adjustedPrice *= (1 + rate);
                    }
                    adjustedPrice = Math.Round(adjustedPrice, 2);
                    listBox1.Items.Add($"Цена услуги в {years.Last()} с учетом инфляции: {adjustedPrice}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при расчёте инфляции: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            if (globalMin == double.MaxValue || globalMax == double.MinValue)
                return;

            // Настройка осей графика
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
                    ForecastPrices(lines, yearsToForecast);
                    break;
                case DataType.Inflation:
                    ForecastInflat(lines, yearsToForecast);
                    break;
                case DataType.Marriage:
                    ForecastMarriage(lines, yearsToForecast);
                    break;
            }
        }
        private void AnalyzeMarriageTrends(string[] lines)
        {
            listBox1.Items.Clear();

            string[] headers = lines[0].Split(',');

            Dictionary<string, List<double>> valuesByGroup = new Dictionary<string, List<double>>();
            Dictionary<string, List<double>> divorcesByGroup = new Dictionary<string, List<double>>();
            Dictionary<string, int> totalDivorcesByGroup = new Dictionary<string, int>();

            for (int i = 1; i < headers.Length; i++)
            {
                valuesByGroup[headers[i]] = new List<double>();
                divorcesByGroup[headers[i]] = new List<double>();
                totalDivorcesByGroup[headers[i]] = 0;
            }

            List<int> years = new List<int>();

            for (int row = 1; row < lines.Length; row++)
            {
                string[] values = lines[row].Split(',');

                if (int.TryParse(values[0], out int year))
                    years.Add(year);

                for (int col = 1; col < values.Length && col < headers.Length; col++)
                {
                    if (double.TryParse(values[col], out double value))
                        valuesByGroup[headers[col]].Add(value);
                }
            }

            for (int i = 1; i < years.Count; i++)
            {
                foreach (var group in valuesByGroup.Keys)
                {
                    double prevYear = valuesByGroup[group][i - 1];
                    double currentYear = valuesByGroup[group][i];

                    if (currentYear < prevYear)
                    {
                        double divorces = prevYear - currentYear;
                        divorcesByGroup[group].Add(divorces);
                        totalDivorcesByGroup[group] += (int)divorces;
                    }
                    else
                    {
                        divorcesByGroup[group].Add(0);
                    }
                }
            }


            List<string> menGroups = headers.Where(h => h.StartsWith("Мужчины")).ToList();
            List<string> womenGroups = headers.Where(h => h.StartsWith("Женщины")).ToList();

            string mostCommonMenMarriageGroup = FindMostCommonGroup(menGroups, valuesByGroup);
            string mostCommonWomenMarriageGroup = FindMostCommonGroup(womenGroups, valuesByGroup);


            string mostCommonMenDivorceGroup = FindGroupWithHighestTotal(menGroups, totalDivorcesByGroup);
            string mostCommonWomenDivorceGroup = FindGroupWithHighestTotal(womenGroups, totalDivorcesByGroup);


            listBox1.Items.Add($"Чаще всего женились мужчины: {mostCommonMenMarriageGroup}");
            listBox1.Items.Add($"Чаще всего выходили замуж женщины: {mostCommonWomenMarriageGroup}");
            listBox1.Items.Add($"Чаще всего разводились мужчины: {mostCommonMenDivorceGroup}");
            listBox1.Items.Add($"Чаще всего разводились женщины: {mostCommonWomenDivorceGroup}");

           

        }

        private string FindMostCommonGroup(List<string> groups, Dictionary<string, List<double>> valuesByGroup)
        {
            string mostCommonGroup = "";
            double highestAverage = double.MinValue;

            foreach (string group in groups)
            {
                double avg = valuesByGroup[group].Average();
                if (avg > highestAverage)
                {
                    highestAverage = avg;
                    mostCommonGroup = group;
                }
            }

            return mostCommonGroup;
        }

        private string FindGroupWithHighestTotal(List<string> groups, Dictionary<string, int> totalsByGroup)
        {
            string highestGroup = "";
            int highestTotal = int.MinValue;

            foreach (string group in groups)
            {
                int total = totalsByGroup[group];
                if (total > highestTotal)
                {
                    highestTotal = total;
                    highestGroup = group;
                }
            }

            return highestGroup;
        }
        private void ForecastMarriage(string[] lines, int yearsToForecast)
        {
            foreach (var s in chart1.Series.Cast<Series>().Where(s => s.Name.Contains("(прогноз)")).ToList())
            {
                chart1.Series.Remove(s);
            }

            if (lines == null || lines.Length < 2)
            {
                MessageBox.Show("Недостаточно данных для построения прогноза.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string[] headers = lines[0].Split(',');
            int smoothing;

            try
            {
                smoothing = (int)numericUpDown1.Value;
            }
            catch
            {
                MessageBox.Show("Некорректное значение сглаживания.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (smoothing <= 0)
            {
                MessageBox.Show("Сглаживание должно быть положительным числом.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            double globalMin = double.MaxValue;
            double globalMax = double.MinValue;

            for (int col = 1; col < headers.Length; col++)
            {
                List<double> values;
                List<int> years;

                try
                {
                    values = lines.Skip(1).Select(l => double.Parse(l.Split(',')[col])).ToList();
                    years = lines.Skip(1).Select(l => int.Parse(l.Split(',')[0])).ToList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при чтении данных для {headers[col]}: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    continue;
                }

                if (smoothing > values.Count)
                {
                    MessageBox.Show($"Сглаживание превышает количество данных для {headers[col]}.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    continue;
                }

                var forecastSeries = new Series
                {
                    Name = $"Прогноз {col} (прогноз)",
                    ChartType = SeriesChartType.Line,
                    BorderDashStyle = ChartDashStyle.Dash,
                    Color = Color.Red,
                    BorderWidth = 2,
                    LegendText = $"Прогноз {headers[col]}"
                };

                forecastSeries.Points.AddXY(years.Last(), values.Last());
                int lastYear = years.Last();

                for (int i = 0; i < yearsToForecast; i++)
                {
                    double avg = values.Skip(values.Count - smoothing).Take(smoothing).Average();
                    values.Add(avg);
                    forecastSeries.Points.AddXY(lastYear + i + 1, avg);
                }

                chart1.Series.Add(forecastSeries);

                globalMin = Math.Min(globalMin, values.Min());
                globalMax = Math.Max(globalMax, values.Max());
            }

            if (globalMin == double.MaxValue || globalMax == double.MinValue)
                return;

            double padding = (globalMax - globalMin) * 0.1;
            chart1.ChartAreas[0].AxisY.Minimum = Math.Floor(globalMin - padding);
            chart1.ChartAreas[0].AxisY.Maximum = Math.Ceiling(globalMax + padding);
            chart1.ChartAreas[0].AxisY.Interval = Math.Ceiling((chart1.ChartAreas[0].AxisY.Maximum - chart1.ChartAreas[0].AxisY.Minimum) / 10);
            chart1.ChartAreas[0].AxisY.IsStartedFromZero = false;
            chart1.ChartAreas[0].AxisX.Interval = 1;
            chart1.ChartAreas[0].AxisX.LabelStyle.Angle = -45;
        }
    }
}
