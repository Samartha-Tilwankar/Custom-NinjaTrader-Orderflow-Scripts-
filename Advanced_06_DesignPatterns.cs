using System;
using System.Collections.Generic;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;

namespace NinjaTrader.NinjaScript.Indicators
{
    public sealed class IndicatorFactory {
        private static readonly Dictionary<string, Func<IOrderFlowIndicator>> registry =
            new Dictionary<string, Func<IOrderFlowIndicator>>();

        static IndicatorFactory() {
            registry["delta"] = () => new DeltaIndicatorImpl();
            registry["volume"] = () => new VolumeIndicatorImpl();
            registry["imbalance"] = () => new ImbalanceIndicatorImpl();
        }

        public static IOrderFlowIndicator Create(string typeKey) {
            if (registry.ContainsKey(typeKey.ToLower()))
                return registry[typeKey.ToLower()].Invoke();
            throw new ArgumentException($"Unknown indicator type: {typeKey}");
        }

        public static void RegisterIndicator(string key, Func<IOrderFlowIndicator> factory) {
            if (factory != null)
                registry[key.ToLower()] = factory;
        }
    }

    public interface IOrderFlowIndicator {
        string Name { get; }
        double Calculate(double[] data);
    }

    public class DeltaIndicatorImpl : IOrderFlowIndicator {
        public string Name => "DeltaIndicator";

        public double Calculate(double[] data) {
            if (data == null || data.Length == 0)
                return 0;
            double sum = 0;
            for (int i = 0; i < data.Length; i++)
                sum += data[i];
            return sum;
        }
    }

    public class VolumeIndicatorImpl : IOrderFlowIndicator {
        public string Name => "VolumeIndicator";

        public double Calculate(double[] data) {
            if (data == null || data.Length == 0)
                return 0;
            double max = data[0];
            for (int i = 1; i < data.Length; i++)
                if (data[i] > max)
                    max = data[i];
            return max;
        }
    }

    public class ImbalanceIndicatorImpl : IOrderFlowIndicator {
        public string Name => "ImbalanceIndicator";

        public double Calculate(double[] data) {
            if (data == null || data.Length < 2)
                return 0;
            double up = 0, down = 0;
            for (int i = 0; i < data.Length; i++) {
                if (data[i] > 0)
                    up += data[i];
                else
                    down += Math.Abs(data[i]);
            }
            double total = up + down;
            return total > 0 ? Math.Max(up, down) / total : 0;
        }
    }

    public sealed class ConfigurationManager {
        private static ConfigurationManager instance;
        private static readonly object lockOb = new object();
        private readonly Dictionary<string, object> config;

        private ConfigurationManager() {
            config = new Dictionary<string, object>();
        }

        public static ConfigurationManager Instance {
            get {
                if (instance == null) {
                    lock (lockOb) {
                        if (instance == null)
                            instance = new ConfigurationManager();
                    }
                }
                return instance;
            }
        }

        public void Set(string key, object value) {
            lock (config) {
                config[key] = value;
            }
        }

        public object Get(string key) {
            lock (config) {
                return config.ContainsKey(key) ? config[key] : null;
            }
        }

        public int GetInt(string key, int defVal = 0) {
            object val = Get(key);
            return val != null && int.TryParse(val.ToString(), out int res) ? res : defVal;
        }

        public double GetDouble(string key, double defVal = 0.0) {
            object val = Get(key);
            return val != null && double.TryParse(val.ToString(), out double res) ? res : defVal;
        }
    }

    public class FactoryPatternIndicator : Indicator {
        private IOrderFlowIndicator calc;
        private double[] volBuf;
        private double[] deltaBuf;
        private int bufIdx;
        private readonly int bufSz = 30;

        protected override void OnStateChange() {
            if (State == State.SetDefaults) {
                Description = "Factory Pattern - Dynamic indicator creation and configuration";
                Name = "FactoryPatternIndicator";
                Calculate = Calculate.OnBarClose;
                IsOverlay = false;
                DisplayInDataBox = true;

                AddPlot(Brushes.DodgerBlue, "FactoryResult");
                AddPlot(Brushes.Green, "Type");
            }
            else if (State == State.Configure) {
                var cfg = ConfigurationManager.Instance;
                cfg.Set("analysis_type", "delta");
                calc = IndicatorFactory.Create(cfg.Get("analysis_type").ToString());

                volBuf = new double[bufSz];
                deltaBuf = new double[bufSz];
                bufIdx = 0;
            }
        }

        protected override void OnBarUpdate() {
            if (calc == null)
                return;

            double delta = Close[0] - (CurrentBar > 0 ? Close[1] : Close[0]);
            
            deltaBuf[bufIdx] = delta;
            volBuf[bufIdx] = Volume[0];
            bufIdx = (bufIdx + 1) % bufSz;

            double result = calc.Calculate(deltaBuf);
            Values[0][0] = result;
            Values[1][0] = calc.Name.Contains("Delta") ? 1 : 0;
        }

        public override string DisplayName => "Factory Pattern Indicator";
    }

    public class ObserverPatternIndicator : Indicator {
        private List<IIndicatorObserver> observers;

        protected override void OnStateChange() {
            if (State == State.SetDefaults) {
                Description = "Observer Pattern - Event-driven architecture";
                Name = "ObserverPatternIndicator";
                Calculate = Calculate.OnBarClose;
                IsOverlay = false;
                DisplayInDataBox = true;

                AddPlot(Brushes.Purple, "Events");
            }
            else if (State == State.Configure) {
                observers = new List<IIndicatorObserver>();
                observers.Add(new HighVolumeObserver(Volumes[0]));
                observers.Add(new LargeMovementObserver(Close[0]));
            }
        }

        protected override void OnBarUpdate() {
            int eventCount = 0;
            foreach (var obs in observers) {
                if (obs.OnBarsUpdate(Open[0], High[0], Low[0], Close[0], Volume[0]))
                    eventCount++;
            }
            Values[0][0] = eventCount;
        }

        public override string DisplayName => "Observer Pattern Indicator";
    }

    public interface IIndicatorObserver {
        bool OnBarsUpdate(double o, double h, double l, double c, long vol);
    }

    public class HighVolumeObserver : IIndicatorObserver {
        private readonly long threshold;

        public HighVolumeObserver(long thresh) {
            threshold = thresh;
        }

        public bool OnBarsUpdate(double o, double h, double l, double c, long vol) {
            return vol > threshold * 1.5;
        }
    }

    public class LargeMovementObserver : IIndicatorObserver {
        private readonly double threshold;

        public LargeMovementObserver(double thresh) {
            threshold = thresh;
        }

        public bool OnBarsUpdate(double o, double h, double l, double c, long vol) {
            double move = Math.Abs(c - o);
            return move > threshold * 0.02;
        }
    }
}
