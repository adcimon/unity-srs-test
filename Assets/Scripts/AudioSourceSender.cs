using UnityEngine;
using Unity.WebRTC;

[RequireComponent(typeof(AudioSource))]
public class AudioSourceSender : MediaSender
{
    private AudioSource audioSource;
    private AudioStreamTrack audioTrack;

    private void Awake()
    {
        audioSource = this.GetComponent<AudioSource>();
    }

    private void Start()
    {
        audioTrack = new AudioStreamTrack(audioSource);
    }

    public override MediaStreamTrack GetTrack()
    {
        return audioTrack;
    }
}