/* 
 *   Dicount Range V2.0.1
 *   
 *   An indicator showing discount prices which are used to indicate areas of value to buy and sell.
 *   
 *   12/09/2021
 *   V2.0
 *   Visual enhancements and removal of lower range as not used.
 *   Addition of level 3 contact for completion of discount area.
 * 
 *  14/09/2021
 *  V2.0.1
 *  Option added to globally turn off the discount range boxes.
 * 
*/
 
 
using System;
using System.Collections.Generic;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;
 
namespace cAlgo
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.EEuropeStandardTime, AccessRights = AccessRights.None)]
    public class DiscountRangesv2 : Indicator
    {
        [Parameter("is CBDR Zone", Group = "CBDR", DefaultValue = true)]
        public bool IsCBDR { get; set; }
 
        [Parameter("Area Filled", Group = "CBDR ", DefaultValue = true)]
        public bool CbdrFilled { get; set; }
        [Parameter("Shading Up Color ", Group = "CBDR", DefaultValue = "Green")]
        public string CbdrUColor { get; set; }
 
        [Parameter("Shading Down Color ", Group = "CBDR", DefaultValue = "Red")]
        public string CbdrDColor { get; set; }
 
        [Parameter("Opacity", Group = "CBDR ", DefaultValue = 15, MaxValue = 100)]
        public int CbdrOpt { get; set; }
        [Parameter("Line Thickness ", Group = "CBDR", DefaultValue = 2)]
        public int CbdrLThc { get; set; }
        [Parameter("LineStyle ", Group = "CBDR", DefaultValue = LineStyle.Solid)]
        public LineStyle CbdrLLS { get; set; }
 
        [Parameter("Discount 1 Color", Group = "Discount 1 ", DefaultValue = "Red")]
        public string Discount1Color { get; set; }
        [Parameter("Arae Opacity", Group = "Discount 1 ", DefaultValue = 20, MaxValue = 100)]
        public int Discount1Opt { get; set; }
 
        [Parameter("Liquidity Color", Group = "Liquidity", DefaultValue = "Green")]
        public string Liquidity1Color { get; set; }
        [Parameter("Area Opacity", Group = "Liquidity", DefaultValue = 20, MaxValue = 100)]
        public int LiquidityOpt { get; set; }
 
        [Parameter("Discount 2 Color", Group = "Discount 2", DefaultValue = "Blue")]
        public string Discount2Color { get; set; }
        [Parameter("Area  Opacity", Group = "Discount 2", DefaultValue = 20, MaxValue = 100)]
        public int Discount2Opt { get; set; }
 
        [Parameter("Rush Discount Color", Group = "Rush Discount", DefaultValue = "cyan")]
        public string RushDiscountColor { get; set; }
        [Parameter("Area  Opacity", Group = "Rush Discount", DefaultValue = 50, MaxValue = 100)]
        public int RushDiscountOpt { get; set; }
 
        [Parameter("US Color", Group = "US Session Extended", DefaultValue = "Yellow")]
        public string USColor { get; set; }
        [Parameter("Area Opacity", Group = "US Session Extended ", DefaultValue = 20, MaxValue = 100)]
        public int USOpt { get; set; }
 
        [Parameter("Range Color", Group = "Range ", DefaultValue = "Orange")]
        public string RangeColor { get; set; }
        [Parameter("Out of Range Color", Group = "Range ", DefaultValue = "Violet")]
        public string RangeOutColor { get; set; }
        [Parameter("Area Opacity", Group = "Range", DefaultValue = 80, MaxValue = 100)]
        public int RangeOpt { get; set; }
        [Parameter("Average Range period", Group = "Range", DefaultValue = 21, MaxValue = 100)]
        public int RangePeriod { get; set; }
 
        [Parameter("ATR Period", DefaultValue = 5)]
        public int atr_period { get; set; }
 
        [Parameter("Show boxes", DefaultValue = true)]
        public bool show_boxes { get; set; }
 
        private Canvas _panelCanvas = null;
        private TextBlock _header, _values;
        private Bars WeeklyBars, DailyBars, HourlyBars;
        private double Adr_Value;
        private List<double> RangeList;
        private Dictionary<double, List<ChartTrendLine>> DiscountLevels;
        private Dictionary<DateTime, List<ChartRectangle>> DiscountRects;
 
        private Color Discount1ColorWAlpha, LiquidityColorWAlpha, Discount2ColorWAlpha, RushDiscountColorWAlpha, USColorWAlpha, CbdrUColorWAlpha, CbdrDColorWAlpha, RangeColorWAlpha, RangeOutColorWAlpha;
        private DateTime RangeStart, RangeEnd, LiquidityStart, LiquidityEnd, Present;
 
        private bool USDL, EUDL;
 
        private void SetDates(int index)
        {
            DateTime Date = Bars.OpenTimes[index].AddHours(-24);
 
            int StartHour = 21;
            int LiqStartHour = 7;
            int LiqEndHour = 12;
            USDL = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time").IsDaylightSavingTime(Bars.OpenTimes[index]);
            EUDL = TimeZoneInfo.FindSystemTimeZoneById("E. Europe Standard Time").IsDaylightSavingTime(Bars.OpenTimes[index]);
 
            if (USDL && !EUDL)
            {
                StartHour = 20;
                LiqStartHour = 6;
                LiqEndHour = 11;
            }
            else if (!USDL && EUDL)
            {
                StartHour = 22;
                LiqStartHour = 8;
                LiqEndHour = 13;
            }
 
 
            RangeStart = new DateTime(Date.Year, Date.Month, Date.Day, StartHour, 0, 0);
            RangeEnd = RangeStart.AddHours(6);
 
            if (RangeStart.DayOfWeek == DayOfWeek.Sunday)
            {
                RangeStart = RangeStart.AddDays(-2);
            }
 
            LiquidityStart = TimeZoneInfo.ConvertTimeFromUtc(new DateTime(Date.Year, Date.Month, Date.Day, LiqStartHour, 0, 0), TimeZoneInfo.FindSystemTimeZoneById("E. Europe Standard Time")).AddDays(1);
            LiquidityEnd = TimeZoneInfo.ConvertTimeFromUtc(new DateTime(Date.Year, Date.Month, Date.Day, LiqEndHour, 0, 0), TimeZoneInfo.FindSystemTimeZoneById("E. Europe Standard Time")).AddDays(1);
        }
 
        private Canvas CreatePanel()
        {
            var canvas = new Canvas();
            canvas.Margin = "0 5";
            canvas.Width = 90;
            canvas.Height = 60;
 
            _header = new TextBlock();
            _header.Margin = 5;
            _header.FontSize = 12;
            _header.FontWeight = FontWeight.Bold;
            _header.Opacity = 1;
            _header.ForegroundColor = Color.Lime;
            _header.TextAlignment = TextAlignment.Left;
            _header.VerticalAlignment = VerticalAlignment.Top;
            _header.Text = "RPip\nYDR\nTDR\n5Avg\nR1.5\nR2.0";
 
            _values = new TextBlock();
            _values.Margin = 5;
            _values.FontSize = 12;
            _values.FontWeight = FontWeight.Normal;
            _values.Opacity = 1;
            _values.ForegroundColor = Color.White;
            _values.TextAlignment = TextAlignment.Left;
            _values.VerticalAlignment = VerticalAlignment.Top;
 
            var grid = new Grid(1, 1);
            grid.Rows[0].SetHeightToAuto();
 
            grid.AddChild(canvas, 0, 0, 3, 1);
 
            grid.AddChild(_header, 0, 0, 3, 1);
            grid.AddChild(_values, 0, 0, 3, 1);
 
            var border = new Border();
            border.BorderThickness = 1;
            border.BorderColor = Color.Gray;
            border.Margin = 10;
            border.VerticalAlignment = VerticalAlignment.Top;
            border.HorizontalAlignment = HorizontalAlignment.Left;
            border.Child = grid;
 
            var gridStyle = new Style();
            gridStyle.Set(ControlProperty.BackgroundColor, Color.FromArgb(32, 32, 32));
            gridStyle.Set(ControlProperty.Opacity, 1);
            gridStyle.Set(ControlProperty.Opacity, 0.25, ControlState.Hover);
 
            grid.Style = gridStyle;
 
            Chart.AddControl(border);
            return canvas;
        }
 
        private void updatePanel(int index)
        {
            int DailyIndex = DailyBars.OpenTimes.GetIndexByTime(Bars.OpenTimes[index]);
            double range_pips = RangeList.Count > 0 ? RangeList.ToArray()[RangeList.Count - 1] : 0;
            double range_yesterday = Math.Round((DailyBars.HighPrices[DailyIndex - 1] - DailyBars.LowPrices[DailyIndex - 1]) * Math.Pow(10, Symbol.Digits - 1), 1);
            double range_today = Math.Round((DailyBars.HighPrices[DailyIndex] - DailyBars.LowPrices[DailyIndex]) * Math.Pow(10, Symbol.Digits - 1), 1);
 
            _values.Text = "\t" + range_pips + "\n\t" + range_yesterday + "\n\t" + range_today + "\n\t" + Adr_Value + "\n\t" + Math.Round(Adr_Value * 1.5, 0) + "\n\t" + Math.Round(Adr_Value * 2, 0);
        }
 
        private void DebugInfo(string info, int index)
        {
            ChartText ct = Chart.DrawText("Debug" + index, info, index, Bars.HighPrices[index], Color.Black);
            ct.HorizontalAlignment = HorizontalAlignment.Center;
            ct.VerticalAlignment = VerticalAlignment.Top;
        }
 
        protected override void Initialize()
        {
 
            RangeList = new List<double>(RangePeriod);
            DiscountLevels = new Dictionary<double, List<ChartTrendLine>>();
            DiscountRects = new Dictionary<DateTime, List<ChartRectangle>>();
            Present = Bars.OpenTimes.Last(0);
 
            RangeOpt = (int)(255 * RangeOpt * 0.01);
            CbdrOpt = (int)(255 * CbdrOpt * 0.01);
            Discount1Opt = (int)(255 * Discount1Opt * 0.01);
            LiquidityOpt = (int)(255 * LiquidityOpt * 0.01);
            Discount2Opt = (int)(255 * Discount2Opt * 0.01);
            USOpt = (int)(255 * USOpt * 0.01);
            CbdrUColorWAlpha = Color.FromArgb(CbdrOpt, Color.FromName(CbdrUColor).R, Color.FromName(CbdrUColor).G, Color.FromName(CbdrUColor).B);
            CbdrDColorWAlpha = Color.FromArgb(CbdrOpt, Color.FromName(CbdrDColor).R, Color.FromName(CbdrDColor).G, Color.FromName(CbdrDColor).B);
            RangeColorWAlpha = Color.FromArgb(RangeOpt, Color.FromName(RangeColor).R, Color.FromName(RangeColor).G, Color.FromName(RangeColor).B);
            RangeOutColorWAlpha = Color.FromArgb(RangeOpt, Color.FromName(RangeOutColor).R, Color.FromName(RangeOutColor).G, Color.FromName(RangeOutColor).B);
            Discount1ColorWAlpha = Color.FromArgb(Discount1Opt, Color.FromName(Discount1Color).R, Color.FromName(Discount1Color).G, Color.FromName(Discount1Color).B);
            LiquidityColorWAlpha = Color.FromArgb(LiquidityOpt, Color.FromName(Liquidity1Color).R, Color.FromName(Liquidity1Color).G, Color.FromName(Liquidity1Color).B);
            Discount2ColorWAlpha = Color.FromArgb(Discount2Opt, Color.FromName(Discount2Color).R, Color.FromName(Discount2Color).G, Color.FromName(Discount2Color).B);
            RushDiscountColorWAlpha = Color.FromArgb(RushDiscountOpt, Color.FromName(RushDiscountColor).R, Color.FromName(RushDiscountColor).G, Color.FromName(RushDiscountColor).B);
            USColorWAlpha = Color.FromArgb(USOpt, Color.FromName(USColor).R, Color.FromName(USColor).G, Color.FromName(USColor).B);
            DailyBars = MarketData.GetBars(TimeFrame.Daily);
 
 
            WeeklyBars = MarketData.GetBars(TimeFrame.Weekly);
 
            AverageTrueRange atr = Indicators.AverageTrueRange(DailyBars, atr_period, MovingAverageType.Simple);
 
            Adr_Value = Math.Round(atr.Result.Last(1) / Symbol.PipSize, 0);
 
            if (_panelCanvas == null)
            {
                _panelCanvas = CreatePanel();
            }
 
            if (IsCBDR)
            {
                HourlyBars = MarketData.GetBars(TimeFrame.Hour);
            }
 
            EnsureEnoughBarsIsLoaded();
        }
 
        private void EnsureEnoughBarsIsLoaded()
        {
            if (Bars.TimeFrame > TimeFrame.Hour2)
                return;
 
            int MaxBarsNeeded = 0;
            if (Bars.TimeFrame == TimeFrame.Hour2)
                MaxBarsNeeded = 1440 / 60 * RangePeriod;
            else if (Bars.TimeFrame == TimeFrame.Minute45)
                MaxBarsNeeded = 1440 / 45 * RangePeriod;
            else if (Bars.TimeFrame == TimeFrame.Minute30)
                MaxBarsNeeded = 1440 / 30 * RangePeriod;
            else if (Bars.TimeFrame == TimeFrame.Minute20)
                MaxBarsNeeded = 1440 / 20 * RangePeriod;
            else if (Bars.TimeFrame == TimeFrame.Minute15)
                MaxBarsNeeded = 1440 / 15 * RangePeriod;
            else if (Bars.TimeFrame == TimeFrame.Minute10)
                MaxBarsNeeded = 144 * RangePeriod;
            else
                MaxBarsNeeded = 1440 * RangePeriod;
 
            while (Bars.Count < MaxBarsNeeded)
            {
                int loaded = Bars.LoadMoreHistory();
                if (loaded == 0)
                    break;
            }
        }
 
        public override void Calculate(int index)
        {
 
            if (TimeFrame > TimeFrame.Hour2)
                return;
 
            updatePanel(index);
 
            int TzOffset = 0;
 
            if (USDL && !EUDL)
                TzOffset = -1;
            else if (!USDL && EUDL)
                TzOffset = 1;
 
            if (Bars.OpenTimes[index].Hour == 3 + TzOffset && Bars.OpenTimes[index].Minute == 0)
            {
                SetDates(index);
                DrawRangeBox(index - 1);
                DrawMM(index - 1);
            }
 
            if (show_boxes && Bars.OpenTimes[index] >= LiquidityStart.AddHours(-6).AddMinutes(0) && Bars.OpenTimes[index] <= LiquidityStart.AddHours(1).AddMinutes(0))
            {
                DrawDiscount1Box(index);
            }
 
            if (show_boxes && Bars.OpenTimes[index] >= LiquidityStart && Bars.OpenTimes[index] <= LiquidityEnd)
            {
                DrawLiquidityBox(index);
            }
 
            if (show_boxes && Bars.OpenTimes[index] >= LiquidityStart.AddHours(3).AddMinutes(30) && Bars.OpenTimes[index] <= LiquidityStart.AddHours(9).AddMinutes(30))
            {
                DrawDiscount2Box(index);
            }
 
            if (show_boxes && Bars.OpenTimes[index] >= LiquidityStart.AddHours(5).AddMinutes(30) && Bars.OpenTimes[index] <= LiquidityStart.AddHours(8).AddMinutes(30))
            {
                RushDiscountBox(index);
            }
 
            if (show_boxes && Bars.OpenTimes[index] >= LiquidityStart.AddHours(8).AddMinutes(0) && Bars.OpenTimes[index] <= LiquidityStart.AddHours(12).AddMinutes(0))
            {
                DrawUS(index);
            }
 
            foreach (KeyValuePair<double, List<ChartTrendLine>> entry in DiscountLevels)
            {
                if (entry.Key >= Bars.LowPrices[index] && entry.Key <= Bars.HighPrices[index])
                {
                    List<ChartTrendLine> lines = DiscountLevels[entry.Key];
                    lines.ForEach(delegate(ChartTrendLine line)
                    {
                        if (line.ExtendToInfinity)
                        {
                            line.ExtendToInfinity = false;
                            if (RangeStart.DayOfWeek != DayOfWeek.Friday)
                            {
                                line.Time2 = RangeStart.AddDays(1);
                            }
                            // Shading extended area
                            bool IsUp = line.Name.IndexOf("UP") >= 0;
                            DiscountRects[line.Time1].ForEach(delegate(ChartRectangle rectangle)
                            {
                                if (IsUp && rectangle.Name.IndexOf("UP") >= 0 || !IsUp && rectangle.Name.IndexOf("DN") >= 0)
                                {
                                    rectangle.Time2 = Bars.OpenTimes[index];
                                    if (Bars.OpenTimes[index] > line.Time2)
                                        line.Time2 = Bars.OpenTimes[index];
                                }
                            });
                        }
 
                    });
                }
            }
        }
 
 
 
        private Tuple<double, double> GetOpenCloseRange()
        {
            int StartIndex = HourlyBars.OpenTimes.GetIndexByTime(RangeStart);
            int index = HourlyBars.OpenTimes.GetIndexByTime(RangeEnd) - 1;
 
            double Max = 0;
            double Min = double.PositiveInfinity;
            while (index >= StartIndex)
            {
                Max = Math.Max(Max, Math.Max(HourlyBars.OpenPrices[index], HourlyBars.ClosePrices[index]));
                Min = Math.Min(Min, Math.Min(HourlyBars.OpenPrices[index], HourlyBars.ClosePrices[index]));
                index--;
            }
            return Tuple.Create(Max, Min);
        }
 
        private Tuple<double, double> CalculateMM(Tuple<double, double> range, double percentage)
        {
            double deviation = (range.Item1 - range.Item2) * (percentage / 100.0);
 
            double h1 = range.Item1 + deviation / 2;
            double h2 = range.Item1 + deviation;
            double h3 = (h1 + h2) / 2;
 
            double l1 = range.Item2 - deviation / 2;
            double l2 = range.Item2 - deviation;
            double l3 = (l1 + l2) / 2;
 
            return Tuple.Create(h3, l3);
        }
 
        private List<ChartTrendLine> GetLevelList(double level)
        {
            if (!DiscountLevels.ContainsKey(level))
                DiscountLevels.Add(level, new List<ChartTrendLine>());
 
            return DiscountLevels[level];
        }
 
        private void DrawMM(int index)
        {
 
 
            if (IsCBDR)
            {
                Tuple<double, double> _crange = GetOpenCloseRange();
                Tuple<double, double> _values1a = CalculateMM(_crange, 133.3);
                Tuple<double, double> _values2a = CalculateMM(_crange, 200);
                Tuple<double, double> _values3a = CalculateMM(_crange, 266.7);
 
// Change the _value number refernce if you need a different line broken before zone ends
                var UpList = GetLevelList(_values3a.Item1);
 
                ChartTrendLine line = Chart.DrawTrendLine("MM_UP1a_" + index, RangeEnd, _values1a.Item1, RangeEnd.AddHours(24), _values1a.Item1, Color.FromName(CbdrUColor), CbdrLThc, CbdrLLS);
                line.ExtendToInfinity = true;
                UpList.Add(line);
 
 
 
 
 
                var DownList = GetLevelList(_values3a.Item2);
 
                line = Chart.DrawTrendLine("MM_DN1a_" + index, RangeEnd, _values1a.Item2, RangeEnd.AddHours(24), _values1a.Item2, Color.FromName(CbdrDColor), CbdrLThc, CbdrLLS);
                line.ExtendToInfinity = true;
                DownList.Add(line);
 
 
 
                List<ChartRectangle> rectList = new List<ChartRectangle>();
 
                ChartRectangle cr1 = Chart.DrawRectangle("Rect_MMUP0a_3a" + RangeEnd, RangeEnd, _values1a.Item1, Present, _values3a.Item1, CbdrUColorWAlpha, CbdrLThc);
                cr1.IsFilled = CbdrFilled;
                rectList.Add(cr1);
                ChartRectangle cr2 = Chart.DrawRectangle("Rect_MMDN0a_3a" + RangeEnd, RangeEnd, _values1a.Item2, Present, _values3a.Item2, CbdrDColorWAlpha, CbdrLThc);
                cr2.IsFilled = CbdrFilled;
                rectList.Add(cr2);
 
                DiscountRects.Add(RangeEnd, rectList);
            }
 
        }
 
        private void DrawRangeBox(int index)
        {
            double Max = FindRangeMax(index);
            double Min = FindRangeMin(index);
            double range = Math.Round((Max - Min) / Symbol.PipSize, 2);
 
            if (RangeList.Count == RangePeriod)
            {
                RangeList.RemoveAt(0);
            }
            RangeList.Add(range);
 
            double average = GetAverageRange();
 
 
            Chart.DrawRectangle("Range_" + RangeStart, RangeStart, Max, RangeEnd, Min, range > average ? RangeOutColorWAlpha : RangeColorWAlpha).IsFilled = true;
            Chart.DrawText("Range_Text_" + RangeStart, "" + range, RangeEnd.AddHours(-3), Min, range > average ? Color.FromName(RangeOutColor) : Color.FromName(RangeColor));
        }
 
        private double GetAverageRange()
        {
            double total = 0;
            foreach (double range in RangeList)
            {
                total += double.IsNaN(range) ? 0 : range;
            }
 
            return Math.Round(total / RangeList.Count, 2);
        }
 
        private double FindRangeMax(int index)
        {
            int StartIndex = Bars.OpenTimes.GetIndexByTime(RangeStart);
            double Max = 0;
            while (index >= StartIndex)
            {
                Max = Math.Max(Max, Bars.HighPrices[index]);
                index--;
            }
            return Max;
        }
 
        private double FindRangeMin(int index)
        {
            int StartIndex = Bars.OpenTimes.GetIndexByTime(RangeStart);
            double Min = double.PositiveInfinity;
            while (index >= StartIndex)
            {
                Min = Math.Min(Min, Bars.LowPrices[index]);
                index--;
            }
            return Min;
        }
 
        private void DrawDiscount1Box(int index)
        {
            double Max = FindDiscount1Max(index);
            double Min = FindDiscount1Min(index);
 
            Chart.DrawRectangle("Discount1_" + LiquidityStart, LiquidityStart.AddHours(-6).AddMinutes(0), Max, LiquidityStart.AddHours(1).AddMinutes(0), Min, Discount1ColorWAlpha).IsFilled = true;
 
        }
 
        private double FindDiscount1Max(int index)
        {
            int StartIndex = Bars.OpenTimes.GetIndexByTime(LiquidityStart.AddHours(-6).AddMinutes(0));
            double Max = 0;
            while (index >= StartIndex)
            {
                Max = Math.Max(Max, Bars.HighPrices[index]);
                index--;
            }
            return Max;
        }
 
        private double FindDiscount1Min(int index)
        {
            int StartIndex = Bars.OpenTimes.GetIndexByTime(LiquidityStart.AddHours(-6).AddMinutes(0));
            double Min = double.PositiveInfinity;
            while (index >= StartIndex)
            {
                Min = Math.Min(Min, Bars.LowPrices[index]);
                index--;
            }
            return Min;
        }
 
 
        private void DrawLiquidityBox(int index)
        {
            double Max = FindLiquidityMax(index);
            double Min = FindLiquidityMin(index);
 
            Chart.DrawRectangle("Liquidity_" + LiquidityStart, LiquidityStart, Max, LiquidityEnd, Min, LiquidityColorWAlpha).IsFilled = true;
 
        }
 
        private double FindLiquidityMax(int index)
        {
            int StartIndex = Bars.OpenTimes.GetIndexByTime(LiquidityStart);
            double Max = 0;
            while (index >= StartIndex)
            {
                Max = Math.Max(Max, Bars.HighPrices[index]);
                index--;
            }
            return Max;
        }
 
        private double FindLiquidityMin(int index)
        {
            int StartIndex = Bars.OpenTimes.GetIndexByTime(LiquidityStart);
            double Min = double.PositiveInfinity;
            while (index >= StartIndex)
            {
                Min = Math.Min(Min, Bars.LowPrices[index]);
                index--;
            }
            return Min;
        }
 
        private void DrawDiscount2Box(int index)
        {
            double Max = FindDiscount2Max(index);
            double Min = FindDiscount2Min(index);
 
            Chart.DrawRectangle("Discount2_" + LiquidityStart, LiquidityStart.AddHours(3).AddMinutes(30), Max, LiquidityStart.AddHours(9).AddMinutes(30), Min, Discount2ColorWAlpha).IsFilled = true;
 
        }
 
        private double FindDiscount2Max(int index)
        {
            int StartIndex = Bars.OpenTimes.GetIndexByTime(LiquidityStart.AddHours(3));
            double Max = 0;
            while (index >= StartIndex)
            {
                Max = Math.Max(Max, Bars.HighPrices[index]);
                index--;
            }
            return Max;
        }
 
        private double FindDiscount2Min(int index)
        {
            int StartIndex = Bars.OpenTimes.GetIndexByTime(LiquidityStart.AddHours(3));
            double Min = double.PositiveInfinity;
            while (index >= StartIndex)
            {
                Min = Math.Min(Min, Bars.LowPrices[index]);
                index--;
            }
            return Min;
        }
 
        private void RushDiscountBox(int index)
        {
            double Max = FindRushDiscountMax(index);
            double Min = FindRushDiscountMin(index);
 
            Chart.DrawRectangle("RushDiscount_" + LiquidityStart, LiquidityStart.AddHours(5).AddMinutes(30), Max, LiquidityStart.AddHours(8).AddMinutes(30), Min, RushDiscountColorWAlpha).IsFilled = true;
 
        }
 
        private double FindRushDiscountMax(int index)
        {
            int StartIndex = Bars.OpenTimes.GetIndexByTime(LiquidityStart.AddHours(5).AddMinutes(30));
            double Max = 0;
            while (index >= StartIndex)
            {
                Max = Math.Max(Max, Bars.HighPrices[index]);
                index--;
            }
            return Max;
        }
 
        private double FindRushDiscountMin(int index)
        {
            int StartIndex = Bars.OpenTimes.GetIndexByTime(LiquidityStart.AddHours(5).AddMinutes(30));
            double Min = double.PositiveInfinity;
            while (index >= StartIndex)
            {
                Min = Math.Min(Min, Bars.LowPrices[index]);
                index--;
            }
            return Min;
        }
 
 
        private void DrawUS(int index)
        {
            double Max = FindUSMax(index);
            double Min = FindUSMin(index);
 
            Chart.DrawRectangle("US_Area_" + LiquidityStart, LiquidityStart.AddHours(8).AddMinutes(0), Max, LiquidityStart.AddHours(12).AddMinutes(0), Min, USColorWAlpha).IsFilled = true;
 
        }
 
        private double FindUSMax(int index)
        {
            int StartIndex = Bars.OpenTimes.GetIndexByTime(LiquidityStart.AddHours(8).AddMinutes(0));
            double Max = 0;
            while (index >= StartIndex)
            {
                Max = Math.Max(Max, Bars.HighPrices[index]);
                index--;
            }
            return Max;
        }
 
        private double FindUSMin(int index)
        {
            int StartIndex = Bars.OpenTimes.GetIndexByTime(LiquidityStart.AddHours(8).AddMinutes(0));
            double Min = double.PositiveInfinity;
            while (index >= StartIndex)
            {
                Min = Math.Min(Min, Bars.LowPrices[index]);
                index--;
            }
            return Min;
        }
 
    }
}