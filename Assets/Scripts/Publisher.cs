using System;
using System.Collections;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using UnityEngine;
using Unity.WebRTC;

public class Publisher : MonoBehaviour
{
	public enum Protocol
	{
		HTTP,
		HTTPS
	}

	[Header("WHIP")]
	[Tooltip("http://localhost:1985/rtc/v1/whip/?app=live&stream=livestream")]
	public Protocol protocol = Protocol.HTTP;
	[Tooltip("http://localhost:1985/rtc/v1/whip/?app=live&stream=livestream")]
	public string host = "localhost";
	[Tooltip("http://localhost:1985/rtc/v1/whip/?app=live&stream=livestream")]
	public int port = 1985;
	[Tooltip("http://localhost:1985/rtc/v1/whip/?app=live&stream=livestream")]
	public string app = "live";
	[Tooltip("http://localhost:1985/rtc/v1/whip/?app=live&stream=livestream")]
	public string stream = "livestream";
	public string url { get { return $"{this.protocol.ToString().ToLower()}://{this.host}:{this.port}/rtc/v1/whip/?app={this.app}&stream={this.stream}"; } }

	[Header("Media")]
	public MediaSender audioSender;
	public MediaSender videoSender;

	private RTCPeerConnection peerConnection;

	public void Publish()
	{
		Debug.Log($"Publishing to {url}");

		peerConnection = new RTCPeerConnection();

		peerConnection.OnIceCandidate = candidate =>
		{
			Debug.Log($"OnIceCandidate candidate={candidate.ToString()}");
		};
		peerConnection.OnIceConnectionChange = state =>
		{
			Debug.Log($"OnIceConnectionChange state={state.ToString()}");
		};
		peerConnection.OnTrack = e =>
		{
			Debug.Log($"OnTrack kind={e.Track.Kind} id={e.Track.Id}");
		};

		StartCoroutine(SetupPeerConnection());
		IEnumerator SetupPeerConnection()
		{
			RTCRtpTransceiverInit init = new RTCRtpTransceiverInit();
			init.direction = RTCRtpTransceiverDirection.SendOnly;
			peerConnection.AddTransceiver(TrackKind.Audio, init);
			peerConnection.AddTransceiver(TrackKind.Video, init);

			yield return StartCoroutine(GrabVideo());
		}

		IEnumerator GrabVideo()
		{
			peerConnection.AddTrack(videoSender.GetTrack());
			Debug.Log($"Add video track, id={videoSender.GetTrack().Id}");

			yield return StartCoroutine(GrabAudio());
		}

		IEnumerator GrabAudio()
		{
			peerConnection.AddTrack(audioSender.GetTrack());
			Debug.Log($"Add audio track, id={audioSender.GetTrack().Id}");

			yield return StartCoroutine(PeerNegotiationNeeded());
		}

		IEnumerator PeerNegotiationNeeded()
		{
			var op = peerConnection.CreateOffer();
			yield return op;

			Debug.Log($"CreateOffer done={op.IsDone} hasError={op.IsError} offer={op.Desc}");
			if (op.IsError)
			{
				yield break;
			}

			yield return StartCoroutine(OnCreateOfferSuccess(op.Desc));
		}

		IEnumerator OnCreateOfferSuccess(RTCSessionDescription offer)
		{
			var op = peerConnection.SetLocalDescription(ref offer);
			Debug.Log($"SetLocalDescription type={offer.type} sdp={offer.sdp}");
			yield return op;

			Debug.Log($"Offer done={op.IsDone} hasError={op.IsError}");
			if (op.IsError)
			{
				yield break;
			}

			yield return StartCoroutine(ExchangeSDP(url, offer.sdp));
		}

		IEnumerator ExchangeSDP(string url, string offer)
		{
			var task = Task<string>.Run(async () =>
			{
				Uri uri = new UriBuilder(url).Uri;
				Debug.Log($"Build uri {uri}");

				var content = new StringContent(offer);
				content.Headers.ContentType = new MediaTypeHeaderValue("application/sdp");

				var client = new HttpClient();
				var res = await client.PostAsync(uri, content);
				res.EnsureSuccessStatusCode();

				string data = await res.Content.ReadAsStringAsync();
				Debug.Log($"Exchange SDP success, answer={data}");
				return data;
			});

			yield return new WaitUntil(() => task.IsCompleted);

			if (task.Exception != null)
			{
				Debug.Log($"Exchange SDP failure, url={url} error={task.Exception.ToString()}");
				yield break;
			}

			StartCoroutine(OnGotAnswerSuccess(task.Result));
		}

		IEnumerator OnGotAnswerSuccess(string answer)
		{
			RTCSessionDescription desc = new RTCSessionDescription();
			desc.type = RTCSdpType.Answer;
			desc.sdp = answer;
			var op = peerConnection.SetRemoteDescription(ref desc);
			yield return op;

			Debug.Log($"Answer done={op.IsDone} hasError={op.IsError}");
			yield break;
		}
	}

	private void OnDestroy()
	{
		peerConnection?.Close();
		peerConnection?.Dispose();
		peerConnection = null;
	}
}