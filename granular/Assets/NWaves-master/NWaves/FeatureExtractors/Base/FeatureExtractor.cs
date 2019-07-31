﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NWaves.Signals;

namespace NWaves.FeatureExtractors.Base
{
    /// <summary>
    /// Abstract class for all feature extractors
    /// </summary>
    public abstract class FeatureExtractor
    {
        /// <summary>
        /// Number of features to extract
        /// </summary>
        public abstract int FeatureCount { get; }

        /// <summary>
        /// String annotations (or simply names) of features
        /// </summary>
        public abstract List<string> FeatureDescriptions { get; }

        /// <summary>
        /// String annotations (or simply names) of delta features (1st order derivatives)
        /// </summary>
        public virtual List<string> DeltaFeatureDescriptions
        {
            get { return FeatureDescriptions.Select(d => "delta_" + d).ToList(); }
        }

        /// <summary>
        /// String annotations (or simply names) of delta-delta features (2nd order derivatives)
        /// </summary>
        public virtual List<string> DeltaDeltaFeatureDescriptions
        {
            get { return FeatureDescriptions.Select(d => "delta_delta_" + d).ToList(); }
        }

        /// <summary>
        /// Length of analysis frame (in seconds)
        /// </summary>
        public double FrameDuration { get; protected set; }

        /// <summary>
        /// Hop length (in seconds)
        /// </summary>
        public double HopDuration { get; protected set; }

        /// <summary>
        /// Size of analysis frame (in samples)
        /// </summary>
        public int FrameSize { get; protected set; }

        /// <summary>
        /// Hop size (in samples)
        /// </summary>
        public int HopSize { get; protected set; }

        /// <summary>
        /// Sampling rate that the processed signals are expected to have
        /// </summary>
        public int SamplingRate { get; protected set; }

        /// <summary>
        /// Constructor requires FrameSize and HopSize to be set
        /// </summary>
        /// <param name="samplingRate"></param>
        /// <param name="frameSize"></param>
        /// <param name="hopSize"></param>
        protected FeatureExtractor(int samplingRate, int frameSize, int hopSize)
        {
            FrameDuration = (double) frameSize / samplingRate;
            HopDuration = (double) hopSize / samplingRate;
            FrameSize = frameSize;
            HopSize = hopSize;
            SamplingRate = samplingRate;
        }

        /// <summary>
        /// Constructor requires FrameSize and HopSize to be set
        /// </summary>
        /// <param name="samplingRate"></param>
        /// <param name="frameSize"></param>
        /// <param name="hopSize"></param>
        protected FeatureExtractor(int samplingRate, double frameDuration, double hopDuration)
        {
            FrameSize = (int) (samplingRate * frameDuration);
            HopSize = (int) (samplingRate * hopDuration);
            FrameDuration = frameDuration;
            HopDuration = hopDuration;
            SamplingRate = samplingRate;
        }

        /// <summary>
        /// Compute the sequence of feature vectors from some part of array of samples
        /// </summary>
        /// <param name="samples">Array of real-valued samples</param>
        /// <param name="startSample">The offset (position) of the first sample for processing</param>
        /// <param name="endSample">The offset (position) of last sample for processing</param>
        /// <returns>Sequence of feature vectors</returns>
        public abstract List<FeatureVector> ComputeFrom(float[] samples, int startSample, int endSample);

        /// <summary>
        /// Compute the sequence of feature vectors from the entire array of samples
        /// </summary>
        /// <param name="samples">Array of real-valued samples</param>
        /// <returns>Sequence of feature vectors</returns>
        public List<FeatureVector> ComputeFrom(float[] samples)
        {
            return ComputeFrom(samples, 0, samples.Length);
        }

        /// <summary>
        /// Compute the sequence of feature vectors from some fragment of a signal
        /// </summary>
        /// <param name="signal">Discrete real-valued signal</param>
        /// <param name="startSample">The offset (position) of the first sample for processing</param>
        /// <param name="endSample">The offset (position) of the last sample for processing</param>
        /// <returns>Sequence of feature vectors</returns>
        public List<FeatureVector> ComputeFrom(DiscreteSignal signal, int startSample, int endSample)
        {
            return ComputeFrom(signal.Samples, startSample, endSample);
        }

        /// <summary>
        /// Compute the sequence of feature vectors from the entire DiscreteSignal
        /// </summary>
        /// <param name="signal">Discrete real-valued signal</param>
        /// <returns>Sequence of feature vectors</returns>
        public List<FeatureVector> ComputeFrom(DiscreteSignal signal)
        {
            return ComputeFrom(signal, 0, signal.Length);
        }


        #region parallelization

        /// <summary>
        /// True if computations can be done in parallel
        /// </summary>
        /// <returns></returns>
        public virtual bool IsParallelizable() => false;

        /// <summary>
        /// Copy of current extractor that can work in parallel
        /// </summary>
        /// <returns></returns>
        public virtual FeatureExtractor ParallelCopy() => null;

        /// <summary>
        /// Parallel computation (returns chunks of fecture vector lists)
        /// </summary>
        /// <param name="samples"></param>
        /// <param name="startSample"></param>
        /// <param name="endSample"></param>
        /// <param name="parallelThreads"></param>
        /// <returns></returns>
        public virtual List<FeatureVector>[] ParallelChunksComputeFrom(float[] samples, int startSample, int endSample, int parallelThreads = 0)
        {
            if (!IsParallelizable())
            {
                throw new NotImplementedException();
            }

            var threadCount = parallelThreads > 0 ? parallelThreads : Environment.ProcessorCount;
            var chunkSize = (endSample - startSample) / threadCount;

            var extractors = new FeatureExtractor[threadCount];
            extractors[0] = this;
            for (var i = 1; i < threadCount; i++)
            {
                extractors[i] = ParallelCopy();
            }

            // ============== carefully define the sample positions for merging ===============

            var startPositions = new int[threadCount];
            var endPositions = new int[threadCount];

            var hopCount = (chunkSize - FrameSize) / HopSize;

            var lastPosition = startSample;
            for (var i = 0; i < threadCount; i++)
            {
                startPositions[i] = lastPosition;
                endPositions[i] = lastPosition + hopCount * HopSize + FrameSize;
                lastPosition = endPositions[i] - FrameSize;
            }

            endPositions[threadCount - 1] = endSample;

            // =========================== actual parallel computing ===========================

            var featureVectors = new List<FeatureVector>[threadCount];

            Parallel.For(0, threadCount, i =>
            {
                featureVectors[i] = extractors[i].ComputeFrom(samples, startPositions[i], endPositions[i]);
            });

            return featureVectors;
        }

        /// <summary>
        /// Parallel computation (joins chunks of feature vector lists into one list)
        /// </summary>
        /// <param name="samples"></param>
        /// <param name="startSample"></param>
        /// <param name="endSample"></param>
        /// <param name="parallelThreads"></param>
        /// <returns></returns>
        public virtual List<FeatureVector> ParallelComputeFrom(float[] samples, int startSample, int endSample, int parallelThreads = 0)
        {
            var chunks = ParallelChunksComputeFrom(samples, startSample, endSample, parallelThreads);

            var featureVectors = new List<FeatureVector>();

            foreach (var vectors in chunks)
            {
                featureVectors.AddRange(vectors);
            }

            return featureVectors;
        }

        /// <summary>
        /// Parallel computation (joins chunks of feature vector lists into one list)
        /// </summary>
        /// <param name="samples"></param>
        /// <param name="parallelThreads"></param>
        /// <returns></returns>
        public virtual List<FeatureVector> ParallelComputeFrom(float[] samples, int parallelThreads = 0)
        {
            return ParallelComputeFrom(samples, 0, samples.Length, parallelThreads);
        }

        /// <summary>
        /// Compute the sequence of feature vectors from some fragment of a signal
        /// </summary>
        /// <param name="signal">Discrete real-valued signal</param>
        /// <param name="startSample">The offset (position) of the first sample for processing</param>
        /// <param name="endSample">The offset (position) of the last sample for processing</param>
        /// <param name="parallelThreads">Number of threads</param>
        /// <returns>Sequence of feature vectors</returns>
        public List<FeatureVector> ParallelComputeFrom(DiscreteSignal signal, int startSample, int endSample, int parallelThreads = 0)
        {
            return ParallelComputeFrom(signal.Samples, startSample, endSample, parallelThreads);
        }

        /// <summary>
        /// Compute the sequence of feature vectors from the entire DiscreteSignal
        /// </summary>
        /// <param name="signal">Discrete real-valued signal</param>
        /// <param name="parallelThreads">Number of threads</param>
        /// <returns>Sequence of feature vectors</returns>
        public List<FeatureVector> ParallelComputeFrom(DiscreteSignal signal, int parallelThreads = 0)
        {
            return ParallelComputeFrom(signal.Samples, 0, signal.Length, parallelThreads);
        }

        #endregion
    }
}
