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

## Installation Instructions

1. **Copy Files to NinjaTrader:**
   - Navigate to: `Documents\NinjaTrader 8\bin\Custom\Indicators`
   - Copy all `.cs` files from this collection

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

---

**Disclaimer:** These tools are for educational and analysis purposes. Always practice proper risk management and test thoroughly before live trading.
