using System;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;

namespace NinjaTrader.NinjaScript.Indicators
{
    public class BuySellPressureIndicator : Indicator {
        private int lookbackPeriod = 10;
        private double buyingPressure = 0;
        private double sellingPressure = 0;
        protected override void OnStateChange() {
            if (State == State.SetDefaults) {
                Description = "Buy/Sell Pressure Indicator - Measures buying vs selling intensity";
                Name = "BuySellPressureIndicator";
                Calculate = Calculate.OnBarClose;
                IsOverlay = false;
                DisplayInDataBox = true;
                AddPlot(Brushes.Green, "BuyPressure");
                AddPlot(Brushes.Red, "SellPressure");
                AddPlot(Brushes.DodgerBlue, "NetPressure");
            }
        }
        protected override void OnBarUpdate() {
            if (CurrentBar < lookbackPeriod)
                return;
            buyingPressure = 0;
            sellingPressure = 0;
            for (int i = 0; i < lookbackPeriod; i++) {
                double upStrength = (Close[i] - Low[i]) / (High[i] - Low[i] + 0.0001);
                buyingPressure += Volume[i] * upStrength;
                double downStrength = (High[i] - Close[i]) / (High[i] - Low[i] + 0.0001);
                sellingPressure += Volume[i] * downStrength;
            }
            double totalPressure = buyingPressure + sellingPressure;
            if (totalPressure > 0) {
                buyingPressure /= totalPressure;
                sellingPressure /= totalPressure;
            }
            double netPressure = buyingPressure - sellingPressure;
            Values[0][0] = buyingPressure;
            Values[1][0] = sellingPressure;
            Values[2][0] = netPressure;
        }
        public override string DisplayName => "Buy/Sell Pressure Indicator";
    }
}
