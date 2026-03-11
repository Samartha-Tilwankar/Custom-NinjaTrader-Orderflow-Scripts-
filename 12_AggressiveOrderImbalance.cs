// Aggressive Order Imbalance — Indian Market Edition
// Detects bars where aggressive market orders are hitting one side of the book
// with statistically unusual force, using a tick-rule delta proxy and Z-score
// normalisation over a rolling window.
//
// Design rationale for Indian markets (NSE/BSE/MCX)
// --------------------------------------------------
//   • Spoofing is rare (SEBI surveillance + smaller participant base), so an
//     unusually large delta is more likely to be genuine directional conviction.
//   • Lot sizes on NSE F&O are fixed (Nifty 50 = 75 lots, BankNifty = 15 lots),
//     so absolute volume thresholds can be tuned per instrument.
//   • Indian cash and F&O sessions are 09:15–15:30 IST with a defined lunch lull;
//     the Z-score window naturally adapts to within-session volume rhythm.
//
// Core math (per bar)
// -------------------
//   BuyVolEst  = Volume * (Close - Low)  / (High - Low + TickSize)
//   SellVolEst = Volume * (High - Close) / (High - Low + TickSize)
//   Delta      = BuyVolEst  - SellVolEst
//   ImbalRatio = Delta / Volume                    → range [-1, +1]
//
//   Over a rolling window of length N:
//   DeltaMean  = EMA(Delta, N)
//   DeltaStd   = sqrt( EMA((Delta - DeltaMean)^2, N) )
//   DeltaZ     = (Delta - DeltaMean) / (DeltaStd + epsilon)
//
//   Aggressive buy  signal: DeltaZ >  zScoreThreshold  AND ImbalRatio > imbalRatioMin
//   Aggressive sell signal: DeltaZ < -zScoreThreshold  AND ImbalRatio < -imbalRatioMin
//
//   Persistence filter: imbalance must hold for minConsecutiveBars in a row
//   before the confirmed signal fires (reduces noise on chop).

using System;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;

namespace NinjaTrader.NinjaScript.Indicators
{
    public class AggressiveOrderImbalance : Indicator
    {
        // --- configurable parameters ---
        private int    zScoreWindow       = 30;    // rolling EMA window for mean/std
        private double zScoreThreshold    = 1.5;   // |Z| above this = aggressive
        private double imbalRatioMin      = 0.20;  // minimum |ImbalRatio| to qualify
        private int    minConsecutiveBars = 2;      // bars imbalance must persist

        // --- internal EMA state ---
        private double emaDelta    = 0;
        private double emaVar      = 0;
        private double emaAlpha    = 0;
        private const double Epsilon = 1e-9;

        // --- persistence counter ---
        private int consecutiveBuyBars  = 0;
        private int consecutiveSellBars = 0;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Aggressive Order Imbalance (Indian Market Edition) — detects bars "
                            + "where aggressive market orders dominate one side of the book using "
                            + "tick-rule delta proxy and Z-score normalisation. Tuned for NSE/BSE/MCX "
                            + "where low spoofing makes large delta a genuine directional signal.";
                Name        = "AggressiveOrderImbalance";
                Calculate   = Calculate.OnBarClose;
                IsOverlay   = false;
                DisplayInDataBox = true;

                // Plot 0: Raw delta (buy - sell volume estimate)
                AddPlot(Brushes.DodgerBlue, "Delta");
                // Plot 1: Imbalance ratio  [-1, +1]
                AddPlot(Brushes.Goldenrod,  "ImbalanceRatio");
                // Plot 2: Z-score of delta
                AddPlot(Brushes.MediumPurple, "DeltaZScore");
                // Plot 3: Confirmed signal (+1 aggressive buy, -1 aggressive sell, 0 neutral)
                AddPlot(Brushes.Lime,       "AggressiveSignal");

                AddLine(Brushes.Red,    zScoreThreshold,  "UpperThreshold");
                AddLine(Brushes.Red,   -zScoreThreshold,  "LowerThreshold");
                AddLine(Brushes.Gray,   0,                "Zero");
            }
            else if (State == State.Configure)
            {
                // EMA smoothing factor for the rolling window
                emaAlpha = 2.0 / (zScoreWindow + 1.0);
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 2)
                return;

            double barRange = High[0] - Low[0] + TickSize;

            // ── 1. Tick-rule delta proxy ─────────────────────────────────────────
            double buyVolEst  = Volume[0] * (Close[0] - Low[0])  / barRange;
            double sellVolEst = Volume[0] * (High[0] - Close[0]) / barRange;
            double delta      = buyVolEst - sellVolEst;
            double imbalRatio = Volume[0] > 0 ? delta / Volume[0] : 0;

            // ── 2. Incremental EMA mean and variance ─────────────────────────────
            if (CurrentBar == 2)
            {
                emaDelta = delta;
                emaVar   = 0;
            }
            else
            {
                double prevMean = emaDelta;
                emaDelta = emaDelta + emaAlpha * (delta - emaDelta);
                double residual = delta - prevMean;
                emaVar   = emaVar   + emaAlpha * (residual * residual - emaVar);
            }

            double deltaStd    = Math.Sqrt(emaVar + Epsilon);
            double deltaZScore = (delta - emaDelta) / deltaStd;

            // ── 3. Persistence filter ────────────────────────────────────────────
            bool aggressiveBuy  = deltaZScore >  zScoreThreshold && imbalRatio >  imbalRatioMin;
            bool aggressiveSell = deltaZScore < -zScoreThreshold && imbalRatio < -imbalRatioMin;

            if (aggressiveBuy)
            {
                consecutiveBuyBars++;
                consecutiveSellBars = 0;
            }
            else if (aggressiveSell)
            {
                consecutiveSellBars++;
                consecutiveBuyBars = 0;
            }
            else
            {
                consecutiveBuyBars  = 0;
                consecutiveSellBars = 0;
            }

            double confirmedSignal = 0;
            if (consecutiveBuyBars  >= minConsecutiveBars) confirmedSignal =  1;
            if (consecutiveSellBars >= minConsecutiveBars) confirmedSignal = -1;

            Values[0][0] = delta;
            Values[1][0] = imbalRatio;
            Values[2][0] = deltaZScore;
            Values[3][0] = confirmedSignal;
        }

        // ── Public accessors ─────────────────────────────────────────────────────
        public double Delta            => Values[0][0];
        public double ImbalanceRatio   => Values[1][0];
        public double DeltaZScore      => Values[2][0];
        public double AggressiveSignal => Values[3][0];

        public override string DisplayName => "Aggressive Order Imbalance";
    }
}
