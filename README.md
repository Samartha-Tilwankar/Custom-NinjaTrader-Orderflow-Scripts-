# NinjaTrader Order Flow Tools Collection

A comprehensive suite of 10 professional-grade order flow analysis tools for NinjaTrader.

## Tools Overview

### 1. Volume Profile Analyzer
**File:** `1_VolumeProfileAnalyzer.cs`

Analyzes cumulative volume at different price levels to identify significant support/resistance zones.

**Key Features:**
- Point of Control (POC) - price level with highest volume
- Value Area calculation (70% of volume concentration)
- Identifies key support and resistance levels
- 50-bar lookback period (configurable)

**Use Cases:**
- Identify high-volume nodes for support/resistance
- Confirm breakout levels
- Find mean reversion targets

---

### 2. Cumulative Delta Analyzer
**File:** `2_CumulativeDeltaAnalyzer.cs`

Tracks the cumulative difference between up volume and down volume over time.

**Key Features:**
- Delta calculation (up volume vs down volume)
- Session-based delta tracking
- Identifies directional sweep
- Resets at price gaps

**Use Cases:**
- Detect buying/selling dominance
- Confirm trend strength
- Identify potential exhaustion moves

---

### 3. Order Imbalance Detector
**File:** `3_OrderImbalanceDetector.cs`

Identifies significant bid/ask imbalances that may precede directional moves.

**Key Features:**
- 20-bar lookback analysis
- Imbalance ratio calculation
- 60% threshold detection
- Signal generation for extremes

**Use Cases:**
- Trade imbalanced markets
- Detect one-sided order flow
- Identify reversal setups

---

### 4. Buy/Sell Pressure Indicator
**File:** `4_BuySellPressureIndicator.cs`

Measures the relative intensity of buying versus selling activity.

**Key Features:**
- Buying pressure: volume with high close relative strength
- Selling pressure: volume with low close relative strength
- Net pressure calculation
- 10-bar period analysis

**Use Cases:**
- Quantify market sentiment
- Validate trend strength
- Identify divergences with price

---

### 5. VWAP Delta Divergence
**File:** `5_VWAPDeltaDivergence.cs`

Detects divergences between price direction and cumulative delta.

**Key Features:**
- Volume-weighted average price (VWAP) calculation
- Delta exponential moving average
- Divergence identification
- Alert conditions for reversal setup

**Use Cases:**
- Identify hidden strength/weakness
- Confirm breakouts
- Early reversal signals

---

### 6. Volume Rate of Change
**File:** `6_VolumeRateOfChange.cs`

Measures velocity of volume changes and volume acceleration.

**Key Features:**
- Rate of change (ROC) of volume over 10 bars
- Volume acceleration metric
- Percentage-based calculation
- Identifies volume surge/decline

**Use Cases:**
- Monitor volume intensity changes
- Validate trend moves with volume spikes
- Identify climactic moves

---

### 7. Market Microstructure Analyzer
**File:** `7_MarketMicrostructureAnalyzer.cs`

Analyzes price movement efficiency relative to volume and identifies reversal patterns.

**Key Features:**
- Efficiency metric (price range vs volume)
- Volatility calculation
- Reversal signal detection
- 50-bar period analysis

**Use Cases:**
- Quantify market efficiency
- Detect inefficient moves
- Identify mean reversion opportunities

---

### 8. Liquidity Pool Detector
**File:** `8_LiquidityPoolDetector.cs`

Identifies areas of high order concentration and liquidity clustering.

**Key Features:**
- Price level clustering analysis
- Liquidity zone identification
- Pool strength measurement
- 100-bar historical analysis

**Use Cases:**
- Find institutional buying/selling zones
- Predict support/resistance breaks
- Identify stop-loss clustering areas

---

### 9. Order Flow Momentum
**File:** `9_OrderFlowMomentum.cs`

Calculates momentum based purely on directional order flow.

**Key Features:**
- Delta-to-volume ratio momentum
- Fast and slow moving averages
- 14-period analysis window
- Percentage-based scaling

**Use Cases:**
- Momentum confirmation
- Trend strength validation
- Overbought/oversold detection

---

### 10. Smart Money Flow Index
**File:** `10_SmartMoneyFlowIndex.cs`

Identifies institutional order flow patterns based on volume-weighted price moves.

**Key Features:**
- Institutional money flow detection
- Buying vs selling flow separation
- 20-bar period analysis
- 0-100 scale for easy interpretation

**Use Cases:**
- Detect smart money accumulation
- Identify institutional exits
- Confirm trend reversals

---

### 11. Absorption Detector
**File:** `11_AbsorptionDetector.cs`

Identifies price bars where large opposing volume is being absorbed without significant price displacement — a hallmark of institutional accumulation or distribution. Particularly effective in Indian equity and F&O markets (NSE/BSE/MCX) where spoofing is limited, making absorbed volume a reliable signal.

**Core math:**
- `BarEfficiency = |Close - Open| / (High - Low + TickSize)` — 0 means full absorption, 1 means clean directional bar
- `VolumeIntensity = Volume / SMA(Volume, lookbackBars)` — normalises for time-of-day variation
- `AbsorptionScore = VolumeIntensity × (1 − BarEfficiency)` — high score = lots of volume, minimal net movement
- Signal direction uses close position within bar range as a confirmation filter (must close in opposing 30% of range)

**Signals:**
- `+1` SellAbsorption (bullish): bearish candle, high score, close recovers into upper 30% of range
- `-1` BuyAbsorption (bearish): bullish candle, high score, close gives back into lower 30% of range
- `0` No absorption

**Key Parameters:**
- `lookbackBars = 20` — rolling volume average window
- `absorptionThreshold = 1.8` — minimum score to qualify
- `confirmationZone = 0.30` — close must be in top/bottom 30% of range

**Use Cases:**
- Detect institutional accumulation/distribution at key levels
- Find high-probability reversal entries in trending markets
- Confirm support/resistance tests before entering counter-trend trades

---

### 12. Aggressive Order Imbalance (Indian Market Edition)
**File:** `12_AggressiveOrderImbalance.cs`

Detects bars where aggressive market orders are hitting one side of the book with statistically unusual force. Uses a tick-rule delta proxy (no tick-by-tick data required) and Z-score normalisation over a rolling EMA window. Designed for NSE/BSE/MCX where low spoofing makes large delta a genuinely reliable directional signal.

**Core math:**
- `BuyVolEst  = Volume × (Close − Low)  / (High − Low + TickSize)`
- `SellVolEst = Volume × (High − Close) / (High − Low + TickSize)`
- `Delta = BuyVolEst − SellVolEst`
- `ImbalanceRatio = Delta / Volume` — ranges from −1 (pure selling) to +1 (pure buying)
- `DeltaZ = (Delta − EMA(Delta, N)) / sqrt(EMA(residual², N))` — rolling Z-score

**Signals:**
- `+1` Aggressive buy: DeltaZ > threshold AND ImbalanceRatio > minimum, sustained for N bars
- `-1` Aggressive sell: DeltaZ < −threshold AND ImbalanceRatio < −minimum, sustained for N bars
- `0` No confirmed imbalance

**Key Parameters:**
- `zScoreWindow = 30` — rolling EMA window for mean/variance
- `zScoreThreshold = 1.5` — |Z| required to classify as aggressive
- `imbalRatioMin = 0.20` — minimum absolute imbalance ratio
- `minConsecutiveBars = 2` — persistence filter (reduces noise)

**Use Cases:**
- Detect institutional aggressive sweeping of a price level
- Confirm breakouts with genuine order flow conviction
- Filter out random price noise from true directional moves

---

### 13. Indian Market Absorption Strategy
**File:** `13_IndianMarketAbsorptionStrategy.cs`

A complete automated strategy combining AbsorptionDetector and AggressiveOrderImbalance for high-probability reversal trades on NSE Nifty 50 / BankNifty / MCX futures. Uses session VWAP as directional bias and ATR for adaptive risk management.

**Entry logic:**
- **Long**: Sell absorption signal (+1) within last 3 bars + aggressive buy imbalance confirms + price ≤ VWAP
- **Short**: Buy absorption signal (−1) within last 3 bars + aggressive sell imbalance confirms + price ≥ VWAP

**Risk management:**
- Stop  : `entry ± (atrMultiplier × ATR)` — adapts to current volatility
- Target: `rrRatio × stop distance` (default 1:2 risk-reward)
- Hard exit at 15:20 IST to avoid illiquid close-of-session spreads

**Key Parameters:**
- `atrPeriod = 14`, `atrMultiplier = 1.2`, `rrRatio = 2.0`
- `maxBarRangeATR = 2.5` — skips runaway bars
- `hardExitTime = 15:20` — mandatory session-end flat

**Recommended settings by instrument:**
- Nifty 50 (lot 75): 3-min bars, `atrMultiplier 1.2`
- BankNifty (lot 15): 3-min bars, `atrMultiplier 1.5` (higher volatility)
- MCX Crude Oil (lot 100 bbl): 5-min bars, `atrMultiplier 1.0`

**Use Cases:**
- Intraday reversal trading on Indian F&O instruments
- Institutional level identification for bracket orders
- Automated execution of absorption-based setups

---

## Installation Instructions

1. **Copy Files to NinjaTrader:**
   - Indicators (`1_` – `12_`): `Documents\NinjaTrader 8\bin\Custom\Indicators`
   - Strategies (`13_`): `Documents\NinjaTrader 8\bin\Custom\Strategies`
   - Copy all relevant `.cs` files

2. **Compile Indicators:**
   - Open NinjaTrader 8
   - Go to Tools → Compiling Scripts
   - Select "Compile All Scripts"
   - Ensure no errors appear

3. **Add to Charts:**
   - Insert → New Indicator
   - Find indicator by name (e.g., "VolumeProfileAnalyzer")
   - Configure parameters as needed

## Common Parameters

Most indicators use these configurable parameters:

- **lookbackBars/lookbackPeriod**: Historical bars to analyze (default: 10-100)
- **imbalanceThreshold**: Threshold for extreme conditions (default: 0.6 = 60%)
- **rocPeriod**: Rate of change calculation period (default: 10)

## Usage Tips

### Effective Trading Combinations:
1. **Trend Confirmation**: Order Flow Momentum + Volume Rate of Change + Buy/Sell Pressure
2. **Reversal Setup**: Smart Money Flow Index + Order Imbalance Detector + VWAP Delta Divergence
3. **Entry Zone**: Liquidity Pool Detector + Volume Profile Analyzer + Market Microstructure Analyzer
4. **Position Sizing**: Cumulative Delta Analyzer + Volume Profile Analyzer
5. **Indian Market Reversals**: Absorption Detector + Aggressive Order Imbalance (use `13_IndianMarketAbsorptionStrategy.cs` for automated execution)

### Best Practices:
- Use multiple indicators together for confirmation
- Adjust lookback periods based on timeframe (lower for 1-min, higher for daily)
- Combine with price action analysis for best results
- Backtest any strategy before live trading

## System Requirements

- NinjaTrader 8.1 or higher
- Windows 7 or higher
- 2GB RAM minimum
- .NET Framework 4.8+

## Notes

- All indicators are real-time compatible
- No external data feeds required
- Suitable for equities, futures, and forex
- All code is unmanaged (no DLL dependencies)

## Version Information

- Created: March 2026
- NinjaTrader Version: 8.1+
- Language: C#
- Indian Market instruments: NSE Nifty 50, BankNifty, MCX Crude

---

**Disclaimer:** These tools are for educational and analysis purposes. Always practice proper risk management and test thoroughly before live trading.
