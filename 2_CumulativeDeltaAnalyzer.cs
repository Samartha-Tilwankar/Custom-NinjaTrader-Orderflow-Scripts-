using System;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;

namespace NinjaTrader.NinjaScript.Indicators
{
    public class CumulativeDeltaAnalyzer : Indicator {
        private double cumulativeDelta = 0;
        private double previousClose = 0;
        private double sessionDelta = 0;
        protected override void OnStateChange() {
            if (State == State.SetDefaults) {
                Description = "Cumulative Delta Analyzer - Tracks difference between up volume and down volume";
                Name = "CumulativeDeltaAnalyzer";
                Calculate = Calculate.OnBarClose;
                IsOverlay = false;
                DisplayInDataBox = true;
                AddPlot(Brushes.Green, "Delta");
                AddPlot(Brushes.Red, "SessionDelta");
                AddLine(Brushes.Gray, 0, "Zero");
            }
        }
        protected override void OnBarUpdate() {
            if (CurrentBar == 0) {
                previousClose = Close[0];
                cumulativeDelta = 0;
                sessionDelta = 0;
            }
            double volume = Volume[0];
            double delta = 0;
            if (Close[0] > previousClose) {
                delta = volume;
            }
            else if (Close[0] < previousClose) {
                delta = -volume;
            }
            else {
                delta = 0;
            }
            cumulativeDelta += delta;
            sessionDelta += delta;
            if (Close[0] < Low[1] || Close[0] > High[1]) {
                if (Math.Abs(sessionDelta) > Math.Abs(delta) * 10) {
                    sessionDelta = delta;
                }
            }
            Values[0][0] = cumulativeDelta;
            Values[1][0] = sessionDelta;
            previousClose = Close[0];
        }
        public override string DisplayName => "Cumulative Delta Analyzer";
    }
}
