﻿using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NWaves.Operations.Convolution;
using NWaves.Signals;
using NWaves.Transforms;
using NWaves.Utils;

namespace NWaves.Filters.Base
{
    /// <summary>
    /// Class representing Finite Impulse Response filters
    /// </summary>
    public class FirFilter : LtiFilter
    {
        /// <summary>
        /// Filter's kernel.
        /// 
        /// Numerator part coefficients in filter's transfer function 
        /// (non-recursive part in difference equations)
        /// </summary>
        protected double[] _kernel;
        protected double[] Kernel
        {
            get
            {
                return _kernel;
            }
            set
            {
                _kernel = value;
                _kernel32 = _kernel.ToFloats();
                Tf = new TransferFunction(_kernel, new [] { 1.0 });
            }
        }
        
        /// <summary>
        /// Float versions of filter coefficients for computations by default
        /// </summary>
        protected float[] _kernel32;

        /// <summary>
        /// If Kernel.Length exceeds this value, 
        /// the filtering code will always call Overlap-Add routine.
        /// </summary>
        public const int FilterSizeForOptimizedProcessing = 64;

        /// <summary>
        /// Internal buffer for delay line
        /// </summary>
        protected float[] _delayLine;

        /// <summary>
        /// Current offset in delay line
        /// </summary>
        protected int _delayLineOffset;

        /// <summary>
        /// Constructor accepting the kernel of a filter
        /// </summary>
        /// <param name="kernel"></param>
        public FirFilter(IEnumerable<double> kernel)
        {
            Kernel = kernel.ToArray();
            ResetInternals();
        }

        /// <summary>
        /// Apply filter to entire signal
        /// </summary>
        /// <param name="signal"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        public override DiscreteSignal ApplyTo(DiscreteSignal signal, 
                                               FilteringMethod method = FilteringMethod.Auto)
        {
            if (_kernel.Length >= FilterSizeForOptimizedProcessing && method == FilteringMethod.Auto)
            {
                method = FilteringMethod.OverlapAdd;
            }

            switch (method)
            {
                case FilteringMethod.OverlapAdd:
                {
                    var fftSize = MathUtils.NextPowerOfTwo(4 * Kernel.Length);
                    var blockConvolver = OlaBlockConvolver.FromFilter(this, fftSize);
                    return blockConvolver.ApplyTo(signal);
                }
                case FilteringMethod.OverlapSave:
                {
                    var fftSize = MathUtils.NextPowerOfTwo(4 * Kernel.Length);
                    var blockConvolver = OlsBlockConvolver.FromFilter(this, fftSize);
                    return blockConvolver.ApplyTo(signal);
                }
                case FilteringMethod.Custom:
                {
                    return this.ProcessChunks(signal);
                }
                default:
                {
                    return ApplyFilterDirectly(signal);
                }
            }
        }

        /// <summary>
        /// FIR online filtering (sample-by-sample)
        /// </summary>
        /// <param name="sample"></param>
        /// <returns></returns>
        public override float Process(float sample)
        {
            var output = 0.0f;

            _delayLine[_delayLineOffset] = sample;

            var pos = 0;
            for (var k = _delayLineOffset; k < _kernel32.Length; k++)
            {
                output += _kernel32[pos++] * _delayLine[k];
            }
            for (var k = 0; k < _delayLineOffset; k++)
            {
                output += _kernel32[pos++] * _delayLine[k];
            }

            if (--_delayLineOffset < 0)
            {
                _delayLineOffset = _delayLine.Length - 1;
            }

            return output;
        }

        /// <summary>
        /// The most straightforward implementation of the difference equation:
        /// code the difference equation as it is
        /// </summary>
        /// <param name="signal"></param>
        /// <returns></returns>
        public DiscreteSignal ApplyFilterDirectly(DiscreteSignal signal)
        {
            var input = signal.Samples;

            var output = new float[input.Length + _kernel32.Length - 1];

            for (var n = 0; n < output.Length; n++)
            {
                for (var k = 0; k < _kernel32.Length; k++)
                {
                    if (n >= k && n < input.Length + k)
                    {
                        output[n] += _kernel32[k] * input[n - k];
                    }
                }
            }

            return new DiscreteSignal(signal.SamplingRate, output);
        }

        /// <summary>
        /// Reset internal buffer
        /// </summary>
        private void ResetInternals()
        {
            if (_delayLine == null)
            {
                _delayLine = new float[_kernel32.Length];
            }
            else
            {
                for (var i = 0; i < _delayLine.Length; i++)
                {
                    _delayLine[i] = 0;
                }
            }
            _delayLineOffset = _delayLine.Length - 1;
        }

        /// <summary>
        /// Reset filter
        /// </summary>
        public override void Reset()
        {
            ResetInternals();
        }

        /// <summary>
        /// Frequency response of an FIR filter is the FT of its impulse response
        /// </summary>
        public override ComplexDiscreteSignal FrequencyResponse(int length = 512)
        {
            var real = Kernel.PadZeros(length);
            var imag = new double[length];

            var fft = new Fft64(length);
            fft.Direct(real, imag);

            return new ComplexDiscreteSignal(1, real.Take(length / 2 + 1),
                                                imag.Take(length / 2 + 1));
        }

        /// <summary>
        /// Impulse response of an FIR filter is its kernel
        /// </summary>
        public override double[] ImpulseResponse(int length = 512)
        {
            return Kernel.ToArray();    // copy
        } 

        /// <summary>
        /// Convert to IIR filter
        /// </summary>
        /// <returns></returns>
        public IirFilter AsIir()
        {
            return new IirFilter(Kernel, new []{ 1.0 });
        }

        /// <summary>
        /// Load kernel from csv file
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="delimiter"></param>
        public static FirFilter FromCsv(Stream stream, char delimiter = ',')
        {
            using (var reader = new StreamReader(stream))
            {
                var content = reader.ReadToEnd();
                var kernel = content.Split(delimiter).Select(s => double.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture));
                return new FirFilter(kernel);
            }
        }

        /// <summary>
        /// Serialize kernel to csv file
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="delimiter"></param>
        public void ToCsv(Stream stream, char delimiter = ',')
        {
            using (var writer = new StreamWriter(stream))
            {
                var content = string.Join(delimiter.ToString(), Kernel.Select(k => k.ToString(CultureInfo.InvariantCulture)));
                writer.WriteLine(content);
            }
        }

        /// <summary>
        /// Sequential combination of two FIR filters (also an FIR filter)
        /// </summary>
        /// <param name="filter1"></param>
        /// <param name="filter2"></param>
        /// <returns></returns>
        public static FirFilter operator *(FirFilter filter1, FirFilter filter2)
        {
            var tf = filter1.Tf * filter2.Tf;
            return new FirFilter(tf.Numerator);
        }

        /// <summary>
        /// Sequential combination of an FIR and IIR filter
        /// </summary>
        /// <param name="filter1"></param>
        /// <param name="filter2"></param>
        /// <returns></returns>
        public static IirFilter operator *(FirFilter filter1, IirFilter filter2)
        {
            var tf = filter1.Tf * filter2.Tf;
            return new IirFilter(tf.Numerator, tf.Denominator);
        }

        /// <summary>
        /// Parallel combination of two FIR filters
        /// </summary>
        /// <param name="filter1"></param>
        /// <param name="filter2"></param>
        /// <returns></returns>
        public static FirFilter operator +(FirFilter filter1, FirFilter filter2)
        {
            var tf = filter1.Tf + filter2.Tf;
            return new FirFilter(tf.Numerator);
        }

        /// <summary>
        /// Parallel combination of an FIR and IIR filter
        /// </summary>
        /// <param name="filter1"></param>
        /// <param name="filter2"></param>
        /// <returns></returns>
        public static IirFilter operator +(FirFilter filter1, IirFilter filter2)
        {
            var tf = filter1.Tf + filter2.Tf;
            return new IirFilter(tf.Numerator, tf.Denominator);
        }
    }
}
