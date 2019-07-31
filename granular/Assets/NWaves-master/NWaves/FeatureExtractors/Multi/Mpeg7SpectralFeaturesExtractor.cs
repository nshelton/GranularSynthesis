﻿using System;
using System.Collections.Generic;
using System.Linq;
using NWaves.FeatureExtractors.Base;
using NWaves.Features;
using NWaves.Filters.Fda;
using NWaves.Transforms;
using NWaves.Utils;
using NWaves.Windows;

namespace NWaves.FeatureExtractors.Multi
{
    /// <summary>
    /// Extractor of spectral features according to methodology described in MPEG7 standard.
    /// 
    /// It's a flexible extractor that allows varying almost everything.
    /// 
    /// The difference between Mpeg7SpectralExtractor and SpectralExtractor is that 
    /// former calculates spectral features from total energy in frequency BANDS
    /// while latter analyzes signal energy at particular frequencies (spectral bins).
    /// 
    /// Also, optionally it allows computing harmonic features along with spectral features.
    /// 
    /// </summary>
    public class Mpeg7SpectralFeaturesExtractor : FeatureExtractor
    {
        /// <summary>
        /// Names of supported spectral features
        /// </summary>
        public const string FeatureSet = "centroid, spread, flatness, noiseness, rolloff, crest, entropy, decrease, loudness, sharpness";

        /// <summary>
        /// Names of supported harmonic features
        /// </summary>
        public const string HarmonicSet = "hcentroid, hspread, inharmonicity, oer, t1+t2+t3";

        /// <summary>
        /// String annotations (or simply names) of features
        /// </summary>
        public override List<string> FeatureDescriptions { get; }

        /// <summary>
        /// Number of features to extract
        /// </summary>
        public override int FeatureCount => FeatureDescriptions.Count;

        /// <summary>
        /// Size of used FFT
        /// </summary>
        private readonly int _fftSize;

        /// <summary>
        /// FFT transformer
        /// </summary>
        private readonly Fft _fft;

        /// <summary>
        /// Type of the window function
        /// </summary>
        private readonly WindowTypes _window;

        /// <summary>
        /// Window samples
        /// </summary>
        private readonly float[] _windowSamples;

        /// <summary>
        /// Filterbank from frequency bands
        /// </summary>
        private readonly float[][] _filterbank;

        /// <summary>
        /// Internal buffer for frequency bands
        /// </summary>
        private Tuple<double, double, double>[] _frequencyBands;

        /// <summary>
        /// Internal buffer for central frequencies
        /// </summary>
        private float[] _frequencies;

        /// <summary>
        /// Internal buffer for harmonic peak frequencies (optional)
        /// </summary>
        private float[] _peakFrequencies;

        /// <summary>
        /// Internal buffer for spectral positions of harmonic peaks (optional)
        /// </summary>
        private int[] _peaks;

        /// <summary>
        /// Internal buffer for magnitude spectrum
        /// </summary>
        private float[] _spectrum;

        /// <summary>
        /// Internal buffer for total energies in frequency bands
        /// </summary>
        private float[] _mappedSpectrum;

        /// <summary>
        /// Internal buffer for currently processed block
        /// </summary>
        private float[] _block;

        /// <summary>
        /// Internal block of zeros for a quick memset
        /// </summary>
        private float[] _zeroblock;

        /// <summary>
        /// Extractor functions
        /// </summary>
        private List<Func<float[], float[], float>> _extractors;

        /// <summary>
        /// Extractor parameters
        /// </summary>
        private readonly IReadOnlyDictionary<string, object> _parameters;

        /// <summary>
        /// Harmonic extractor functions (optional)
        /// </summary>
        private List<Func<float[], int[], float[], float>> _harmonicExtractors;

        /// <summary>
        /// Pitch estimator function (optional)
        /// </summary>
        private Func<float[], float> _pitchEstimator;

        /// <summary>
        /// Array of precomputed pitches (optional)
        /// </summary>
        private float[] _pitchTrack;

        /// <summary>
        /// Harmonic peaks detector function (optional)
        /// </summary>
        Action<float[], int[], float[], int, float> _peaksDetector;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="samplingRate"></param>
        /// <param name="featureList"></param>
        /// <param name="frameDuration"></param>
        /// <param name="hopDuration"></param>
        /// <param name="fftSize"></param>
        /// <param name="frequencyBands"></param>
        /// <param name="parameters"></param>
        public Mpeg7SpectralFeaturesExtractor(int samplingRate,
                                              string featureList,
                                              double frameDuration = 0.0256/*sec*/,
                                              double hopDuration = 0.010/*sec*/,
                                              int fftSize = 0,
                                              Tuple<double, double, double>[] frequencyBands = null,
                                              WindowTypes window = WindowTypes.Hamming,
                                              IReadOnlyDictionary<string, object> parameters = null)

            : base(samplingRate, frameDuration, hopDuration)
        {
            if (featureList == "all" || featureList == "full")
            {
                featureList = FeatureSet;
            }

            var features = featureList.Split(',', '+', '-', ';', ':')
                                      .Select(f => f.Trim().ToLower());

            _extractors = features.Select<string, Func<float[], float[], float>>(feature =>
            {
                switch (feature)
                {
                    case "sc":
                    case "centroid":
                        return Spectral.Centroid;

                    case "ss":
                    case "spread":
                        return Spectral.Spread;

                    case "sfm":
                    case "flatness":
                        if (parameters?.ContainsKey("minLevel") ?? false)
                        {
                            var minLevel = (float)parameters["minLevel"];
                            return (spectrum, freqs) => Spectral.Flatness(spectrum, minLevel);
                        }
                        else
                        {
                            return (spectrum, freqs) => Spectral.Flatness(spectrum);
                        }

                    case "sn":
                    case "noiseness":
                        if (parameters?.ContainsKey("noiseFrequency") ?? false)
                        {
                            var noiseFrequency = (float)parameters["noiseFrequency"];
                            return (spectrum, freqs) => Spectral.Noiseness(spectrum, freqs, noiseFrequency);
                        }
                        else
                        {
                            return (spectrum, freqs) => Spectral.Noiseness(spectrum, freqs);
                        }

                    case "rolloff":
                        if (parameters?.ContainsKey("rolloffPercent") ?? false)
                        {
                            var rolloffPercent = (float)parameters["rolloffPercent"];
                            return (spectrum, freqs) => Spectral.Rolloff(spectrum, freqs, rolloffPercent);
                        }
                        else
                        {
                            return (spectrum, freqs) => Spectral.Rolloff(spectrum, freqs);
                        }

                    case "crest":
                        return (spectrum, freqs) => Spectral.Crest(spectrum);

                    case "entropy":
                    case "ent":
                        return (spectrum, freqs) => Spectral.Entropy(spectrum);

                    case "sd":
                    case "decrease":
                        return (spectrum, freqs) => Spectral.Decrease(spectrum);

                    case "loud":
                    case "loudness":
                        return (spectrum, freqs) => Perceptual.Loudness(spectrum);

                    case "sharp":
                    case "sharpness":
                        return (spectrum, freqs) => Perceptual.Sharpness(spectrum);

                    default:
                        return (spectrum, freqs) => 0;
                }
            }).ToList();

            FeatureDescriptions = features.ToList();

            _fftSize = fftSize > FrameSize ? fftSize : MathUtils.NextPowerOfTwo(FrameSize);
            _fft = new Fft(_fftSize);

            _window = window;
            if (_window != WindowTypes.Rectangular)
            {
                _windowSamples = Window.OfType(_window, FrameSize);
            }

            _frequencyBands = frequencyBands ?? FilterBanks.OctaveBands(6, _fftSize, samplingRate);
            _filterbank = FilterBanks.Rectangular(_fftSize, samplingRate, _frequencyBands);

            var cfs = _frequencyBands.Select(b => b.Item2).ToList();
            // insert zero frequency so that it'll be ignored during calculations
            // just like in case of FFT spectrum (0th DC component)
            cfs.Insert(0, 0);
            _frequencies = cfs.ToFloats();

            _parameters = parameters;

            // reserve memory for reusable blocks

            _spectrum = new float[_fftSize / 2 + 1];                // buffer for magnitude spectrum
            _mappedSpectrum = new float[_filterbank.Length + 1];    // buffer for total energies in bands
            _block = new float[_fftSize];                           // buffer for currently processed block
            _zeroblock = new float[_fftSize];                       // just a buffer of zeros for quick memset
        }

        /// <summary>
        /// Add set of harmonic features to calculation list
        /// </summary>
        /// <param name="featureList"></param>
        /// <param name="peakCount"></param>
        /// <param name="pitchEstimator"></param>
        /// <param name="lowPitch"></param>
        /// <param name="highPitch"></param>
        public void IncludeHarmonicFeatures(string featureList,
                                            int peakCount = 10,
                                            Func<float[], float> pitchEstimator = null,
                                            Action<float[], int[], float[], int, float> peaksDetector = null,
                                            float lowPitch = 80,
                                            float highPitch = 400)
        {
            if (featureList == "all" || featureList == "full")
            {
                featureList = HarmonicSet;
            }

            var features = featureList.Split(',', '+', '-', ';', ':')
                                      .Select(f => f.Trim().ToLower());

            _harmonicExtractors = features.Select<string, Func<float[], int[], float[], float>>(feature =>
            {
                switch (feature)
                {
                    case "hc":
                    case "hcentroid":
                        return Harmonic.Centroid;

                    case "hs":
                    case "hspread":
                        return Harmonic.Spread;

                    case "inh":
                    case "inharmonicity":
                        return Harmonic.Inharmonicity;

                    case "oer":
                    case "oddevenratio":
                        return (spectrum, peaks, freqs) => Harmonic.OddToEvenRatio(spectrum, peaks);

                    case "t1":
                    case "t2":
                    case "t3":
                        return (spectrum, peaks, freqs) => Harmonic.Tristimulus(spectrum, peaks, int.Parse(feature.Substring(1)));

                    default:
                        return (spectrum, peaks, freqs) => 0;
                }
            }).ToList();

            FeatureDescriptions.AddRange(features);

            if (pitchEstimator == null)
            {
                _pitchEstimator = spectrum => Pitch.FromSpectralPeaks(spectrum, SamplingRate, lowPitch, highPitch);
            }
            else
            {
                _pitchEstimator = pitchEstimator;
            }

            if (peaksDetector == null)
            {
                _peaksDetector = Harmonic.Peaks;
            }
            else
            {
                _peaksDetector = peaksDetector;
            }

            _peaks = new int[peakCount];
            _peakFrequencies = new float[peakCount];
        }

        /// <summary>
        /// Add one more harmonic feature with routine for its calculation
        /// </summary>
        /// <param name="name"></param>
        /// <param name="algorithm"></param>
        public void AddHarmonicFeature(string name, Func<float[], int[], float[], float> algorithm)
        {
            if (_harmonicExtractors == null)
            {
                return;
            }

            FeatureDescriptions.Add(name);
            _harmonicExtractors.Add(algorithm);
        }

        /// <summary>
        /// Set array of precomputed pitches
        /// </summary>
        /// <param name="pitchTrack"></param>
        public void SetPitchTrack(float[] pitchTrack)
        {
            _pitchTrack = pitchTrack;
        }

        /// <summary>
        /// Compute the sequence of feature vectors from some fragment of a signal
        /// </summary>
        /// <param name="samples">Signal</param>
        /// <param name="startSample">The number (position) of the first sample for processing</param>
        /// <param name="endSample">The number (position) of last sample for processing</param>
        /// <returns>Sequence of feature vectors</returns>
        public override List<FeatureVector> ComputeFrom(float[] samples, int startSample, int endSample)
        {
            Guard.AgainstInvalidRange(startSample, endSample, "starting pos", "ending pos");

            var nullExtractorPos = _extractors.IndexOf(null);
            if (nullExtractorPos >= 0)
            {
                throw new ArgumentException($"Unknown feature: {FeatureDescriptions[nullExtractorPos]}");
            }

            var featureVectors = new List<FeatureVector>();

            var pitchPos = 0;

            var i = startSample;
            while (i + FrameSize < endSample)
            {
                // prepare all blocks in memory for the current step:

                _zeroblock.FastCopyTo(_block, _fftSize);
                samples.FastCopyTo(_block, FrameSize, i);

                // apply window if necessary

                if (_window != WindowTypes.Rectangular)
                {
                    _block.ApplyWindow(_windowSamples);
                }

                // compute and prepare spectrum

                _fft.MagnitudeSpectrum(_block, _spectrum);

                // apply filterbank (ignoring 0th coefficient)

                for (var k = 0; k < _filterbank.Length; k++)
                {
                    _mappedSpectrum[k + 1] = 0.0f;

                    for (var j = 0; j < _spectrum.Length; j++)
                    {
                        _mappedSpectrum[k + 1] += _filterbank[k][j] * _spectrum[j];
                    }
                }

                // extract spectral features

                var featureVector = new float[FeatureCount];

                for (var j = 0; j < _extractors.Count; j++)
                {
                    featureVector[j] = _extractors[j](_mappedSpectrum, _frequencies);
                }

                // ...and maybe harmonic features

                if (_harmonicExtractors != null)
                {
                    var pitch = _pitchTrack == null ? _pitchEstimator(_spectrum) : _pitchTrack[pitchPos++];

                    _peaksDetector(_spectrum, _peaks, _peakFrequencies, SamplingRate, pitch);

                    var offset = _extractors.Count;
                    for (var j = 0; j < _harmonicExtractors.Count; j++)
                    {
                        featureVector[j + offset] = _harmonicExtractors[j](_spectrum, _peaks, _peakFrequencies);
                    }
                }

                // finally create new feature vector

                featureVectors.Add(new FeatureVector
                {
                    Features = featureVector,
                    TimePosition = (double)i / SamplingRate
                });

                i += HopSize;
            }

            return featureVectors;
        }

        /// <summary>
        /// True if computations can be done in parallel
        /// </summary>
        /// <returns></returns>
        public override bool IsParallelizable() => true;

        /// <summary>
        /// Copy of current extractor that can work in parallel
        /// </summary>
        /// <returns></returns>
        public override FeatureExtractor ParallelCopy()
        {
            var spectralFeatureSet = string.Join(",", FeatureDescriptions.Take(_extractors.Count));

            var copy = new Mpeg7SpectralFeaturesExtractor(SamplingRate, spectralFeatureSet, FrameDuration, HopDuration, _fftSize, _frequencyBands, _window, _parameters)
            {
                _extractors = _extractors,
                _pitchTrack = _pitchTrack
            };

            if (_harmonicExtractors != null)
            {
                var harmonicFeatureSet = string.Join(",", FeatureDescriptions.Skip(_extractors.Count));
                copy.IncludeHarmonicFeatures(harmonicFeatureSet, _peaks.Length, _pitchEstimator);
            }

            return copy;
        }
    }
}
