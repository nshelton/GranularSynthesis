using System;
using System.Collections.Generic;
using UnityEngine;
using NWaves.Audio;
using NWaves.Signals;
using System.IO;

public class granularDCT : MonoBehaviour
{

    public enum GrainTableType
    {
        Random,
        Skip,
        Single
    }

    public class FrequencyGrain
    {
        public int Start = 0;
        public int Length = 100;

        public float[] samples;
        public float[] weights;
    }

    public string sampleFile;

    public float grainSizeMS = 20;
    public int grainOffset = 20;
    public float gain = 0.5f;
    public GrainTableType grainTableType;
    public int skipAmount = 2;
    public int skipLength = 20;

    private int m_grainSizeFFT;
    private double sampleLength;
    private ulong currentSample;

    List<FrequencyGrain> m_grains;

    FrequencyGrain m_currentGrain;

    System.Random rand = new System.Random();
    float[] m_outputBuffer;
    public DiscreteSignal SourceSignal { set; get; }

    [ContextMenu("LoadSample")]
    private void Test()
    {
        LoadAudio("Assets/" + sampleFile);
    }

    public void LoadAudio(string path)
    {
        if (path.EndsWith(".wav"))
        {
            using (var stream = new FileStream(path, FileMode.Open))
            {
                var waveFile = new WaveFile(stream);
                SourceSignal = waveFile[Channels.Left]; ;
            }
        }
    }


    private void Start()
    {

        LoadAudio(sampleFile);

        CollectGrains();

    }


    private void CollectGrains()
    {
        m_grains = new List<FrequencyGrain>();
        int numsamples = SourceSignal.Length;
        int grainSizeSamples = (int)(AudioSettings.outputSampleRate * grainSizeMS / 1000f);
        int numGrains = numsamples / grainSizeSamples - 1;
        m_grainSizeFFT = 2;
        while (m_grainSizeFFT < grainSizeSamples)
        {
            m_grainSizeFFT *= 2;
        }

        Debug.Log($"numsamples {numsamples}");
        Debug.Log($"grainSizeSamples {grainSizeSamples}");
        Debug.Log($"grainSizeFFT {m_grainSizeFFT}");
        Debug.Log($"numGrains {numGrains}");

        var fft = new NWaves.Transforms.Dct2(m_grainSizeFFT, m_grainSizeFFT);
        float[] source = new float[m_grainSizeFFT];

        for (int i = 0; i < numsamples - m_grainSizeFFT; i += grainSizeSamples)
        {
            var grain = new FrequencyGrain();
            grain.Start = i;
            grain.Length = grainSizeSamples;

            grain.samples = new float[m_grainSizeFFT];
            grain.weights = new float[m_grainSizeFFT];

            for (int j = 0; j < m_grainSizeFFT; j++)
            {
                grain.samples[j] = SourceSignal.Samples[j + i];
            }

            fft.Direct(grain.samples, grain.weights);
            m_grains.Add(grain);
        }
    }

    private void Update()
    {

        sampleLength = 1.0 / AudioSettings.outputSampleRate;
        int grainSizeSamples = (int)(AudioSettings.outputSampleRate * grainSizeMS / 1000f);

    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        Debug.Log(data.Length);
        if (SourceSignal == null)
            return;

        if (m_outputBuffer == null || m_outputBuffer.Length != data.Length / channels)
        {
            m_outputBuffer = new float[data.Length / channels];
        }

        var fft = new NWaves.Transforms.Dct2(m_grainSizeFFT, data.Length);

        int dataLen = data.Length;
        double starttime = AudioSettings.dspTime;

        fft.Inverse(m_grains[grainOffset].weights, m_outputBuffer);

        for ( int i = 0; i < dataLen-2; i +=2)
        {
            data[i] = m_outputBuffer[i/2];
            data[i+1] = m_outputBuffer[i/2+1];
        }
    }
}
