using System;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;

namespace NinjaTrader.NinjaScript.Indicators
{
    public class MarketMicrostructureAnalyzer : Indicator {
        private int barRange = 50;
        private double highestHigh = 0;
        private double lowestLow = 0;
        private double volumeWeightedRange = 0;
        protected override void OnStateChange() {
            if (State == State.SetDefaults) {
                Description = "Market Microstructure Analyzer - Analyzes order flow patterns and price structure";
                Name = "MarketMicrostructureAnalyzer";
                Calculate = Calculate.OnBarClose;
                IsOverlay = false;
                DisplayInDataBox = true;
                AddPlot(Brushes.Blue, "Efficiency");
                AddPlot(Brushes.Green, "Volatility");
                AddPlot(Brushes.Red, "Reversal Signal");
            }
        }
        protected override void OnBarUpdate() {
            if (CurrentBar < barRange)
                return;
            highestHigh = High[0];
            lowestLow = Low[0];
            double totalVolume = 0;
            double directionalVolume = 0;
            for (int i = 0; i < barRange; i++) {
                if (High[i] > highestHigh)
                    highestHigh = High[i];
                if (Low[i] < lowestLow)
                    lowestLow = Low[i];
                totalVolume += Volume[i];
                if (Close[i] > Open[i])
                    directionalVolume += Volume[i];
            }
            double priceRange = highestHigh - lowestLow;
            double efficiency = totalVolume > 0 ? (priceRange / totalVolume) * 10000 : 0;
            double sumSquaredReturns = 0;
            for (int i = 1; i < barRange; i++) {
                double ret = Math.Log(Close[i] / Close[i + 1]);
                sumSquaredReturns += ret * ret;
            }
            double volatility = Math.Sqrt(sumSquaredReturns / barRange);
            double directionalRatio = totalVolume > 0 ? directionalVolume / totalVolume : 0.5;
            double reversalSignal = 0;
            if ((Close[0] > Close[barRange - 1] && directionalRatio < 0.4) ||
                (Close[0] < Close[barRange - 1] && directionalRatio > 0.6)) {
                reversalSignal = 1;
            }
            Values[0][0] = efficiency;
            Values[1][0] = volatility;
            Values[2][0] = reversalSignal;
        }
        public override string DisplayName => "Market Microstructure Analyzer";
    }
}
