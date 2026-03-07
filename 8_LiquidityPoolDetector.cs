using System;
using System.Collections.Generic;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;

namespace NinjaTrader.NinjaScript.Indicators
{
    public class LiquidityPoolDetector : Indicator {
        private int lookbackBars = 100;
        private Dictionary<double, int> priceLevelTouches;
        private double liquidityThreshold = 5;
        protected override void OnStateChange() {
            if (State == State.SetDefaults) {
                Description = "Liquidity Pool Detector - Identifies areas of high liquidity and order clustering";
                Name = "LiquidityPoolDetector";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = true;
                AddPlot(Brushes.Cyan, "LiquidityZone");
                AddPlot(Brushes.Yellow, "PoolStrength");
            }
            else if (State == State.Configure) {
                priceLevelTouches = new Dictionary<double, int>();
            }
        }
        protected override void OnBarUpdate() {
            if (CurrentBar < lookbackBars)
                return;
            priceLevelTouches.Clear();
            for (int i = 0; i < lookbackBars; i++) {
                double highBucket = Math.Round(High[i] / 0.5) * 0.5;
                double lowBucket = Math.Round(Low[i] / 0.5) * 0.5;
                double closeBucket = Math.Round(Close[i] / 0.5) * 0.5;
                if (!priceLevelTouches.ContainsKey(highBucket))
                    priceLevelTouches[highBucket] = 0;
                if (!priceLevelTouches.ContainsKey(lowBucket))
                    priceLevelTouches[lowBucket] = 0;
                if (!priceLevelTouches.ContainsKey(closeBucket))
                    priceLevelTouches[closeBucket] = 0;
                priceLevelTouches[highBucket]++;
                priceLevelTouches[lowBucket]++;
                priceLevelTouches[closeBucket]++;
            }
            int maxTouches = 0;
            double liquidityZone = Close[0];
            double poolStrength = 0;
            foreach (var kvp in priceLevelTouches) {
                if (kvp.Value > maxTouches && kvp.Value >= liquidityThreshold) {
                    maxTouches = kvp.Value;
                    liquidityZone = kvp.Key;
                    poolStrength = kvp.Value;
                }
            }
            Values[0][0] = liquidityZone;
            Values[1][0] = poolStrength / lookbackBars;
        }
        public override string DisplayName => "Liquidity Pool Detector";
    }
}
