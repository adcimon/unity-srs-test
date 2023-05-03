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

    private void Awake()
    {
        WebRTC.Initialize();
        Debug.Log("WebRTC: Initialize ok");
    }

    private void Start()
    {
        Debug.Log($"WebRTC: Start to stream {url}");

        // Start WebRTC update.
        StartCoroutine(WebRTC.Update());

        // Create object only after WebRTC initialized.
        peerConnection = new RTCPeerConnection();

        // Setup player peer connection.
        peerConnection.OnIceCandidate = candidate =>
        {
            Debug.Log($"WebRTC: OnIceCandidate {candidate.ToString()}");
        };
        peerConnection.OnIceConnectionChange = state =>
        {
            Debug.Log($"WebRTC: OnIceConnectionChange {state.ToString()}");
        };
        peerConnection.OnTrack = e =>
        {
            Debug.Log($"WebRTC: OnTrack {e.Track.Kind} id={e.Track.Id}");
        };

        // Setup PeerConnection to send stream only.
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
            Debug.Log($"WebRTC: Add video track, id={videoSender.GetTrack().Id}");

            yield return StartCoroutine(GrabAudio());
        }

        IEnumerator GrabAudio()
        {
            peerConnection.AddTrack(audioSender.GetTrack());
            Debug.Log($"WebRTC: Add audio track, id={audioSender.GetTrack().Id}");

            yield return StartCoroutine(PeerNegotiationNeeded());
        }

        // Generate offer.
        IEnumerator PeerNegotiationNeeded()
        {
            var op = peerConnection.CreateOffer();
            yield return op;

            Debug.Log($"WebRTC: CreateOffer done={op.IsDone}, hasError={op.IsError}, {op.Desc}");
            if( op.IsError )
            {
                yield break;
            }

            yield return StartCoroutine(OnCreateOfferSuccess(op.Desc));
        }

        // When offer is ready, set to local description.
        IEnumerator OnCreateOfferSuccess( RTCSessionDescription offer )
        {
            var op = peerConnection.SetLocalDescription(ref offer);
            Debug.Log($"WebRTC: SetLocalDescription {offer.type} {offer.sdp}");
            yield return op;

            Debug.Log($"WebRTC: Offer done={op.IsDone}, hasError={op.IsError}");
            if( op.IsError )
            {
                yield break;
            }

            yield return StartCoroutine(ExchangeSDP(url, offer.sdp));
        }

        // Exchange SDP(offer) with server, got answer.
        IEnumerator ExchangeSDP( string url, string offer )
        {
            // Use Task to call async methods.
            var task = Task<string>.Run(async () =>
            {
                Uri uri = new UriBuilder(url).Uri;
                Debug.Log($"WebRTC: Build uri {uri}");

                var content = new StringContent(offer);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/sdp");

                var client = new HttpClient();
                var res = await client.PostAsync(uri, content);
                res.EnsureSuccessStatusCode();

                string data = await res.Content.ReadAsStringAsync();
                Debug.Log($"WebRTC: Exchange SDP ok, answer is {data}");
                return data;
            });

            // Covert async to coroutine yield, wait for task to be completed.
            yield return new WaitUntil(() => task.IsCompleted);

            // Check async task exception, it won't throw it automatically.
            if( task.Exception != null )
            {
                Debug.Log($"WebRTC: Exchange SDP failed, url={url}, err is {task.Exception.ToString()}");
                yield break;
            }

            StartCoroutine(OnGotAnswerSuccess(task.Result));
        }

        // When got answer, set remote description.
        IEnumerator OnGotAnswerSuccess( string answer )
        {
            RTCSessionDescription desc = new RTCSessionDescription();
            desc.type = RTCSdpType.Answer;
            desc.sdp = answer;
            var op = peerConnection.SetRemoteDescription(ref desc);
            yield return op;

            Debug.Log($"WebRTC: Answer done={op.IsDone}, hasError={op.IsError}");
            yield break;
        }
    }

    private void OnDestroy()
    {
        peerConnection?.Close();
        peerConnection?.Dispose();
        peerConnection = null;

        WebRTC.Dispose();
        Debug.Log("WebRTC: Dispose ok");
    }
}