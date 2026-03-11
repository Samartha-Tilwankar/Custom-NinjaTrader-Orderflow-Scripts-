// Indian Market Absorption + Aggressive Imbalance Strategy
// =========================================================
// Tailored for NSE Nifty 50 / BankNifty / MCX Crude futures on 3-minute bars.
// Works on any Indian equity/F&O instrument loaded into NinjaTrader via Zerodha
// Kite Connect, Upstox, or any NT8-compatible data feed.
//
// Strategy logic overview
// -----------------------
// The market in India tends to respect strong absorption events because large
// participants (FIIs, domestic mutual funds, proprietary desks) are the primary
// movers of price at key intraday levels. When their orders are absorbed, it
// signals a near-term trap and provides a high-probability reversal entry.
//
// Entry conditions (LONG):
//   1. Price is near or below the session VWAP (bearish context for a reversal up)
//   2. AbsorptionDetector fires a SellAbsorption signal (+1)
//      → bearish candle absorbed in upper zone → buyers stepping in
//   3. AggressiveOrderImbalance confirms AggressiveSignal = +1
//      → delta Z-score > threshold on the same or next bar
//   4. ATR-based volatility filter: bar range must not be a runaway bar
//      (avoids chasing after the move has already happened)
//
// Entry conditions (SHORT):
//   1. Price is at or above session VWAP (bullish context for a reversal down)
//   2. AbsorptionDetector fires a BuyAbsorption signal (-1)
//      → bullish candle absorbed in lower zone → sellers stepping in
//   3. AggressiveOrderImbalance confirms AggressiveSignal = -1
//   4. ATR filter passes
//
// Exit / risk management:
//   Stop  : entry ± (ATR multiplier × ATR) — adapts to current volatility
//   Target: 2× stop distance (fixed RR 1:2, configurable)
//   Time  : hard exit at 15:20 IST (10 mins before NSE close) to avoid illiquid
//           close-of-session spreads
//
// Mathematical foundations
// ------------------------
//   VWAP  = Σ(Price_i × Volume_i) / Σ(Volume_i)   [reset each session]
//   ATR_n = EMA(TrueRange, n)
//   TrueRange = max(High - Low, |High - Close_prev|, |Low - Close_prev|)
//   Stop distance = atrMultiplier × ATR
//   Target        = rrRatio × Stop distance
//
// Practical tuning notes for Indian markets
// ------------------------------------------
//   • Nifty 50 futures (lot 75): use 3-min bars, absorptionThreshold 1.8,
//     zScoreThreshold 1.5, atrMultiplier 1.2
//   • BankNifty futures (lot 15): use 3-min bars, atrMultiplier 1.5 (more volatile)
//   • MCX Crude Oil (lot 100 bbl): use 5-min bars, atrMultiplier 1.0
//   • Adjust minConsecutiveBars to 2 on quiet sessions, 1 on trending days

using System;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;

namespace NinjaTrader.NinjaScript.Strategies
{
    public class IndianMarketAbsorptionStrategy : Strategy
    {
        // ── Strategy parameters ───────────────────────────────────────────────
        private int    atrPeriod         = 14;
        private double atrMultiplier     = 1.2;   // stop = atrMultiplier × ATR
        private double rrRatio           = 2.0;   // reward:risk
        private double maxBarRangeATR    = 2.5;   // skip entry if bar > 2.5× ATR (runaway)
        private int    signalLookback    = 3;      // bars to look back for confirming signal
        private TimeSpan hardExitTime    = new TimeSpan(15, 20, 0); // 15:20 IST hard flat

        // ── Indicator handles ─────────────────────────────────────────────────
        private AbsorptionDetector       absorption;
        private AggressiveOrderImbalance imbalance;
        private ATR                      atrIndicator;

        // ── Session VWAP state ────────────────────────────────────────────────
        private double vwapNumerator   = 0;
        private double vwapDenominator = 0;
        private double vwap            = 0;
        private DateTime lastSessionDate = DateTime.MinValue;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description  = "Indian Market Absorption + Aggressive Imbalance Strategy — "
                             + "uses AbsorptionDetector and AggressiveOrderImbalance to trade "
                             + "high-probability reversals on NSE/BSE/MCX instruments.";
                Name         = "IndianMarketAbsorptionStrategy";
                Calculate    = Calculate.OnBarClose;
                EntriesPerDirection = 1;
                EntryHandling       = EntryHandling.AllEntries;
                IsExitOnSessionCloseStrategy = true;
            }
            else if (State == State.Configure)
            {
                absorption   = AbsorptionDetector();
                imbalance    = AggressiveOrderImbalance();
                atrIndicator = ATR(atrPeriod);
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < Math.Max(atrPeriod, signalLookback) + 1)
                return;

            // ── Hard exit before session close ────────────────────────────────
            if (Time[0].TimeOfDay >= hardExitTime)
            {
                if (Position.MarketPosition == MarketPosition.Long)
                    ExitLong("HardExit", "Long");
                else if (Position.MarketPosition == MarketPosition.Short)
                    ExitShort("HardExit", "Short");
                return;
            }

            // ── Session VWAP reset ────────────────────────────────────────────
            if (Time[0].Date != lastSessionDate)
            {
                vwapNumerator   = 0;
                vwapDenominator = 0;
                lastSessionDate = Time[0].Date;
            }

            // Typical price = (High + Low + Close) / 3
            double typicalPrice  = (High[0] + Low[0] + Close[0]) / 3.0;
            vwapNumerator   += typicalPrice * Volume[0];
            vwapDenominator += Volume[0];
            vwap = vwapDenominator > 0 ? vwapNumerator / vwapDenominator : Close[0];

            // ── ATR ───────────────────────────────────────────────────────────
            double atr         = atrIndicator[0];
            double stopDist    = atrMultiplier * atr;
            double targetDist  = rrRatio * stopDist;
            double barRange    = High[0] - Low[0];

            // Skip runaway bars (already moved, not an absorption entry)
            if (barRange > maxBarRangeATR * atr)
                return;

            // ── Read indicator signals ────────────────────────────────────────
            // AggressiveSignal is the current confirmed signal (plot index 3)
            double aggrSignal  = imbalance.Values[3][0];

            // Look back signalLookback bars for an absorption trigger.
            // absorption.Values[1] is the Signal plot (index 1).
            bool recentSellAbsorption = false;
            bool recentBuyAbsorption  = false;
            for (int i = 0; i < signalLookback; i++)
            {
                double sig = absorption.Values[1][i];
                if (sig > 0.5)  recentSellAbsorption = true;
                if (sig < -0.5) recentBuyAbsorption  = true;
            }

            // ── LONG entry ────────────────────────────────────────────────────
            //   Sell absorption (bullish) + aggressive buy imbalance confirms
            //   + price at or below VWAP (buy the dip on failed sell attempt)
            if (Position.MarketPosition == MarketPosition.Flat
                && recentSellAbsorption
                && aggrSignal >= 1
                && Close[0] <= vwap)
            {
                double stopPrice   = Low[0]  - stopDist;
                double targetPrice = Close[0] + targetDist;
                EnterLong("AbsorptionLong");
                SetStopLoss("AbsorptionLong",  CalculationMode.Price, stopPrice,   false);
                SetProfitTarget("AbsorptionLong", CalculationMode.Price, targetPrice);
            }

            // ── SHORT entry ───────────────────────────────────────────────────
            //   Buy absorption (bearish) + aggressive sell imbalance confirms
            //   + price at or above VWAP (short the rally on failed buy attempt)
            else if (Position.MarketPosition == MarketPosition.Flat
                     && recentBuyAbsorption
                     && aggrSignal <= -1
                     && Close[0] >= vwap)
            {
                double stopPrice   = High[0] + stopDist;
                double targetPrice = Close[0] - targetDist;
                EnterShort("AbsorptionShort");
                SetStopLoss("AbsorptionShort",    CalculationMode.Price, stopPrice,   false);
                SetProfitTarget("AbsorptionShort", CalculationMode.Price, targetPrice);
            }
        }

        public override string DisplayName => "Indian Market Absorption Strategy";
    }
}
