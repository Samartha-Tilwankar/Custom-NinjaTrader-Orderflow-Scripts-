using System;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;

namespace NinjaTrader.NinjaScript.Indicators
{
    public class OrderImbalanceDetector : Indicator {
        private int lookbackBars = 20;
        private double imbalanceThreshold = 0.6;
        protected override void OnStateChange() {
            if (State == State.SetDefaults) {
                Description = "Order Imbalance Detector - Identifies significant bid/ask imbalances";
                Name = "OrderImbalanceDetector";
                Calculate = Calculate.OnBarClose;
                IsOverlay = false;
                DisplayInDataBox = true;
                AddPlot(Brushes.DodgerBlue, "ImbalanceRatio");
                AddPlot(Brushes.Orange, "Signal");
                AddLine(Brushes.Gray, imbalanceThreshold, "Threshold");
            }
        }
        protected override void OnBarUpdate() {
            if (CurrentBar < lookbackBars)
                return;
            double upVolume = 0;
            double downVolume = 0;
            for (int i = 0; i < lookbackBars; i++) {
                if (Close[i] > Open[i])
                    upVolume += Volume[i];
                else if (Close[i] < Open[i])
                    downVolume += Volume[i];
                else
                    upVolume += Volume[i] * 0.5;
            }
            double totalVolume = upVolume + downVolume;
            double imbalanceRatio = totalVolume > 0 ? Math.Max(upVolume, downVolume) / totalVolume : 0;
            double signal = imbalanceRatio > imbalanceThreshold ? 1 : (imbalanceRatio < (1 - imbalanceThreshold) ? -1 : 0);
            Values[0][0] = imbalanceRatio;
            Values[1][0] = signal;
        }
        public override string DisplayName => "Order Imbalance Detector";
    }
}
