using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class oscillator : MonoBehaviour
{
    public Note note = Note.A4;
    public float gain = 0.5f;
    public float overtone = 1;

    private double sampleLength;
    private float freq;

    private void Update()
    {
        sampleLength = 1.0 / AudioSettings.outputSampleRate;
        freq = Notes.GetFreq(note);
    }

    double lastFrequency = 0;
    double lastSampleVal = 0;
    double phaseOffset = 0;

    void OnAudioFilterRead(float[] data, int channels)
    {
        int dataLen = data.Length;
        double starttime = AudioSettings.dspTime;

        phaseOffset += 2 * Math.PI * starttime * (lastFrequency - freq);

        for ( int i = 0; i < dataLen; i +=2)
        {
            double time = starttime + ((double)i/2) * sampleLength;
            data[i + 0] += (float)(gain * Math.Sin(2 * Math.PI * time * freq + phaseOffset));
            data[i + 1] += (float)(gain * Math.Sin(2 * Math.PI * time * freq + phaseOffset));
        }

        lastSampleVal = data[dataLen - 1];
        lastFrequency = freq;
    }
}
