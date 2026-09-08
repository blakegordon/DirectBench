using System.Diagnostics;

namespace DirectBench
{
    /// <summary>
    /// Splits FPS into an immediate (rolling window) rate shown during the
    /// run and a measured average that ignores the warmup period.
    /// </summary>
    public sealed class FrameCounter
    {
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private readonly double _warmupSeconds;
        private readonly double _measureSeconds;

        private int _windowFrames;
        private double _windowStartSeconds;
        private double _instantFps;

        private long _totalFrames;
        private long _measuredFrames;
        private double _measureStartSeconds = -1.0;
        private bool _finished;

        public FrameCounter(double warmupSeconds, double measureSeconds)
        {
            _warmupSeconds = warmupSeconds;
            _measureSeconds = measureSeconds;
        }

        public double InstantFps
        {
            get { return _instantFps; }
        }

        public double AverageFps { get; private set; }

        public long MeasuredFrames
        {
            get { return _measuredFrames; }
        }

        public double ElapsedSeconds
        {
            get { return _stopwatch.Elapsed.TotalSeconds; }
        }

        public double MeasuredSeconds { get; private set; }

        public bool IsWarmingUp
        {
            get { return ElapsedSeconds < _warmupSeconds; }
        }

        public bool IsFinished
        {
            get { return _finished; }
        }

        public double MeasureProgress
        {
            get
            {
                if (_measureStartSeconds < 0.0)
                {
                    return 0.0;
                }

                if (_measureSeconds <= 0.0)
                {
                    return 0.0;
                }

                double t = (ElapsedSeconds - _measureStartSeconds) / _measureSeconds;
                if (t < 0.0)
                {
                    return 0.0;
                }

                if (t > 1.0)
                {
                    return 1.0;
                }

                return t;
            }
        }

        public void Start()
        {
            _stopwatch.Reset();
            _stopwatch.Start();
            _windowStartSeconds = 0.0;
            _windowFrames = 0;
            _totalFrames = 0;
            _measuredFrames = 0;
            _measureStartSeconds = -1.0;
            _finished = false;
            AverageFps = 0.0;
            _instantFps = 0.0;
        }

        public void Tick()
        {
            if (_finished || !_stopwatch.IsRunning)
            {
                return;
            }

            double elapsed = _stopwatch.Elapsed.TotalSeconds;
            _totalFrames++;
            _windowFrames++;

            double windowLength = elapsed - _windowStartSeconds;
            if (windowLength >= 0.25)
            {
                _instantFps = _windowFrames / windowLength;
                _windowFrames = 0;
                _windowStartSeconds = elapsed;
            }

            if (elapsed < _warmupSeconds)
            {
                return;
            }

            if (_measureStartSeconds < 0.0)
            {
                _measureStartSeconds = elapsed;
            }

            _measuredFrames++;
            MeasuredSeconds = elapsed - _measureStartSeconds;
            if (MeasuredSeconds > 0.0)
            {
                AverageFps = _measuredFrames / MeasuredSeconds;
            }

            if (_measureSeconds > 0.0 && MeasuredSeconds >= _measureSeconds)
            {
                Complete();
            }
        }

        public void Complete()
        {
            if (_finished)
            {
                return;
            }

            double elapsed = _stopwatch.Elapsed.TotalSeconds;
            if (_measureStartSeconds >= 0.0 && _measuredFrames > 0)
            {
                MeasuredSeconds = elapsed - _measureStartSeconds;
                if (MeasuredSeconds > 0.0)
                {
                    AverageFps = _measuredFrames / MeasuredSeconds;
                }
            }
            else if (elapsed > 0.0)
            {
                MeasuredSeconds = elapsed;
                AverageFps = _totalFrames / elapsed;
            }

            _finished = true;
            _stopwatch.Stop();
        }
    }
}
