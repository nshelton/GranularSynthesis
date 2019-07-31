using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NWaves.Audio;
using NWaves.Signals;
using System.IO;

public class AudioLoader : MonoBehaviour
{
    public LineRenderer m_renderer;
 
    public string testFileToLoad;

    DiscreteSignal left, right;

    private void Start()
    {
        Test();
    }

    [ContextMenu("LoadTest")]
    private void Test()
    {
        LoadAudio("Assets/" + testFileToLoad);
    }

    public void LoadAudio(string path)
    {
        if (path.EndsWith(".wav"))
        {
            using (var stream = new FileStream(path, FileMode.Open))
            {
                var waveFile = new WaveFile(stream);
                left = waveFile[Channels.Left];
                right = waveFile[Channels.Right];

                PlotLine(left);
            //    GranularSynthesizer.SourceSignal = left;
            }
        }
    }

    private void PlotLine(DiscreteSignal signal)
    {
        m_renderer.positionCount = signal.Samples.Length;

        for(int i = 0; i < m_renderer.positionCount; i ++)
        {
            m_renderer.SetPosition(i, new Vector3((float)i / m_renderer.positionCount, signal[i], 0));
        }

    }
}
