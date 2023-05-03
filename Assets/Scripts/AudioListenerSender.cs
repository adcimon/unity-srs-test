using UnityEngine;
using Unity.WebRTC;

[RequireComponent(typeof(AudioListener))]
public class AudioListenerSender : MediaSender
{
    public int sampleRate = 48000;

    private AudioStreamTrack audioTrack;

    private void Start()
    {
        audioTrack = new AudioStreamTrack();
    }

    private void OnAudioFilterRead( float[] data, int channels )
    {
        if( audioTrack != null )
        {
            audioTrack.SetData(data, channels, sampleRate);
        }
    }

    public override MediaStreamTrack GetTrack()
    {
        return audioTrack;
    }
}