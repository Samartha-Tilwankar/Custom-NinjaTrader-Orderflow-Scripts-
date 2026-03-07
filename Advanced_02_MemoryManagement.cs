using System;
using System.Collections.Generic;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;

namespace NinjaTrader.NinjaScript.Indicators
{
    public interface IOrderFlowCalculator {
        void Calculate(double[] prices, double[] volumes);
        double GetResult();
    }

    public abstract class MemoryEfficientBuffer {
        protected double[] buffer;
        protected int writeIdx;
        protected readonly int capacity;
        protected readonly object syncLock = new object();

        public MemoryEfficientBuffer(int size) {
            capacity = size;
            buffer = new double[size];
            writeIdx = 0;
        }

        public virtual void Append(double value) {
            lock (syncLock) {
                buffer[writeIdx] = value;
                writeIdx = (writeIdx + 1) % capacity;
            }
        }

        public virtual double[] ToArray() {
            lock (syncLock) {
                double[] result = new double[capacity];
                Array.Copy(buffer, result, capacity);
                return result;
            }
        }

        public virtual void Clear() {
            lock (syncLock) {
                Array.Clear(buffer, 0, capacity);
                writeIdx = 0;
            }
        }
    }

    public class RingBuffer : MemoryEfficientBuffer {
        private int elemCount;

        public RingBuffer(int size) : base(size) {
            elemCount = 0;
        }

        public override void Append(double value) {
            lock (syncLock) {
                buffer[writeIdx] = value;
                writeIdx = (writeIdx + 1) % capacity;
                if (elemCount < capacity)
                    elemCount++;
            }
        }

        public double GetSum() {
            lock (syncLock) {
                double sum = 0;
                for (int i = 0; i < elemCount; i++)
                    sum += buffer[i];
                return sum;
            }
        }

        public double GetAverage() {
            lock (syncLock) {
                return elemCount > 0 ? GetSum() / elemCount : 0;
            }
        }

        public int Count {
            get { lock (syncLock) { return elemCount; } }
        }
    }

    public class SmartDeltaCalculator : Indicator, IOrderFlowCalculator {
        private RingBuffer deltaBuffer;
        private RingBuffer volumeBuffer;
        private readonly int lookback = 20;
        private double sessionPnL = 0;

        protected override void OnStateChange() {
            if (State == State.SetDefaults) {
                Description = "Smart Delta Calculator - RAII pattern with proper resource management";
                Name = "SmartDeltaCalculator";
                Calculate = Calculate.OnBarClose;
                IsOverlay = false;
                DisplayInDataBox = true;

                AddPlot(Brushes.Green, "Delta");
                AddPlot(Brushes.Red, "DeltaMA");
                AddPlot(Brushes.Orange, "Trend");
            }
            else if (State == State.Configure) {
                deltaBuffer = new RingBuffer(lookback);
                volumeBuffer = new RingBuffer(lookback);
            }
        }

        public void Calculate(double[] prices, double[] volumes) {
            if (prices == null || volumes == null || prices.Length == 0)
                return;

            for (int i = 0; i < prices.Length; i++) {
                double delta = (prices[i] > (i > 0 ? prices[i - 1] : prices[i])) ? volumes[i] : -volumes[i];
                deltaBuffer.Append(delta);
                volumeBuffer.Append(volumes[i]);
            }
        }

        public double GetResult() {
            return deltaBuffer.GetSum();
        }

        protected override void OnBarUpdate() {
            if (CurrentBar < lookback)
                return;

            double delta = 0;
            if (Close[0] > (CurrentBar > 0 ? Close[1] : Close[0]))
                delta = Volume[0];
            else if (Close[0] < (CurrentBar > 0 ? Close[1] : Close[0]))
                delta = -Volume[0];

            deltaBuffer.Append(delta);
            volumeBuffer.Append(Volume[0]);

            double cumulativeDelta = GetResult();
            double avgVolume = volumeBuffer.GetAverage();
            double dayPnL = cumulativeDelta / (avgVolume + 0.0001);

            Values[0][0] = cumulativeDelta;
            Values[1][0] = (cumulativeDelta + (CurrentBar > 0 ? Values[1][1] : 0)) / 2;
            Values[2][0] = dayPnL > 0 ? 1 : (dayPnL < 0 ? -1 : 0);

            sessionPnL = dayPnL;
        }

        public override string DisplayName => "Smart Delta Calculator";

        ~SmartDeltaCalculator() {
            deltaBuffer?.Clear();
            volumeBuffer?.Clear();
        }
    }
}
