using System;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;

namespace NinjaTrader.NinjaScript.Indicators
{
    public class VWAPDeltaDivergence : Indicator {
        private double vwap = 0;
        private double cumulativePV = 0;
        private double cumulativeVolume = 0;
        private double previousDelta = 0;
        private double deltaMA = 0;
        protected override void OnStateChange() {
            if (State == State.SetDefaults) {
                Description = "VWAP with Delta Divergence - Identifies divergences between price and volume-weighted delta";
                Name = "VWAPDeltaDivergence";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = true;
                AddPlot(Brushes.Purple, "VWAP");
                AddPlot(Brushes.Orange, "DeltaMA");
                AddPlot(Brushes.Red, "Divergence");
            }
        }
        protected override void OnBarUpdate() {
            cumulativePV += (Close[0] + High[0] + Low[0]) / 3.0 * Volume[0];
            cumulativeVolume += Volume[0];
            vwap = cumulativeVolume > 0 ? cumulativePV / cumulativeVolume : Close[0];
            double currentDelta = 0;
            if (Close[0] > Open[0])
                currentDelta = Volume[0];
            else if (Close[0] < Open[0])
                currentDelta = -Volume[0];
            if (CurrentBar == 0)
                deltaMA = currentDelta;
            else
                deltaMA = (deltaMA * 0.8) + (currentDelta * 0.2);
            double priceChange = Close[0] - Close[1];
            double divergence = (priceChange > 0 && deltaMA < 0) || (priceChange < 0 && deltaMA > 0) ? 1 : 0;
            Values[0][0] = vwap;
            Values[1][0] = deltaMA / 1000;
            Values[2][0] = divergence;
            previousDelta = currentDelta;
        }
        public override string DisplayName => "VWAP Delta Divergence";
    }
}
