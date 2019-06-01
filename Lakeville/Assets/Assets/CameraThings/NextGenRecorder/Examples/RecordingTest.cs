using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using pmjo.NextGenRecorder;
// using pmjo.NextGenRecorder.Sharing;

public class RecordingTest : MonoBehaviour
{
    void Start()
    {
        if (Recorder.IsSupported)
        {
            StartCoroutine(RecordForSeconds(2.0f, 5.0f));
        }
    }

    void OnEnable()
    {
        Recorder.RecordingStarted += RecordingStarted;
        Recorder.RecordingStopped += RecordingStopped;
        Recorder.RecordingExported += RecordingExported;
    }

    void OnDisable()
    {
        Recorder.RecordingStarted -= RecordingStarted;
        Recorder.RecordingStopped -= RecordingStopped;
        Recorder.RecordingExported -= RecordingExported;
    }

    IEnumerator RecordForSeconds(float startDelaySeconds, float recordingSeconds)
    {
        Recorder.PrepareRecording();

        yield return new WaitForSeconds(startDelaySeconds);

        Recorder.StartRecording();

        yield return new WaitForSeconds(recordingSeconds);

        Recorder.StopRecording();
    }

    private void RecordingStarted(long sessionId)
    {
        Debug.Log("Recording " + sessionId + " was started.");
    }

    private void RecordingStopped(long sessionId)
    {
        Debug.Log("Recording " + sessionId + " was stopped.");

        Recorder.ExportRecordingSession(sessionId);
    }

    void RecordingExported(long sessionId, string path, Recorder.ErrorCode errorCode)
    {
        if (errorCode == Recorder.ErrorCode.NoError)
        {
            Debug.Log("Recording exported to " + path + ", session id " + sessionId);

            // Sharing.SaveToPhotos(path);
        }
        else
        {
            Debug.Log("Failed to export recording, error code " + errorCode + ", session id " + sessionId);
        }
    }
}
