// Absorption Detector
// Identifies price levels where large opposing volume is being absorbed without
// significant price displacement — a hallmark of institutional accumulation or
// distribution. Particularly effective in Indian equity/F&O markets (NSE/BSE/MCX)
// where spoofing is limited, making absorbed volume a reliable signal.
//
// Core math
// ---------
//   BarEfficiency   = |Close - Open| / (High - Low + TickSize)
//                     → 0 = pure doji / full inside-range absorption
//                     → 1 = clean directional bar (no absorption)
//
//   VolumeIntensity = Volume[0] / SMA(Volume, lookbackBars)
//                     → > 1 means above-average volume
//
//   AbsorptionScore = VolumeIntensity * (1 - BarEfficiency)
//                     → high score = lots of volume, very little net price movement
//
//   Direction
//     SellAbsorption (Bullish): bearish candle (Close < Open) with high score
//                                → sellers hitting bids but price won't fall
//     BuyAbsorption  (Bearish): bullish candle (Close > Open) with high score
//                                → buyers lifting offers but price won't rise
//
//   Confirmation filter: the bar must also close in the upper/lower 30% of its
//   own range (for sell/buy absorption respectively), confirming that the
//   opposing side defended the extreme.

using System;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;

namespace NinjaTrader.NinjaScript.Indicators
{
    public class AbsorptionDetector : Indicator
    {
        // --- configurable parameters ---
        private int lookbackBars = 20;
        private double absorptionThreshold = 1.8;   // score must exceed this
        private double confirmationZone = 0.30;      // close must be in top/bottom 30% of range

        // --- internal state ---
        private double avgVolume = 0;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Absorption Detector - Identifies price levels where large volume "
                            + "is being absorbed without significant price movement. Tuned for "
                            + "Indian equity and F&O markets (NSE/BSE/MCX).";
                Name        = "AbsorptionDetector";
                Calculate   = Calculate.OnBarClose;
                IsOverlay   = false;
                DisplayInDataBox = true;

                // Plot 0: AbsorptionScore — magnitude of absorption
                AddPlot(Brushes.DodgerBlue,  "AbsorptionScore");
                // Plot 1: Signal — +1 sell-absorbed (bullish), -1 buy-absorbed (bearish), 0 none
                AddPlot(Brushes.Orange,      "Signal");
                // Plot 2: AvgVolume — rolling average for reference
                AddPlot(Brushes.Gray,        "AvgVolume");
                AddLine(Brushes.Red,    absorptionThreshold, "Threshold");
                AddLine(Brushes.Gray,   0,                   "Zero");
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < lookbackBars)
                return;

            // ── 1. Rolling average volume ───────────────────────────────────────
            double sumVol = 0;
            for (int i = 0; i < lookbackBars; i++)
                sumVol += Volume[i];
            avgVolume = sumVol / lookbackBars;

            // ── 2. Bar efficiency ────────────────────────────────────────────────
            double barRange      = High[0] - Low[0] + TickSize;
            double netMove       = Math.Abs(Close[0] - Open[0]);
            double barEfficiency = netMove / barRange;            // 0 … 1

            // ── 3. Volume intensity relative to recent average ───────────────────
            double volumeIntensity = avgVolume > 0 ? Volume[0] / avgVolume : 1.0;

            // ── 4. Absorption score ──────────────────────────────────────────────
            //   High score = above-average volume + minimal price displacement
            double absorptionScore = volumeIntensity * (1.0 - barEfficiency);

            // ── 5. Close-position within bar range (0 = at low, 1 = at high) ────
            double closePosition = barRange > TickSize
                ? (Close[0] - Low[0]) / barRange
                : 0.5;

            // ── 6. Direction logic ───────────────────────────────────────────────
            double signal = 0;
            if (absorptionScore >= absorptionThreshold)
            {
                bool isBearishCandle = Close[0] < Open[0];
                bool isBullishCandle = Close[0] > Open[0];

                // Sell absorption (bullish setup):
                //   bearish candle + close recovers into upper confirmationZone
                if (isBearishCandle && closePosition >= (1.0 - confirmationZone))
                    signal = 1;

                // Buy absorption (bearish setup):
                //   bullish candle + close gives back into lower confirmationZone
                else if (isBullishCandle && closePosition <= confirmationZone)
                    signal = -1;
            }

            Values[0][0] = absorptionScore;
            Values[1][0] = signal;
            Values[2][0] = avgVolume;
        }

        // ── Public accessors (for use by strategy ────────────────────────────────
        public double AbsorptionScore  => Values[0][0];
        public double Signal            => Values[1][0];

        public override string DisplayName => "Absorption Detector";
    }
}
