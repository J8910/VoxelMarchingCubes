using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelMarchingCubes.Profiling
{
    /// <summary>
    /// Container for performance metrics with statistical analysis.
    /// Tracks min, max, average, and recent samples.
    /// </summary>
    [Serializable]
    public class PerformanceMetrics
    {
        [SerializeField] private string _name;
        [SerializeField] private float _currentValue;
        [SerializeField] private float _minValue;
        [SerializeField] private float _maxValue;
        [SerializeField] private float _averageValue;
        [SerializeField] private int _sampleCount;
        
        private Queue<float> _recentSamples = new Queue<float>(100);
        private const int MaxRecentSamples = 100;

        public string Name => _name;
        public float Current => _currentValue;
        public float Min => _minValue;
        public float Max => _maxValue;
        public float Average => _averageValue;
        public int SampleCount => _sampleCount;

        public PerformanceMetrics(string name)
        {
            _name = name;
            Reset();
        }

        public void RecordSample(float value)
        {
            _currentValue = value;
            _sampleCount++;

            // Update min/max
            if (_sampleCount == 1)
            {
                _minValue = value;
                _maxValue = value;
                _averageValue = value;
            }
            else
            {
                _minValue = Mathf.Min(_minValue, value);
                _maxValue = Mathf.Max(_maxValue, value);
                
                // Running average
                _averageValue = (_averageValue * (_sampleCount - 1) + value) / _sampleCount;
            }

            // Track recent samples
            _recentSamples.Enqueue(value);
            if (_recentSamples.Count > MaxRecentSamples)
            {
                _recentSamples.Dequeue();
            }
        }

        public float GetRecentAverage(int sampleCount = 10)
        {
            if (_recentSamples.Count == 0) return 0f;
            
            int count = Mathf.Min(sampleCount, _recentSamples.Count);
            float sum = 0f;
            int i = 0;
            
            foreach (var sample in _recentSamples)
            {
                if (i >= _recentSamples.Count - count) 
                    sum += sample;
                i++;
            }
            
            return sum / count;
        }

        public void Reset()
        {
            _currentValue = 0f;
            _minValue = float.MaxValue;
            _maxValue = float.MinValue;
            _averageValue = 0f;
            _sampleCount = 0;
            _recentSamples.Clear();
        }

        public override string ToString()
        {
            return $"{_name}: Current={_currentValue:F2}ms, Avg={_averageValue:F2}ms, Min={_minValue:F2}ms, Max={_maxValue:F2}ms ({_sampleCount} samples)";
        }
    }
}