using System;
using System.Collections.Generic;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;

namespace NinjaTrader.NinjaScript.Indicators
{
    public struct OrderFlowSnapshot : ICloneable {
        public double Timestamp { get; set; }
        public double Close { get; set; }
        public double Volume { get; set; }
        public double Delta { get; set; }

        public object Clone() {
            return new OrderFlowSnapshot {
                Timestamp = this.Timestamp,
                Close = this.Close,
                Volume = this.Volume,
                Delta = this.Delta
            };
        }

        public static implicit operator OrderFlowSnapshot(double val) {
            return new OrderFlowSnapshot { Close = val };
        }
    }

    public class SnaphotBuffer {
        private readonly OrderFlowSnapshot[] buf;
        private int wrIdx;
        private readonly int sz;
        private int cnt;

        public SnaphotBuffer(int size) {
            sz = size;
            buf = new OrderFlowSnapshot[size];
            wrIdx = 0;
            cnt = 0;
        }

        public void Add(OrderFlowSnapshot snap) {
            buf[wrIdx] = snap;
            wrIdx = (wrIdx + 1) % sz;
            if (cnt < sz)
                cnt++;
        }

        public OrderFlowSnapshot[] GetAll() {
            var result = new OrderFlowSnapshot[cnt];
            for (int i = 0; i < cnt; i++) {
                int idx = (wrIdx + i) % sz;
                result[i] = (OrderFlowSnapshot)buf[idx].Clone();
            }
            return result;
        }

        public OrderFlowSnapshot GetLatest() {
            if (cnt == 0)
                return default;
            int idx = wrIdx > 0 ? wrIdx - 1 : sz - 1;
            return (OrderFlowSnapshot)buf[idx].Clone();
        }

        public void Clear() {
            Array.Clear(buf, 0, sz);
            wrIdx = 0;
            cnt = 0;
        }

        public int Count => cnt;
    }

    public class OrderFlowSnapshot_Immutable {
        private readonly double timestamp;
        private readonly double close;
        private readonly double volume;
        private readonly double delta;

        public double Timestamp => timestamp;
        public double Close => close;
        public double Volume => volume;
        public double Delta => delta;

        public OrderFlowSnapshot_Immutable(double ts, double c, double v, double d) {
            timestamp = ts;
            close = c;
            volume = v;
            delta = d;
        }

        public OrderFlowSnapshot_Immutable WithClose(double newClose) {
            return new OrderFlowSnapshot_Immutable(timestamp, newClose, volume, delta);
        }

        public OrderFlowSnapshot_Immutable WithDelta(double newDelta) {
            return new OrderFlowSnapshot_Immutable(timestamp, close, volume, newDelta);
        }
    }

    public class StateManagementIndicator : Indicator {
        private SnaphotBuffer snapshots;
        private readonly int bufSize = 100;
        private int barIdx = 0;

        protected override void OnStateChange() {
            if (State == State.SetDefaults) {
                Description = "State Management - Copy semantics and immutable patterns";
                Name = "StateManagementIndicator";
                Calculate = Calculate.OnBarClose;
                IsOverlay = false;
                DisplayInDataBox = true;

                AddPlot(Brushes.Blue, "StateValue");
                AddPlot(Brushes.Green, "StateChange");
            }
            else if (State == State.Configure) {
                snapshots = new SnaphotBuffer(bufSize);
            }
        }

        protected override void OnBarUpdate() {
            double delta = Close[0] - (CurrentBar > 0 ? Close[1] : Close[0]);

            var snap = new OrderFlowSnapshot {
                Timestamp = barIdx++,
                Close = Close[0],
                Volume = Volume[0],
                Delta = delta
            };

            snapshots.Add(snap);

            if (snapshots.Count > 10) {
                var latest = snapshots.GetLatest();
                var all = snapshots.GetAll();

                double avgDelta = 0;
                for (int i = 0; i < all.Length; i++)
                    avgDelta += all[i].Delta;
                avgDelta /= all.Length;

                Values[0][0] = latest.Delta;
                Values[1][0] = avgDelta;
            }
        }

        public override string DisplayName => "State Management Indicator";
    }

    public class ConstCorrectionIndicator : Indicator {
        private readonly int period = 20;
        private readonly double threshold = 0.65;
        private readonly string name = "ConstCorrectionIndicator";

        protected override void OnStateChange() {
            if (State == State.SetDefaults) {
                Description = "Const Correctness - Readonly fields, immutable structs";
                Name = name;
                Calculate = Calculate.OnBarClose;
                IsOverlay = false;
                DisplayInDataBox = true;

                AddPlot(Brushes.Purple, "Quality");
            }
        }

        protected override void OnBarUpdate() {
            if (CurrentBar < period)
                return;

            double quality = CalculateQuality();
            Values[0][0] = quality;
        }

        private double CalculateQuality() {
            double upDays = 0;
            double totalRange = 0;

            for (int i = 0; i < period; i++) {
                if (Close[i] > Open[i])
                    upDays++;
                totalRange += (High[i] - Low[i]);
            }

            double upRatio = upDays / period;
            double avgRange = totalRange / period;
            double quality = avgRange > 0 ? upRatio * (avgRange / Close[0]) : 0;

            return Math.Min(quality, 1.0);
        }

        public override string DisplayName => "Const Correction Indicator";
    }
}
