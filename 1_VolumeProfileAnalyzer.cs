using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;

namespace NinjaTrader.NinjaScript.Indicators
{
    public class VolumeProfileAnalyzer : Indicator {
        private Dictionary<double, double> volumeProfile;
        private double valueArea;
        private double pointOfControl;
        private int profileBars = 50;
        protected override void OnStateChange() {
            if (State == State.SetDefaults) {
                Description = "Volume Profile Analyzer - Analyzes cumulative volume at different price levels";
                Name = "VolumeProfileAnalyzer";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = true;
                AddPlot(Brushes.DodgerBlue, "POC");
                AddPlot(Brushes.LimeGreen, "ValueArea");
            }
            else if (State == State.Configure) {
                volumeProfile = new Dictionary<double, double>();
            }
        }
        protected override void OnBarUpdate() {
            if (CurrentBar < profileBars)
                return;
            volumeProfile.Clear();
            double totalVolume = 0;
            for (int i = CurrentBar - profileBars; i <= CurrentBar; i++) {
                double price = Closes[i];
                double volume = Volumes[i];
                double roundedPrice = Math.Round(price, 2);
                if (!volumeProfile.ContainsKey(roundedPrice))
                    volumeProfile[roundedPrice] = 0;
                volumeProfile[roundedPrice] += volume;
                totalVolume += volume;
            }
            double maxVolume = 0;
            pointOfControl = Close[0];
            foreach (var kvp in volumeProfile) {
                if (kvp.Value > maxVolume) {
                    maxVolume = kvp.Value;
                    pointOfControl = kvp.Key;
                }
            }
            double cumulativeVolume = 0;
            double targetVolume = totalVolume * 0.35;
            foreach (var kvp in volumeProfile) {
                cumulativeVolume += kvp.Value;
                if (cumulativeVolume >= targetVolume) {
                    valueArea = kvp.Key;
                    break;
                }
            }
            Values[0][0] = pointOfControl;
            Values[1][0] = valueArea;
        }
        public override string DisplayName => "Volume Profile Analyzer";
    }
}
