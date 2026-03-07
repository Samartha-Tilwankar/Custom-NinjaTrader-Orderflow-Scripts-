using System;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;

namespace NinjaTrader.NinjaScript.Indicators
{
    public class OrderFlowMomentum : Indicator {
        private int momentumPeriod = 14;
        private double[] deltaValues;
        private double[] volumeValues;
        protected override void OnStateChange() {
            if (State == State.SetDefaults) {
                Description = "Order Flow Momentum - Momentum based on order flow directionality and intensity";
                Name = "OrderFlowMomentum";
                Calculate = Calculate.OnBarClose;
                IsOverlay = false;
                DisplayInDataBox = true;
                AddPlot(Brushes.DodgerBlue, "Momentum");
                AddPlot(Brushes.Green, "FastMA");
                AddPlot(Brushes.Red, "SlowMA");
                AddLine(Brushes.Gray, 0, "Zero");
            }
        }
        protected override void OnBarUpdate() {
            if (CurrentBar == 0) {
                deltaValues = new double[momentumPeriod];
                volumeValues = new double[momentumPeriod];
            }
            for (int i = momentumPeriod - 1; i > 0; i--) {
                deltaValues[i] = deltaValues[i - 1];
                volumeValues[i] = volumeValues[i - 1];
            }
            double delta = 0;
            if (Close[0] > Open[0])
                delta = Volume[0];
            else if (Close[0] < Open[0])
                delta = -Volume[0];
            else
                delta = 0;
            deltaValues[0] = delta;
            volumeValues[0] = Volume[0];
            double momentum = 0;
            double fastMA = 0;
            double slowMA = 0;
            if (CurrentBar >= momentumPeriod) {
                double cumulativeDelta = 0;
                double totalVolume = 0;
                for (int i = 0; i < momentumPeriod; i++) {
                    cumulativeDelta += deltaValues[i];
                    totalVolume += volumeValues[i];
                }
                momentum = totalVolume > 0 ? cumulativeDelta / totalVolume * 100 : 0;
                fastMA = (momentum + (CurrentBar > 0 ? Values[0][1] : momentum)) / 2;
                slowMA = (momentum + (CurrentBar > 1 ? Values[2][1] : momentum) * 2) / 3;
            }
            Values[0][0] = momentum;
            Values[1][0] = fastMA;
            Values[2][0] = slowMA;
        }
        public override string DisplayName => "Order Flow Momentum";
    }
}
