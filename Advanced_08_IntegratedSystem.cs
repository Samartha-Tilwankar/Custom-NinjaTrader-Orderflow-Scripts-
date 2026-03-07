using System;
using System.Collections.Generic;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;

namespace NinjaTrader.NinjaScript.Indicators
{
    public class IntegratedOrderFlowSystem : Indicator {
        private IVolumeStrategy volStrat;
        private TypeSafeCalculator calc;
        private SnaphotBuffer snapBuf;
        private RingBuffer deltaRing;
        private readonly ConfigurationManager cfg;
        private int barsProcessed = 0;
        private double cumulativeDeviation = 0;

        public IntegratedOrderFlowSystem() {
            cfg = ConfigurationManager.Instance;
            volStrat = new VolumeConcentrationStrategy(20);
        }

        protected override void OnStateChange() {
            if (State == State.SetDefaults) {
                Description = "Integrated Order Flow System - All concepts combined";
                Name = "IntegratedOrderFlowSystem";
                Calculate = Calculate.OnBarClose;
                IsOverlay = false;
                DisplayInDataBox = true;

                AddPlot(Brushes.DodgerBlue, "MainSignal");
                AddPlot(Brushes.Green, "QualityScore");
                AddPlot(Brushes.Red, "RiskLevel");
                AddPlot(Brushes.Orange, "Efficiency");
                AddPlot(Brushes.Purple, "Sentiment");
            }
            else if (State == State.Configure) {
                cfg.Set("system_enabled", true);
                calc = new TypeSafeCalculator();
                snapBuf = new SnaphotBuffer(100);
                deltaRing = new RingBuffer(30);
            }
        }

        protected override void OnBarUpdate() {
            try {
                if (!ValidateInputs())
                    return;

                ProcessBar();
                CalculateMetrics();
                UpdateSignals();
                barsProcessed++;
            }
            catch (Exception ex) {
                HandleCriticalError(ex);
            }
        }

        private bool ValidateInputs() {
            if (Close[0] <= 0 || Volume[0] <= 0)
                return false;
            if (CurrentBar < 5)
                return false;
            bool sysEnabled = (bool)(cfg.Get("system_enabled") ?? true);
            return sysEnabled;
        }

        private void ProcessBar() {
            double delta = Close[0] - (CurrentBar > 0 ? Close[1] : Close[0]);
            deltaRing.Append(delta);

            var snap = new OrderFlowSnapshot {
                Timestamp = CurrentBar,
                Close = Close[0],
                Volume = Volume[0],
                Delta = Math.Sign(delta) * Volume[0]
            };
            snapBuf.Add(snap);
        }

        private void CalculateMetrics() {
            var priceArr = new double[5];
            var volArr = new double[5];

            for (int i = 0; i < 5; i++) {
                if (CurrentBar >= i) {
                    priceArr[4 - i] = Closes[i];
                    volArr[4 - i] = Volumes[i];
                }
            }

            var res = calc.CalculateDelta(priceArr, volArr);
            
            double mainSignal = res.Match(
                val => val / (volArr[4] + 0.0001),
                err => 0
            );

            double concentration = volStrat.Calculate(volArr, priceArr);
            double riskLevel = CalcRisk(concentration);
            double efficiency = CalcEfficiency();
            double sentiment = CalcSentiment();

            Values[0][0] = mainSignal;
            Values[1][0] = concentration;
            Values[2][0] = riskLevel;
            Values[3][0] = efficiency;
            Values[4][0] = sentiment;
        }

        private void UpdateSignals() {
            if (barsProcessed % 5 == 0) {
                cumulativeDeviation = CalculateStats();
            }
        }

        private double CalcRisk(double conc) {
            double baseRisk = conc > 0.7 ? 0.8 : (conc > 0.5 ? 0.5 : 0.2);
            double volRisk = Volume[0] > Volume[1] * 1.5 ? 0.3 : 0;
            return Math.Min(baseRisk + volRisk, 1.0);
        }

        private double CalcEfficiency() {
            double range = High[0] - Low[0];
            double avgRange = 0;
            for (int i = 0; i < 5; i++) {
                if (CurrentBar >= i)
                    avgRange += (High[i] - Low[i]);
            }
            avgRange /= 5;
            return avgRange > 0 ? range / avgRange : 0;
        }

        private double CalcSentiment() {
            double bullish = 0;
            for (int i = 0; i < 10; i++) {
                if (CurrentBar >= i && Close[i] > Open[i])
                    bullish++;
            }
            return (bullish / 10) * 2 - 1;
        }

        private double CalculateStats() {
            double sum = 0;
            double count = 0;
            for (int i = 0; i < 20; i++) {
                if (CurrentBar >= i) {
                    sum += Math.Abs(Close[i] - Close[i + 1]);
                    count++;
                }
            }
            return count > 0 ? sum / count : 0;
        }

        private void HandleCriticalError(Exception ex) {
            Values[2][0] = 1.0;
        }

        public override string DisplayName => "Integrated Order Flow System";

        ~IntegratedOrderFlowSystem() {
            snapBuf?.Clear();
            deltaRing?.Clear();
        }
    }

    public class AdvancedDemo : Indicator {
        protected override void OnStateChange() {
            if (State == State.SetDefaults) {
                Description = "Advanced Demo - Shows all pattern applications";
                Name = "AdvancedDemo";
                Calculate = Calculate.OnBarClose;
                IsOverlay = false;
                DisplayInDataBox = true;

                AddPlot(Brushes.Blue, "Demo");
            }
        }

        protected override void OnBarUpdate() {
            if (CurrentBar < 10)
                return;

            DemoBasePatternsBase();
            DemoMemoryMgmt();
            DemoPolymorphism();
            DemoErrorHandling();

            double combined = CalculateFinal();
            Values[0][0] = combined;
        }

        private void DemoBasePatternsBase() {
            // Shows encapsulation, abstraction
            var snap = new OrderFlowSnapshot {
                Timestamp = CurrentBar,
                Close = Close[0],
                Volume = Volume[0],
                Delta = Volume[0]
            };
        }

        private void DemoMemoryMgmt() {
            // Shows RAII, pooling
            var pool = new ObjectPool<OrderFlowSnapshot>(10);
            var item = pool.Rent();
            pool.Return(item);
        }

        private void DemoPolymorphism() {
            // Shows strategy pattern, virtual functions
            IVolumeStrategy strat = new VolumeConcentrationStrategy(5);
            var prices = new double[5];
            var vols = new double[5];
            for (int i = 0; i < 5; i++) {
                if (CurrentBar >= i) {
                    prices[4 - i] = Closes[i];
                    vols[4 - i] = Volumes[i];
                }
            }
            double res = strat.Calculate(vols, prices);
        }

        private void DemoErrorHandling() {
            // Shows type-safe error handling
            var calc = new TypeSafeCalculator();
            var prices = new List<double> { Close[0], Close[1] };
            var vols = new List<double> { Volume[0], Volume[1] };
            var result = calc.CalculateDelta(prices, vols);
            result.MatchVoid(
                val => { },
                err => { }
            );
        }

        private double CalculateFinal() {
            double score = 0;
            
            if (Close[0] > Close[1]) score += 0.2;
            if (Volume[0] > Volume[1]) score += 0.2;
            if (High[0] > High[1]) score += 0.2;
            if (Close[0] > Open[0]) score += 0.2;
            if (Volume[0] > (Volume[1] + Volume[2]) / 2) score += 0.2;

            return Math.Min(score, 1.0);
        }

        public override string DisplayName => "Advanced Demo";
    }
}
