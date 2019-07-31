using System;
using System.Collections.Generic;
using UnityEngine;
using NWaves.Audio;
using NWaves.Signals;

public class granular : MonoBehaviour
{

    public enum GrainTableType
    {
        Random,
        Skip,
        Single
    }

    public class Grain
    {
        public int Start = 0;
        public int Length = 100;
    }


    public float grainSizeMS = 20;
    public int grainOffset = 20;
    public float gain = 0.5f;
    public GrainTableType grainTableType;
    public int skipAmount = 2;
    public int skipLength = 20;

    private double sampleLength;

    private ulong currentSample;
    Grain evenGrain;
    Grain oddGrain;

    double[] window;

    System.Random rand = new System.Random();

    public DiscreteSignal SourceSignal { set; get; }

    private void Start()
    {
        evenGrain = new Grain();
        evenGrain.Start = 0;

        oddGrain = new Grain();
        oddGrain.Start = 0;

        int grainSizeSamples = (int)(AudioSettings.outputSampleRate * grainSizeMS / 1000f);
        window = NWindow.BlackmanHarris(grainSizeSamples);
    }

    private void Update()
    {
        sampleLength = 1.0 / AudioSettings.outputSampleRate;
        int grainSizeSamples = (int)(AudioSettings.outputSampleRate * grainSizeMS / 1000f);

        evenGrain.Length = grainSizeSamples;
        oddGrain.Length = grainSizeSamples;

        if ( window.Length != grainSizeSamples)
        {
            window = NWindow.BlackmanHarris(grainSizeSamples);
        }

    }

    private float GetGrainVal(Grain grain, int sampleInGrain)
    {
        int currentGrain = (int)grain.Length * (grain.Start);
        return SourceSignal.Samples[(currentGrain + sampleInGrain) % SourceSignal.Samples.Length];
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        if (SourceSignal == null)
            return;

        int dataLen = data.Length;
        double starttime = AudioSettings.dspTime;

        for ( int i = 0; i < dataLen; i +=2)
        {

            int evenGrainOffset = (int)(currentSample % (ulong)evenGrain.Length);
            int oddGrainOffset = (int)((currentSample + (ulong)evenGrain.Length/2) % (ulong)evenGrain.Length);
            float evenGrainWindow = (float) window[evenGrainOffset];
            float oddGrainWindow =  (float) window[oddGrainOffset];
                
            data[i + 0] += gain * (evenGrainWindow * GetGrainVal(evenGrain, evenGrainOffset) + oddGrainWindow * GetGrainVal(evenGrain, oddGrainOffset)) / (evenGrainWindow + oddGrainWindow);
            data[i + 1] += data[i + 0];
            currentSample++;

            // find next EvenGrain
            if (evenGrainOffset == evenGrain.Length - 1)
            {
                if (grainTableType == GrainTableType.Random)
                {
                    evenGrain.Start = rand.Next(100) + grainOffset;
                }
                else if (grainTableType == GrainTableType.Skip)
                {
                    evenGrain.Start = (evenGrain.Start + grainOffset + skipAmount) % (skipLength * skipAmount);
                }
                else
                {
                    evenGrain.Start = grainOffset;
                }
            }

            // find next OddGrain
            if (oddGrainOffset == oddGrain.Length - 1)
            {
                oddGrain.Start = evenGrain.Start;
            }
        }
    }
}
