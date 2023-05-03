using System;
using System.Collections;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Unity.WebRTC;

public class Subscriber : MonoBehaviour
{
    public enum Protocol
    {
        HTTP,
        HTTPS
    }

    [Header("WHIP")]
    [Tooltip("http://localhost:1985/rtc/v1/whip-play/?app=live&stream=livestream")]
    public Protocol protocol = Protocol.HTTP;
    [Tooltip("http://localhost:1985/rtc/v1/whip-play/?app=live&stream=livestream")]
    public string host = "localhost";
    [Tooltip("http://localhost:1985/rtc/v1/whip-play/?app=live&stream=livestream")]
    public int port = 1985;
    [Tooltip("http://localhost:1985/rtc/v1/whip-play/?app=live&stream=livestream")]
    public string app = "live";
    [Tooltip("http://localhost:1985/rtc/v1/whip-play/?app=live&stream=livestream")]
    public string stream = "livestream";
    public string url { get { return $"{this.protocol.ToString().ToLower()}://{this.host}:{this.port}/rtc/v1/whip-play/?app={this.app}&stream={this.stream}"; } }

    [Header("Media")]
    public AudioSource audioSource;
    public RawImage rawImage;

    private MediaStream mediaStream;
    private RTCPeerConnection peerConnection;

    private void Awake()
    {
        WebRTC.Initialize();
        Debug.Log("WebRTC: Initialize ok");
    }

    private void Start()
    {
        Debug.Log($"WebRTC: Start to play {url}");

        // Start WebRTC update.
        StartCoroutine(WebRTC.Update());

        // Create object only after WebRTC initialized.
        peerConnection = new RTCPeerConnection();
        mediaStream = new MediaStream();

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
            mediaStream.AddTrack(e.Track);
        };

        // Setup player media stream.
        mediaStream.OnAddTrack = e =>
        {
            Debug.Log($"WebRTC: OnAddTrack {e.ToString()}");
            if( e.Track is VideoStreamTrack videoTrack )
            {
                videoTrack.OnVideoReceived += tex =>
                {
                    Debug.Log($"WebRTC: OnVideoReceived {videoTrack.ToString()}, tex={tex.width}x{tex.height}");
                    rawImage.texture = tex;

                    var width = tex.width < 1280 ? tex.width : 1280;
                    var height = tex.width > 0 ? width * tex.height / tex.width : 720;
                    rawImage.rectTransform.sizeDelta = new Vector2(width, height);
                };
            }
            if( e.Track is AudioStreamTrack audioTrack )
            {
                Debug.Log($"WebRTC: OnAudioReceived {audioTrack.ToString()}");
                audioSource.SetTrack(audioTrack);
                audioSource.loop = true;
                audioSource.Play();
            }
        };

        // Setup PeerConnection to receive stream only.
        StartCoroutine(SetupPeerConnection());
        IEnumerator SetupPeerConnection()
        {
            RTCRtpTransceiverInit init = new RTCRtpTransceiverInit();
            init.direction = RTCRtpTransceiverDirection.RecvOnly;
            peerConnection.AddTransceiver(TrackKind.Audio, init);
            peerConnection.AddTransceiver(TrackKind.Video, init);

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