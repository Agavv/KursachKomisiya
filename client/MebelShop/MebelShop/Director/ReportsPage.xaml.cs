using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.Win32;
using System.Windows.Media;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using MebelShop.Helpers;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace MebelShop.Director
{
    public partial class ReportsPage : Page, INotifyPropertyChanged
    {
        private readonly Frame _mainFrame;

        // === 1. Продажи и выручка ===
        #region Продажи привязки
        private ObservableCollection<SalesReportItem> _salesTable = new();
        public ObservableCollection<SalesReportItem> SalesTable
        {
            get => _salesTable;
            set => SetField(ref _salesTable, value);
        }

        private SeriesCollection _salesChartData;
        public SeriesCollection SalesChartData
        {
            get => _salesChartData;
            set => SetField(ref _salesChartData, value);
        }

        private string[] _salesDays;
        public string[] SalesDays
        {
            get => _salesDays;
            set => SetField(ref _salesDays, value);
        }

        private string _topProduct;
        public string TopProduct
        {
            get => _topProduct;
            set => SetField(ref _topProduct, value);
        }

        private string _topCategory;
        public string TopCategory
        {
            get => _topCategory;
            set => SetField(ref _topCategory, value);
        }

        private string _selectedMonth;
        public string SelectedMonth
        {
            get => _selectedMonth;
            set
            {
                if (SetField(ref _selectedMonth, value))
                {
                    LoadSalesReport(null);
                }
            }
        }

        private int? _selectedYear;
        public int? SelectedYear
        {
            get => _selectedYear;
            set
            {
                if (SetField(ref _selectedYear, value))
                {
                    LoadSalesReport(null);
                }
            }
        }
        #endregion

        // === 2. Остатки ===
        #region Остатки привязки
        private ObservableCollection<StockReportItem> _stockTable = new();
        public ObservableCollection<StockReportItem> StockTable
        {
            get => _stockTable;
            set => SetField(ref _stockTable, value);
        }

        private SeriesCollection _stockChartData;
        public SeriesCollection StockChartData
        {
            get => _stockChartData;
            set => SetField(ref _stockChartData, value);
        }

        private string[] _stockProductNames;
        public string[] StockProductNames
        {
            get => _stockProductNames;
            set => SetField(ref _stockProductNames, value);
        }

        private double _chartWidth;
        public double ChartWidth
        {
            get => _chartWidth;
            set => SetField(ref _chartWidth, value);
        }
        #endregion
        // === 3. Рейтинги ===
        private ObservableCollection<RatingReportItem> _ratingTable = new();
        public ObservableCollection<RatingReportItem> RatingTable
        {
            get => _ratingTable;
            set => SetField(ref _ratingTable, value);
        }

        private SeriesCollection _ratingChartData;
        public SeriesCollection RatingChartData
        {
            get => _ratingChartData;
            set => SetField(ref _ratingChartData, value);
        }

        private string[] _ratingProductNames;
        public string[] RatingProductNames
        {
            get => _ratingProductNames;
            set => SetField(ref _ratingProductNames, value);
        }

        public ReportsPage(Frame mainFrame)
        {
            InitializeComponent();
            DataContext = this;
            _mainFrame = mainFrame;

            for (int year = 2020; year <= DateTime.Now.Year; year++)
            {
                YearComboBox.Items.Add(year);
            }

            var monthNames = new[] { "Весь период", "Январь", "Февраль", "Март", "Апрель", "Май", "Июнь", "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь" };
            SelectedMonth = monthNames[0];
            SelectedYear = DateTime.Now.Year;

            MonthComboBox.SelectedIndex = 0;
            YearComboBox.SelectedIndex = YearComboBox.Items.Count - 1;
            LoadSalesReport(null);
        }

        public void LoadReports()
        {
            LoadSalesReport(null);
            LoadStockReport();
            LoadReviewsReport();
        }

        #region Продажи методы
        private bool _isLoadingSalesReport = false;

        private async void LoadSalesReport(object parameter)
        {
            if (_isLoadingSalesReport) return;
            _isLoadingSalesReport = true;

            try
            {
                SalesTable.Clear();

                int? year = SelectedYear;
                int? month = SelectedMonth == "Весь период" ? null : Array.IndexOf(
                    new[] { "Январь", "Февраль", "Март", "Апрель", "Май", "Июнь", "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь" },
                    SelectedMonth
                ) + 1;

                string url = $"Reports/sales?year={year}&month={(month.HasValue ? month : null)}";

                var json = await ApiHelper.GetAsync<JsonElement>(url);

                TopProduct = json.GetProperty("topProduct").GetString() ?? "Нет данных";
                TopCategory = json.GetProperty("topCategory").GetString() ?? "Нет данных";

                var items = json.GetProperty("items").EnumerateArray();
                if (!items.Any())
                {
                    SalesChart.Visibility = Visibility.Collapsed;
                    SalesDataGrid.Visibility = Visibility.Collapsed;
                    SalesTop.Visibility = Visibility.Collapsed;
                    NoSalesDataText.Visibility = Visibility.Visible;
                    
                    SalesDays = new string[0];
                    SalesChartData = new SeriesCollection();
                    return;
                }

                SalesChart.Visibility = Visibility.Visible;
                SalesDataGrid.Visibility = Visibility.Visible;
                SalesTop.Visibility = Visibility.Visible;
                NoSalesDataText.Visibility = Visibility.Collapsed;


                var salesData = items
                    .Select(item => new
                    {
                        Date = DateTime.Parse(item.GetProperty("date").GetString()),
                        Orders = item.GetProperty("orders").GetInt32(),
                        Revenue = item.GetProperty("revenue").GetDecimal(),
                        AvgOrder = item.GetProperty("avgOrder").GetDecimal()
                    })
                    .ToList();

                IEnumerable<SalesReportItem> groupedItems;
                if (SelectedMonth == "Весь период")
                {
                    groupedItems = salesData
                        .GroupBy(x => x.Date.Month)
                        .Select(g => new SalesReportItem
                        {
                            Date = new[] { "Январь", "Февраль", "Март", "Апрель", "Май", "Июнь", "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь" }[g.Key - 1] + $" {SelectedYear}",
                            Orders = g.Sum(x => x.Orders),
                            Revenue = Math.Floor(g.Sum(x => x.Revenue)),
                            AvgOrder = Math.Round(g.Average(x => x.AvgOrder), 2)
                        })
                        .OrderBy(x => Array.IndexOf(
                            new[] { "Январь", "Февраль", "Март", "Апрель", "Май", "Июнь", "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь" },
                            x.Date.Split(' ')[0]) + 1);
                }
                else
                {
                    groupedItems = salesData
                        .Select(x => new SalesReportItem
                        {
                            Date = x.Date.ToString("dd.MM.yyyy"),
                            Orders = x.Orders,
                            Revenue = Math.Floor(x.Revenue),
                            AvgOrder = Math.Round(x.AvgOrder, 2)
                        })
                        .OrderBy(x => DateTime.Parse(x.Date));
                }

                foreach (var item in groupedItems)
                {
                    SalesTable.Add(item);
                }

                SalesDays = SalesTable.Select(x => x.Date).ToArray();
                SalesChartData = new SeriesCollection
                    {
                        new ColumnSeries
                        {
                            Title = "Выручка",
                            Values = new ChartValues<decimal>(SalesTable.Select(x => x.Revenue)),
                            Fill = Brushes.Orange,
                            Stroke = Brushes.Orange
                        }
                    };
                }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки отчёта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isLoadingSalesReport = false;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void GeneratePdfReport_Click(object sender, RoutedEventArgs e)
        {
            if (!SalesTable.Any())
            {
                MessageBox.Show("Нет данных для формирования отчёта.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "PDF files (*.pdf)|*.pdf",
                FileName = $"SalesReport_{SelectedMonth}_{SelectedYear}.pdf"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                    Document document = new Document(PageSize.A4, 50, 50, 60, 60);
                    PdfWriter.GetInstance(document, new FileStream(saveFileDialog.FileName, FileMode.Create));
                    document.Open();

                    string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
                    BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);

                    var titleFont = new iTextSharp.text.Font(baseFont, 20, iTextSharp.text.Font.BOLD, new BaseColor(33, 66, 99));
                    var subtitleFont = new iTextSharp.text.Font(baseFont, 14, iTextSharp.text.Font.BOLD, new BaseColor(66, 100, 140));
                    var normalFont = new iTextSharp.text.Font(baseFont, 12, iTextSharp.text.Font.NORMAL, BaseColor.BLACK);
                    var boldFont = new iTextSharp.text.Font(baseFont, 12, iTextSharp.text.Font.BOLD, BaseColor.BLACK);
                    var tableHeaderFont = new iTextSharp.text.Font(baseFont, 12, iTextSharp.text.Font.BOLD, BaseColor.WHITE);

                    Paragraph title = new Paragraph("ОТЧЁТ ПО ПРОДАЖАМ", titleFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 10f
                    };
                    document.Add(title);

                    Paragraph sub = new Paragraph($"MebelShop — {DateTime.Now:dd.MM.yyyy HH:mm}", subtitleFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 20f
                    };
                    document.Add(sub);

                    string periodDescription = SelectedMonth == "Весь период"
                        ? $"за весь период {SelectedYear} года"
                        : $"за {SelectedMonth} {SelectedYear} года";
                    Paragraph description = new Paragraph(
                        $"Данный отчёт содержит данные о количестве заказов, выручке, среднем чеке и ключевых лидерах продаж {periodDescription}.",
                        normalFont
                    )
                    {
                        SpacingAfter = 20f
                    };
                    document.Add(description);

                    var chart = this.FindName("SalesChart") as CartesianChart;
                    if (chart != null)
                    {
                        Size chartSize = new Size(chart.ActualWidth, chart.ActualHeight);
                        chart.Measure(chartSize);
                        chart.Arrange(new Rect(chartSize));

                        RenderTargetBitmap rtb = new RenderTargetBitmap((int)chart.ActualWidth, (int)chart.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                        rtb.Render(chart);

                        PngBitmapEncoder png = new PngBitmapEncoder();
                        png.Frames.Add(BitmapFrame.Create(rtb));
                        using (MemoryStream ms = new MemoryStream())
                        {
                            png.Save(ms);
                            iTextSharp.text.Image chartImage = iTextSharp.text.Image.GetInstance(ms.ToArray());
                            chartImage.ScalePercent(55f);
                            chartImage.Alignment = iTextSharp.text.Image.ALIGN_CENTER;
                            chartImage.SpacingAfter = 20f;
                            document.Add(chartImage);
                        }
                    }

                    PdfPTable table = new PdfPTable(4)
                    {
                        WidthPercentage = 100
                    };
                    table.SetWidths(new float[] { 2f, 1f, 1f, 1f });

                    BaseColor headerColor = new BaseColor(33, 66, 99);
                    BaseColor evenRow = new BaseColor(245, 247, 250);

                    string[] headers = { "Дата", "Заказы", "Выручка (₽)", "Средний чек (₽)" };
                    foreach (var h in headers)
                    {
                        PdfPCell headerCell = new PdfPCell(new Phrase(h, tableHeaderFont))
                        {
                            BackgroundColor = headerColor,
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            Padding = 8
                        };
                        table.AddCell(headerCell);
                    }

                    bool isEven = false;
                    foreach (var item in SalesTable)
                    {
                        BaseColor bg = isEven ? evenRow : BaseColor.WHITE;
                        table.AddCell(new PdfPCell(new Phrase(item.Date, normalFont)) { BackgroundColor = bg, Padding = 6 });
                        table.AddCell(new PdfPCell(new Phrase(item.Orders.ToString(), normalFont)) { BackgroundColor = bg, HorizontalAlignment = Element.ALIGN_CENTER, Padding = 6 });
                        table.AddCell(new PdfPCell(new Phrase(item.Revenue.ToString("N2"), normalFont)) { BackgroundColor = bg, HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 6 });
                        table.AddCell(new PdfPCell(new Phrase(item.AvgOrder.ToString("N2"), normalFont)) { BackgroundColor = bg, HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 6 });
                        isEven = !isEven;
                    }

                    document.Add(table);

                    Paragraph leadersTitle = new Paragraph("\nЛидеры продаж", subtitleFont)
                    {
                        SpacingBefore = 15f,
                        SpacingAfter = 10f
                    };
                    document.Add(leadersTitle);

                    document.Add(new Paragraph($"• Самый продаваемый товар: {TopProduct}", normalFont));
                    document.Add(new Paragraph($"• Самая продаваемая категория: {TopCategory}", normalFont));

                    Paragraph footer = new Paragraph(
                        "\n\nОтчёт сформирован автоматически системой MebelShop.",
                        new iTextSharp.text.Font(baseFont, 10, iTextSharp.text.Font.ITALIC, new BaseColor(100, 100, 100))
                    )
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingBefore = 30f
                    };
                    document.Add(footer);

                    document.Close();

                    MessageBox.Show("Отчёт успешно сохранён в PDF.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении отчёта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        #endregion

        #region Остаток методы
        private async void LoadStockReport()
        {
            try
            {
                StockTable.Clear();

                string url = "Reports/stock";
                var json = await ApiHelper.GetAsync<JsonElement[]>(url);

                var stockItems = json.Select(item => new StockReportItem
                {
                    IdProduct = item.GetProperty("idProduct").GetInt32(),
                    ProductName = item.GetProperty("productName").GetString(),
                    Stock = item.GetProperty("stock").GetInt32(),
                    Sold = item.GetProperty("sold").GetInt32(),
                }).ToList();

                if (StockFilterComboBox.SelectedItem is ComboBoxItem selected)
                {
                    string filter = selected.Content.ToString();
                    stockItems = filter switch
                    {
                        "Топ проданных товаров" => stockItems.OrderByDescending(x => x.Sold).ToList(),
                        "Осталось мало на складе" => stockItems.OrderBy(x => x.Stock).ToList(),
                        "Меньше всего проданных товаров" => stockItems.OrderBy(x => x.Sold).ToList(),
                        _ => stockItems.OrderBy(x => x.ProductName).ToList()
                    };
                }

                foreach (var item in stockItems)
                {
                    StockTable.Add(item);
                }

                StockProductNames = StockTable
                    .Select(x => $"[ID:{x.IdProduct}] - {(x.ProductName.Length > 10 ? x.ProductName.Substring(0, 10) + "…" : x.ProductName)}")
                    .ToArray();
                StockChartData = new SeriesCollection
                {
                    new ColumnSeries
                    {
                        Title = "На складе",
                        Values = new ChartValues<int>(StockTable.Select(x => x.Stock)),
                        DataLabels = true,
                        LabelPoint = point => point.Y.ToString(),
                        Fill = Brushes.Green,
                        Foreground = Brushes.Green,
                    },
                    new ColumnSeries
                    {
                        Title = "Продано",
                        Values = new ChartValues<int>(StockTable.Select(x => x.Sold)),
                        Fill = Brushes.Orange
                    }
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки отчёта по остаткам: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StockFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadStockReport();
        }

        private void GenerateStockPdfReport_Click(object sender, RoutedEventArgs e)
        {
            if (!StockTable.Any())
            {
                MessageBox.Show("Нет данных для формирования отчёта.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "PDF files (*.pdf)|*.pdf",
                FileName = $"StockReport_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                    Document document = new Document(PageSize.A4, 50, 50, 60, 60);
                    PdfWriter.GetInstance(document, new FileStream(saveFileDialog.FileName, FileMode.Create));
                    document.Open();

                    string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
                    BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);

                    var titleFont = new iTextSharp.text.Font(baseFont, 20, iTextSharp.text.Font.BOLD, new BaseColor(33, 66, 99));
                    var subtitleFont = new iTextSharp.text.Font(baseFont, 14, iTextSharp.text.Font.BOLD, new BaseColor(66, 100, 140));
                    var normalFont = new iTextSharp.text.Font(baseFont, 12, iTextSharp.text.Font.NORMAL, BaseColor.BLACK);
                    var tableHeaderFont = new iTextSharp.text.Font(baseFont, 12, iTextSharp.text.Font.BOLD, BaseColor.WHITE);

                    Paragraph title = new Paragraph("ОТЧЁТ ПО ОСТАТКАМ И ДВИЖЕНИЮ ТОВАРОВ", titleFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 10f
                    };
                    document.Add(title);

                    Paragraph sub = new Paragraph($"MebelShop — {DateTime.Now:dd.MM.yyyy HH:mm}", subtitleFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 20f
                    };
                    document.Add(sub);

                    Paragraph description = new Paragraph(
                        "Данный отчёт содержит данные о количестве товаров на складе, проданных единицах и остатках на текущий момент.",
                        normalFont
                    )
                    {
                        SpacingAfter = 20f
                    };
                    document.Add(description);

                    if (StockChart != null)
                    {
                        Size chartSize = new Size(StockChart.ActualWidth, StockChart.ActualHeight);
                        StockChart.Measure(chartSize);
                        StockChart.Arrange(new Rect(chartSize));

                        RenderTargetBitmap rtb = new RenderTargetBitmap((int)StockChart.ActualWidth, (int)StockChart.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                        rtb.Render(StockChart);

                        PngBitmapEncoder png = new PngBitmapEncoder();
                        png.Frames.Add(BitmapFrame.Create(rtb));
                        using (MemoryStream ms = new MemoryStream())
                        {
                            png.Save(ms);
                            iTextSharp.text.Image chartImage = iTextSharp.text.Image.GetInstance(ms.ToArray());
                            chartImage.ScalePercent(55f);
                            chartImage.Alignment = iTextSharp.text.Image.ALIGN_CENTER;
                            chartImage.SpacingAfter = 20f;
                            document.Add(chartImage);
                        }
                    }

                    PdfPTable table = new PdfPTable(4)
                    {
                        WidthPercentage = 100
                    };
                    table.SetWidths(new float[] { 1f, 3f, 1f, 1f });

                    BaseColor headerColor = new BaseColor(33, 66, 99);
                    BaseColor evenRow = new BaseColor(245, 247, 250);

                    string[] headers = {"ID", "Товар", "На складе", "Продано"};
                    foreach (var h in headers)
                    {
                        PdfPCell headerCell = new PdfPCell(new Phrase(h, tableHeaderFont))
                        {
                            BackgroundColor = headerColor,
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            Padding = 8
                        };
                        table.AddCell(headerCell);
                    }

                    bool isEven = false;
                    foreach (var item in StockTable)
                    {
                        BaseColor bg = isEven ? evenRow : BaseColor.WHITE;
                        table.AddCell(new PdfPCell(new Phrase(item.IdProduct.ToString(), normalFont)) { BackgroundColor = bg, HorizontalAlignment = Element.ALIGN_CENTER });
                        table.AddCell(new PdfPCell(new Phrase(item.ProductName, normalFont)) { BackgroundColor = bg, Padding = 6, });
                        table.AddCell(new PdfPCell(new Phrase(item.Stock.ToString(), normalFont)) { BackgroundColor = bg, HorizontalAlignment = Element.ALIGN_CENTER, Padding = 6 });
                        table.AddCell(new PdfPCell(new Phrase(item.Sold.ToString(), normalFont)) { BackgroundColor = bg, HorizontalAlignment = Element.ALIGN_CENTER, Padding = 6 });
                        isEven = !isEven;
                    }

                    document.Add(table);

                    Paragraph footer = new Paragraph(
                        "\n\nОтчёт сформирован автоматически системой MebelShop.",
                        new iTextSharp.text.Font(baseFont, 10, iTextSharp.text.Font.ITALIC, new BaseColor(100, 100, 100))
                    )
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingBefore = 30f
                    };
                    document.Add(footer);

                    document.Close();

                    MessageBox.Show("Отчёт успешно сохранён в PDF.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении отчёта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        #endregion

        private async void LoadReviewsReport()
        {
            try
            {
                RatingTable.Clear();

                string url = "Reports/reviews";
                var json = await ApiHelper.GetAsync<JsonElement[]>(url);

                if (json == null || !json.Any())
                {
                    ReviewsChart.Visibility = Visibility.Collapsed;
                    ReviewsDataGrid.Visibility = Visibility.Collapsed;
                    return;
                }

                var reviewItems = json
                    .Where(item => item.GetProperty("reviewCount").GetInt32() > 0)
                    .Select(item => new RatingReportItem
                    {
                        IdProduct = item.GetProperty("idProduct").GetInt32(),
                        ProductName = item.GetProperty("productName").GetString(),
                        AvgRating = item.GetProperty("avgRating").GetDecimal(),
                        ReviewCount = item.GetProperty("reviewCount").GetInt32()
                    })
                    .ToList();

                if (ReviewsFilterComboBox.SelectedItem is ComboBoxItem selected)
                {
                    string filter = selected.Content.ToString();
                    reviewItems = filter switch
                    {
                        "По средней оценке (по убыванию)" => reviewItems.OrderByDescending(x => x.AvgRating).ToList(),
                        "По средней оценке (по возрастанию)" => reviewItems.OrderBy(x => x.AvgRating).ToList(),
                        "По количеству отзывов (по убыванию)" => reviewItems.OrderByDescending(x => x.ReviewCount).ToList(),
                        "По популярности (рейтинг * отзывы)" => reviewItems
                            .OrderByDescending(x => (double)x.AvgRating * x.ReviewCount)
                            .ToList(),
                        _ => reviewItems.OrderBy(x => x.ProductName).ToList()
                    };
                }

                foreach (var item in reviewItems)
                {
                    RatingTable.Add(item);
                }

                RatingProductNames = RatingTable
                    .Select(x => $"[ID:{x.IdProduct}] - {(x.ProductName.Length > 10 ? x.ProductName.Substring(0, 10) + "…" : x.ProductName)}")
                    .ToArray();

                RatingChartData = new SeriesCollection
                {
                    new ColumnSeries
                    {
                        Title = "Средний рейтинг",
                        Values = new ChartValues<decimal>(RatingTable.Select(x => x.AvgRating)),
                        Fill = Brushes.Gold,
                        DataLabels = true,
                        LabelPoint = point => point.Y.ToString("0.0"),
                        Foreground = Brushes.Gold
                    },
                    new ColumnSeries
                    {
                        Title = "Кол-во отзывов",
                        Values = new ChartValues<int>(RatingTable.Select(x => x.ReviewCount)),
                        Fill = Brushes.Green,
                        DataLabels = true,
                        Foreground = Brushes.Green
                    }
                };

                ReviewsChart.Visibility = Visibility.Visible;
                ReviewsDataGrid.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки отчёта по отзывам: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void ReviewsFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadReviewsReport();
        }

        private void GenerateReviewsPdfReport_Click(object sender, RoutedEventArgs e)
        {
            if (!RatingTable.Any())
            {
                MessageBox.Show("Нет данных для формирования отчёта.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "PDF files (*.pdf)|*.pdf",
                FileName = $"ReviewsReport_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                    Document document = new Document(PageSize.A4, 50, 50, 60, 60);
                    PdfWriter.GetInstance(document, new FileStream(saveFileDialog.FileName, FileMode.Create));
                    document.Open();

                    string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
                    BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);

                    var titleFont = new iTextSharp.text.Font(baseFont, 20, iTextSharp.text.Font.BOLD, new BaseColor(33, 66, 99));
                    var subtitleFont = new iTextSharp.text.Font(baseFont, 14, iTextSharp.text.Font.BOLD, new BaseColor(66, 100, 140));
                    var normalFont = new iTextSharp.text.Font(baseFont, 12, iTextSharp.text.Font.NORMAL, BaseColor.BLACK);
                    var tableHeaderFont = new iTextSharp.text.Font(baseFont, 12, iTextSharp.text.Font.BOLD, BaseColor.WHITE);

                    Paragraph title = new Paragraph("ОТЧЁТ ПО РЕЙТИНГАМ И ОТЗЫВАМ", titleFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 10f
                    };
                    document.Add(title);

                    Paragraph sub = new Paragraph($"MebelShop — {DateTime.Now:dd.MM.yyyy HH:mm}", subtitleFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 20f
                    };
                    document.Add(sub);

                    Paragraph description = new Paragraph(
                        "Отчёт содержит информацию о средней оценке и количестве отзывов по каждому товару. " +
                        "Данные могут использоваться для анализа удовлетворённости клиентов и выявления популярных товаров.",
                        normalFont
                    )
                    {
                        SpacingAfter = 20f
                    };
                    document.Add(description);

                    // 🔹 Добавляем график, если он есть
                    if (ReviewsChart != null)
                    {
                        Size chartSize = new Size(ReviewsChart.ActualWidth, ReviewsChart.ActualHeight);
                        ReviewsChart.Measure(chartSize);
                        ReviewsChart.Arrange(new Rect(chartSize));

                        RenderTargetBitmap rtb = new RenderTargetBitmap((int)ReviewsChart.ActualWidth, (int)ReviewsChart.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                        rtb.Render(ReviewsChart);

                        PngBitmapEncoder png = new PngBitmapEncoder();
                        png.Frames.Add(BitmapFrame.Create(rtb));
                        using (MemoryStream ms = new MemoryStream())
                        {
                            png.Save(ms);
                            iTextSharp.text.Image chartImage = iTextSharp.text.Image.GetInstance(ms.ToArray());
                            chartImage.ScalePercent(55f);
                            chartImage.Alignment = iTextSharp.text.Image.ALIGN_CENTER;
                            chartImage.SpacingAfter = 20f;
                            document.Add(chartImage);
                        }
                    }

                    // 🔹 Таблица данных
                    PdfPTable table = new PdfPTable(4)
                    {
                        WidthPercentage = 100
                    };
                    table.SetWidths(new float[] { 1f, 3f, 2f, 2f });

                    BaseColor headerColor = new BaseColor(33, 66, 99);
                    BaseColor evenRow = new BaseColor(245, 247, 250);

                    string[] headers = { "ID", "Товар", "Средняя оценка", "Кол-во отзывов" };
                    foreach (var h in headers)
                    {
                        PdfPCell headerCell = new PdfPCell(new Phrase(h, tableHeaderFont))
                        {
                            BackgroundColor = headerColor,
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            Padding = 8
                        };
                        table.AddCell(headerCell);
                    }

                    bool isEven = false;
                    foreach (var item in RatingTable)
                    {
                        BaseColor bg = isEven ? evenRow : BaseColor.WHITE;
                        table.AddCell(new PdfPCell(new Phrase(item.IdProduct.ToString(), normalFont)) { BackgroundColor = bg, HorizontalAlignment = Element.ALIGN_CENTER, Padding = 6 });
                        table.AddCell(new PdfPCell(new Phrase(item.ProductName, normalFont)) { BackgroundColor = bg, Padding = 6 });
                        table.AddCell(new PdfPCell(new Phrase(item.AvgRating.ToString("0.0"), normalFont)) { BackgroundColor = bg, HorizontalAlignment = Element.ALIGN_CENTER, Padding = 6 });
                        table.AddCell(new PdfPCell(new Phrase(item.ReviewCount.ToString(), normalFont)) { BackgroundColor = bg, HorizontalAlignment = Element.ALIGN_CENTER, Padding = 6 });
                        isEven = !isEven;
                    }

                    document.Add(table);

                    Paragraph footer = new Paragraph(
                        "\n\nОтчёт сформирован автоматически системой MebelShop.",
                        new iTextSharp.text.Font(baseFont, 10, iTextSharp.text.Font.ITALIC, new BaseColor(100, 100, 100))
                    )
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingBefore = 30f
                    };
                    document.Add(footer);

                    document.Close();

                    MessageBox.Show("Отчёт по отзывам успешно сохранён в PDF.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении отчёта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    public class SalesReportItem
    {
        public string Date { get; set; }
        public int Orders { get; set; }
        public decimal Revenue { get; set; }
        public decimal AvgOrder { get; set; }
    }

    public class StockReportItem
    {
        public int IdProduct { get; set; }
        public string ProductName { get; set; }
        public int Stock { get; set; }
        public int Sold { get; set; }
    }

    public class RatingReportItem
    {
        public int IdProduct { get; set; }
        public string ProductName { get; set; }
        public decimal AvgRating { get; set; }
        public int ReviewCount { get; set; }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);

        public void Execute(object parameter) => _execute(parameter);
    }
}