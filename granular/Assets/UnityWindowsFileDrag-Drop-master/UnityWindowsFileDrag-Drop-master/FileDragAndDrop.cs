using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using B83.Win32;


public class FileDragAndDrop : MonoBehaviour
{

    public AudioLoader m_loader;

    // important to keep the instance alive while the hook is active.
    UnityDragAndDropHook hook;
    void OnEnable ()
    {
        // must be created on the main thread to get the right thread id.
        hook = new UnityDragAndDropHook();
        hook.InstallHook();
        hook.OnDroppedFiles += OnFiles;
    }
    void OnDisable()
    {
        hook.UninstallHook();
    }

    void OnFiles(List<string> aFiles, POINT aPos)
    {
        m_loader.LoadAudio(aFiles[0]);
    }
}
