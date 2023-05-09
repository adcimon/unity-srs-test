using UnityEngine;
using Unity.WebRTC;

public abstract class MediaSender : MonoBehaviour
{
	public abstract MediaStreamTrack GetTrack();
}