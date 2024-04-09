using UnityEngine;
using Unity.WebRTC;

[RequireComponent(typeof(Camera))]
public class CameraSender : MediaSender
{
	public bool useCurrentResolution = true;
	public int width = 1280;
	public int height = 720;

	private Camera cam;
	private VideoStreamTrack videoTrack;

	private void Awake()
	{
		cam = this.GetComponent<Camera>();
	}

	private void Start()
	{
		if (useCurrentResolution)
		{
			width = Screen.currentResolution.width;
			height = Screen.currentResolution.height;
		}

		CaptureStream(width, height);
	}

	public void CaptureStream(int width, int height)
	{
		videoTrack = cam.CaptureStreamTrack(width, height);
	}

	public override MediaStreamTrack GetTrack()
	{
		return videoTrack;
	}
}