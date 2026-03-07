using System;
using System.Collections.Generic;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;

namespace NinjaTrader.NinjaScript.Indicators
{
    public sealed class VolumeAnalysisStrategy {
        private readonly IVolumeStrategy strategy;

        public VolumeAnalysisStrategy(IVolumeStrategy strat) {
            strategy = strat ?? throw new ArgumentNullException(nameof(strat));
        }

        public double Analyze(double[] volumes, double[] prices) {
            return strategy.Calculate(volumes, prices);
        }
    }

    public interface IVolumeStrategy {
        double Calculate(double[] volumes, double[] prices);
        string StrategyName { get; }
    }

    public class VolumeConcentrationStrategy : IVolumeStrategy {
        private readonly int lookbackPeriod;

        public VolumeConcentrationStrategy(int period = 50) {
            lookbackPeriod = period;
        }

        public string StrategyName => "Volume Concentration";

        public double Calculate(double[] volumes, double[] prices) {
            if (volumes.Length < lookbackPeriod)
                return 0;

            double total = 0;
            double max = 0;

            for (int i = 0; i < lookbackPeriod && i < volumes.Length; i++) {
                total += volumes[i];
                if (volumes[i] > max)
                    max = volumes[i];
            }

            return total > 0 ? (max / total) : 0;
        }
    }

    public class VolumeVelocityStrategy : IVolumeStrategy {
        private readonly int period;

        public VolumeVelocityStrategy(int p = 10) {
            period = p;
        }

        public string StrategyName => "Volume Velocity";

        public double Calculate(double[] volumes, double[] prices) {
            if (volumes.Length < period + 1)
                return 0;

            double recentAvg = 0;
            double pastAvg = 0;

            for (int i = 0; i < period && i < volumes.Length; i++)
                recentAvg += volumes[i];

            for (int i = period; i < period * 2 && i < volumes.Length; i++)
                pastAvg += volumes[i];

            recentAvg /= period;
            pastAvg /= period;

            return pastAvg > 0 ? ((recentAvg - pastAvg) / pastAvg) : 0;
        }
    }

    public class VolumeWeightedPriceStrategy : IVolumeStrategy {
        private readonly int period;

        public VolumeWeightedPriceStrategy(int p = 20) {
            period = p;
        }

        public string StrategyName => "VWAP";

        public double Calculate(double[] volumes, double[] prices) {
            if (volumes.Length < period || prices.Length < period)
                return 0;

            double num = 0;
            double den = 0;

            for (int i = 0; i < period; i++) {
                num += volumes[i] * prices[i];
                den += volumes[i];
            }

            return den > 0 ? num / den : 0;
        }
    }

    public class PolyOrderFlowAnalyzer : Indicator {
        private VolumeAnalysisStrategy concentrationStrategy;
        private VolumeAnalysisStrategy velocityStrategy;
        private VolumeAnalysisStrategy vwapStrategy;
        private double[] priceCache;
        private double[] volumeCache;
        private readonly int cacheSize = 50;

        protected override void OnStateChange() {
            if (State == State.SetDefaults) {
                Description = "Poly Order Flow Analyzer - Virtual functions and strategy pattern";
                Name = "PolyOrderFlowAnalyzer";
                Calculate = Calculate.OnBarClose;
                IsOverlay = false;
                DisplayInDataBox = true;

                AddPlot(Brushes.Blue, "Concentration");
                AddPlot(Brushes.Green, "Velocity");
                AddPlot(Brushes.Red, "VWAP");
            }
            else if (State == State.Configure) {
                concentrationStrategy = new VolumeAnalysisStrategy(new VolumeConcentrationStrategy(cacheSize));
                velocityStrategy = new VolumeAnalysisStrategy(new VolumeVelocityStrategy(20));
                vwapStrategy = new VolumeAnalysisStrategy(new VolumeWeightedPriceStrategy(30));

                priceCache = new double[cacheSize];
                volumeCache = new double[cacheSize];
            }
        }

        protected override void OnBarUpdate() {
            if (CurrentBar < cacheSize)
                return;

            for (int i = 0; i < cacheSize - 1; i++) {
                priceCache[i] = priceCache[i + 1];
                volumeCache[i] = volumeCache[i + 1];
            }

            priceCache[cacheSize - 1] = Close[0];
            volumeCache[cacheSize - 1] = Volume[0];

            double conc = concentrationStrategy.Analyze(volumeCache, priceCache);
            double vel = velocityStrategy.Analyze(volumeCache, priceCache);
            double vw = vwapStrategy.Analyze(volumeCache, priceCache);

            Values[0][0] = conc;
            Values[1][0] = vel;
            Values[2][0] = vw > 0 ? vw / Close[0] : 0;
        }

        public override string DisplayName => "Poly Order Flow Analyzer";
    }
}
