using System;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;

namespace NinjaTrader.NinjaScript.Indicators
{
    public class VolumeRateOfChange : Indicator {
        private int rocPeriod = 10;
        private double[] volumeHistory;
        protected override void OnStateChange() {
            if (State == State.SetDefaults) {
                Description = "Volume Rate of Change - Measures acceleration/deceleration of volume";
                Name = "VolumeRateOfChange";
                Calculate = Calculate.OnBarClose;
                IsOverlay = false;
                DisplayInDataBox = true;
                AddPlot(Brushes.DodgerBlue, "VolumeROC");
                AddPlot(Brushes.Green, "VolumeAccel");
                AddLine(Brushes.Gray, 0, "Zero");
            }
        }
        protected override void OnBarUpdate() {
            if (CurrentBar == 0)
                volumeHistory = new double[rocPeriod];
            for (int i = rocPeriod - 1; i > 0; i--)
                volumeHistory[i] = volumeHistory[i - 1];
            volumeHistory[0] = Volume[0];
            double volumeROC = 0;
            if (CurrentBar >= rocPeriod && volumeHistory[rocPeriod - 1] > 0) {
                volumeROC = ((volumeHistory[0] - volumeHistory[rocPeriod - 1]) / volumeHistory[rocPeriod - 1]) * 100;
            }
            double volumeAccel = 0;
            if (CurrentBar > rocPeriod) {
                double previousROC = ((volumeHistory[1] - volumeHistory[rocPeriod]) / (volumeHistory[rocPeriod] + 0.0001)) * 100;
                volumeAccel = volumeROC - previousROC;
            }
            Values[0][0] = volumeROC;
            Values[1][0] = volumeAccel;
        }
        public override string DisplayName => "Volume Rate of Change";
    }
}
