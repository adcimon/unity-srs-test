using UnityEngine;
using UnityEngine.Events;
using Unity.WebRTC;

public class Main : MonoBehaviour
{
	public float publishDelay = 3;
	public UnityEvent onPublish;

	public float subscribeDelay = 3;
	public UnityEvent onSubscribe;

	private void Start()
	{
		WebRTC.Initialize();
		Debug.Log("WebRTC Initialize");

		StartCoroutine(WebRTC.Update());

		this.Invoke("OnPublish", publishDelay);
	}

	private void OnPublish()
	{
		if (onPublish != null)
		{
			onPublish.Invoke();
			this.Invoke("OnSubscribe", subscribeDelay);
		}
	}

	private void OnSubscribe()
	{
		if (onSubscribe != null)
		{
			onSubscribe.Invoke();
		}
	}

	private void OnDestroy()
	{
		WebRTC.Dispose();
		Debug.Log("WebRTC Dispose");
	}
}