using System;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;

namespace NinjaTrader.NinjaScript.Indicators
{
    public class SmartMoneyFlowIndex : Indicator {
        private int smfPeriod = 20;
        private double[] priceChanges;
        private double[] volumeValues;
        protected override void OnStateChange() {
            if (State == State.SetDefaults) {
                Description = "Smart Money Flow Index - Identifies institutional order flow and money flow patterns";
                Name = "SmartMoneyFlowIndex";
                Calculate = Calculate.OnBarClose;
                IsOverlay = false;
                DisplayInDataBox = true;
                AddPlot(Brushes.DodgerBlue, "SmartMoneyFlow");
                AddPlot(Brushes.Green, "BuyingFlow");
                AddPlot(Brushes.Red, "SellingFlow");
                AddLine(Brushes.Gray, 50, "Neutral");
            }
        }
        protected override void OnBarUpdate() {
            if (CurrentBar == 0) {
                priceChanges = new double[smfPeriod];
                volumeValues = new double[smfPeriod];
            }
            for (int i = smfPeriod - 1; i > 0; i--) {
                priceChanges[i] = priceChanges[i - 1];
                volumeValues[i] = volumeValues[i - 1];
            }
            double priceChange = Close[0] - Open[0];
            priceChanges[0] = priceChange;
            volumeValues[0] = Volume[0];
            double buyingFlow = 0;
            double sellingFlow = 0;
            double totalVolume = 0;
            if (CurrentBar >= smfPeriod) {
                for (int i = 0; i < smfPeriod; i++) {
                    totalVolume += volumeValues[i];
                    if (priceChanges[i] > 0)
                        buyingFlow += volumeValues[i] * (priceChanges[i] / (Math.Abs(priceChanges[i]) + 0.0001));
                    else
                        sellingFlow += volumeValues[i] * (-priceChanges[i] / (Math.Abs(priceChanges[i]) + 0.0001));
                }
                if (totalVolume > 0) {
                    buyingFlow /= totalVolume;
                    sellingFlow /= totalVolume;
                }
            }
            double smartMoneyFlow = totalVolume > 0 ? (buyingFlow / (buyingFlow + sellingFlow + 0.0001)) * 100 : 50;
            Values[0][0] = smartMoneyFlow;
            Values[1][0] = buyingFlow * 100;
            Values[2][0] = sellingFlow * 100;
        }
        public override string DisplayName => "Smart Money Flow Index";
    }
}
