using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;

namespace NinjaTrader.NinjaScript.Indicators
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CacheFriendlyOHLC {
        public double Open;
        public double High;
        public double Low;
        public double Close;
        public long Volume;
        private long padding;

        public CacheFriendlyOHLC(double o, double h, double l, double c, long v) {
            Open = o;
            High = h;
            Low = l;
            Close = c;
            Volume = v;
            padding = 0;
        }
    }

    public class CacheOptimizedBuffer {
        private CacheFriendlyOHLC[] data;
        private int idx;
        private readonly int sz;
        private readonly object lk = new object();

        public CacheOptimizedBuffer(int size) {
            sz = size;
            data = new CacheFriendlyOHLC[size];
            idx = 0;
        }

        public void Append(double o, double h, double l, double c, long v) {
            lock (lk) {
                data[idx] = new CacheFriendlyOHLC(o, h, l, c, v);
                idx = (idx + 1) % sz;
            }
        }

        public CacheFriendlyOHLC[] GetBuffer() {
            lock (lk) {
                var res = new CacheFriendlyOHLC[sz];
                Array.Copy(data, res, sz);
                return res;
            }
        }

        public double CalcAverageRange() {
            lock (lk) {
                double sum = 0;
                for (int i = 0; i < sz; i++)
                    sum += (data[i].High - data[i].Low);
                return sum / sz;
            }
        }

        public long GetTotalVolume() {
            lock (lk) {
                long sum = 0;
                for (int i = 0; i < sz; i++)
                    sum += data[i].Volume;
                return sum;
            }
        }
    }

    public class CacheLineIndicator : Indicator {
        private CacheOptimizedBuffer cache;
        private readonly int cacheSize = 64;
        private double[] rollingMean;
        private int mIdx;

        protected override void OnStateChange() {
            if (State == State.SetDefaults) {
                Description = "Cache Line Optimization - Struct packing and cache optimization";
                Name = "CacheLineIndicator";
                Calculate = Calculate.OnBarClose;
                IsOverlay = false;
                DisplayInDataBox = true;

                AddPlot(Brushes.Blue, "Range");
                AddPlot(Brushes.Green, "RangeMA");
                AddPlot(Brushes.Red, "Volatility");
            }
            else if (State == State.Configure) {
                cache = new CacheOptimizedBuffer(cacheSize);
                rollingMean = new double[cacheSize];
                mIdx = 0;
            }
        }

        protected override void OnBarUpdate() {
            cache.Append(Open[0], High[0], Low[0], Close[0], Volume[0]);

            double range = High[0] - Low[0];
            rollingMean[mIdx] = range;
            mIdx = (mIdx + 1) % cacheSize;

            double avgRange = cache.CalcAverageRange();
            long totalVol = cache.GetTotalVolume();

            double ma = 0;
            for (int i = 0; i < cacheSize; i++)
                ma += rollingMean[i];
            ma /= cacheSize;

            Values[0][0] = range;
            Values[1][0] = ma;
            Values[2][0] = totalVol > 0 ? (range / ma) : 0;
        }

        public override string DisplayName => "Cache Line Indicator";
    }

    public class InlineOptimizedCalculator : Indicator {
        private double vol1, vol2, vol3, vol4, vol5;
        private double p1, p2, p3, p4, p5;
        private bool ready = false;

        protected override void OnStateChange() {
            if (State == State.SetDefaults) {
                Description = "Inline Optimized - Unrolled loops and register optimization";
                Name = "InlineOptimizedCalculator";
                Calculate = Calculate.OnBarClose;
                IsOverlay = false;
                DisplayInDataBox = true;

                AddPlot(Brushes.Cyan, "Score");
            }
        }

        protected override void OnBarUpdate() {
            if (CurrentBar < 5) {
                ready = false;
                return;
            }

            vol1 = Volumes[0];
            vol2 = Volumes[1];
            vol3 = Volumes[2];
            vol4 = Volumes[3];
            vol5 = Volumes[4];

            p1 = Closes[0];
            p2 = Closes[1];
            p3 = Closes[2];
            p4 = Closes[3];
            p5 = Closes[4];

            ready = true;

            double comp = (vol1 > vol2 ? 0.2 : 0) +
                         (vol2 > vol3 ? 0.2 : 0) +
                         (vol3 > vol4 ? 0.2 : 0) +
                         (vol4 > vol5 ? 0.2 : 0) +
                         (vol5 > vol1 ? 0.2 : 0);

            Values[0][0] = comp;
        }

        public override string DisplayName => "Inline Optimized Calculator";
    }

    public class SmartPointerPatternIndicator : Indicator {
        private class OrderFlowHandle : IDisposable {
            public double Data { get; set; }
            public int ReferenceCount { get; private set; }

            public OrderFlowHandle() {
                ReferenceCount = 1;
            }

            public void AddRef() => ReferenceCount++;
            public void Release() => ReferenceCount--;

            public void Dispose() {
                Data = 0;
                ReferenceCount = 0;
            }
        }

        private OrderFlowHandle handle;

        protected override void OnStateChange() {
            if (State == State.SetDefaults) {
                Description = "Smart Pointer Pattern - Reference counting simulation";
                Name = "SmartPointerPatternIndicator";
                Calculate = Calculate.OnBarClose;
                IsOverlay = false;
                DisplayInDataBox = true;

                AddPlot(Brushes.Yellow, "RefCount");
            }
            else if (State == State.Configure) {
                handle = new OrderFlowHandle();
            }
        }

        protected override void OnBarUpdate() {
            if (handle == null)
                return;

            handle.Data = Close[0];

            if (Volume[0] > Volume[1])
                handle.AddRef();
            else if (handle.ReferenceCount > 1)
                handle.Release();

            Values[0][0] = handle.ReferenceCount;
        }

        public override string DisplayName => "Smart Pointer Pattern Indicator";

        ~SmartPointerPatternIndicator() {
            handle?.Dispose();
        }
    }
}
