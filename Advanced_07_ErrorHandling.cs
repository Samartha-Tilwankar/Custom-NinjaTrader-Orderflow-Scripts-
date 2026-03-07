using System;
using System.Collections.Generic;
using System.Diagnostics;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;

namespace NinjaTrader.NinjaScript.Indicators
{
    public abstract class TemplateMethodIndicator : Indicator {
        protected sealed override void OnBarUpdate() {
            try {
                Validate();
                PreProcess();
                Calculate();
                PostProcess();
            }
            catch (Exception ex) {
                HandleError(ex);
            }
        }

        protected abstract void Calculate();
        protected virtual void Validate() { }
        protected virtual void PreProcess() { }
        protected virtual void PostProcess() { }

        protected virtual void HandleError(Exception ex) {
            // log or handle
        }
    }

    public class Result<T> {
        public bool Success { get; private set; }
        public T Value { get; private set; }
        public string Error { get; private set; }

        private Result(bool success, T value, string error) {
            Success = success;
            Value = value;
            Error = error;
        }

        public static Result<T> Ok(T val) => new Result<T>(true, val, null);
        public static Result<T> Fail(string err) => new Result<T>(false, default(T), err);

        public TOut Match<TOut>(Func<T, TOut> onOk, Func<string, TOut> onFail) {
            return Success ? onOk(Value) : onFail(Error);
        }

        public void MatchVoid(Action<T> onOk, Action<string> onFail) {
            if (Success)
                onOk(Value);
            else
                onFail(Error);
        }
    }

    public class TypeSafeCalculator {
        public Result<double> CalculateDelta(IEnumerable<double> prices, IEnumerable<double> volumes) {
            try {
                if (prices == null || volumes == null)
                    return Result<double>.Fail("Null input data");

                var priceArr = GetArray(prices);
                var volArr = GetArray(volumes);

                if (priceArr == null || volArr == null || priceArr.Length == 0)
                    return Result<double>.Fail("Empty input data");

                if (priceArr.Length != volArr.Length)
                    return Result<double>.Fail("Mismatched array lengths");

                double sum = 0;
                for (int i = 0; i < priceArr.Length; i++) {
                    double delta = priceArr[i] > (i > 0 ? priceArr[i - 1] : priceArr[i]) ? volArr[i] : -volArr[i];
                    sum += delta;
                }

                return Result<double>.Ok(sum);
            }
            catch (Exception ex) {
                return Result<double>.Fail($"Calculation error: {ex.Message}");
            }
        }

        private double[] GetArray(IEnumerable<double> data) {
            try {
                var list = new List<double>(data);
                return list.ToArray();
            }
            catch {
                return null;
            }
        }
    }

    public class ErrorHandlingIndicator : TemplateMethodIndicator {
        private TypeSafeCalculator calc;
        private List<double> prices;
        private List<double> volumes;
        private readonly int maxSz = 50;

        protected override void OnStateChange() {
            if (State == State.SetDefaults) {
                Description = "Error Handling - Type-safe results and template method pattern";
                Name = "ErrorHandlingIndicator";
                Calculate = Calculate.OnBarClose;
                IsOverlay = false;
                DisplayInDataBox = true;

                AddPlot(Brushes.Red, "Delta");
                AddPlot(Brushes.Orange, "ErrorFlag");
            }
            else if (State == State.Configure) {
                calc = new TypeSafeCalculator();
                prices = new List<double>(maxSz);
                volumes = new List<double>(maxSz);
            }
        }

        protected override void Validate() {
            if (calc == null || prices == null || volumes == null)
                throw new InvalidOperationException("Indicator not initialized");
        }

        protected override void PreProcess() {
            if (prices.Count > maxSz)
                prices.RemoveRange(0, prices.Count - maxSz);
            if (volumes.Count > maxSz)
                volumes.RemoveRange(0, volumes.Count - maxSz);

            prices.Add(Close[0]);
            volumes.Add(Volume[0]);
        }

        protected override void Calculate() {
            var res = calc.CalculateDelta(prices, volumes);
            res.MatchVoid(
                val => {
                    Values[0][0] = val;
                    Values[1][0] = 0;
                },
                err => {
                    Values[0][0] = 0;
                    Values[1][0] = 1;
                }
            );
        }

        protected override void PostProcess() {
            // cleanup or logging
        }

        protected override void HandleError(Exception ex) {
            Values[1][0] = 1;
        }

        public override string DisplayName => "Error Handling Indicator";
    }

    public class GenericBufferIndicator<T> : Indicator where T : struct {
        private List<T> buffer;
        private readonly int capacity = 100;

        protected override void OnStateChange() {
            if (State == State.SetDefaults) {
                Description = "Generic Buffer - Compile-time type safety";
                Name = "GenericBufferIndicator";
                Calculate = Calculate.OnBarClose;
                IsOverlay = false;
                DisplayInDataBox = true;

                AddPlot(Brushes.Cyan, "Value");
            }
            else if (State == State.Configure) {
                buffer = new List<T>(capacity);
            }
        }

        protected override void OnBarUpdate() {
            // this demonstrates generic type safety
            Values[0][0] = 1;
        }

        public override string DisplayName => "Generic Buffer Indicator";
    }

    public class PerformanceMonitorIndicator : Indicator {
        private readonly Stopwatch sw = new Stopwatch();
        private double lastCalcTime = 0;

        protected override void OnStateChange() {
            if (State == State.SetDefaults) {
                Description = "Performance Monitor - Execution time tracking";
                Name = "PerformanceMonitorIndicator";
                Calculate = Calculate.OnBarClose;
                IsOverlay = false;
                DisplayInDataBox = true;

                AddPlot(Brushes.Green, "CalcTime_MS");
                AddPlot(Brushes.Red, "IsSlowing");
            }
        }

        protected override void OnBarUpdate() {
            sw.Restart();

            double sum = 0;
            for (int i = 0; i < 1000; i++) {
                double delta = Close[0] - (i > 0 ? Close[1] : Close[0]);
                sum += Math.Abs(delta);
            }

            sw.Stop();
            lastCalcTime = sw.Elapsed.TotalMilliseconds;

            Values[0][0] = lastCalcTime;
            Values[1][0] = lastCalcTime > 1.0 ? 1 : 0;
        }

        public override string DisplayName => "Performance Monitor Indicator";
    }
}
