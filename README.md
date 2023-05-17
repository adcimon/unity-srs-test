# Unity SRS Template

Unity and [SRS](https://github.com/ossrs) template project.

<p align="center"><img align="center" src="example.gif"></p>

Requirements:
* Unity 2021.3.23f1
* [Unity WebRTC 3.0.0-pre.5](https://github.com/Unity-Technologies/com.unity.webrtc)
* [SRS ossrs/srs:5](https://hub.docker.com/r/ossrs/srs)

## Deploy

Run container.
```
CANDIDATE="<ip>"
docker run \
	--name srs \
	--rm \
	-d \
	-p 1935:1935 \
	-p 1985:1985 \
	-p 8080:8080 \
	-p 8000:8000/udp \
	--env CANDIDATE=$CANDIDATE \
	ossrs/srs:5 ./objs/srs \
	-c conf/docker.conf
```

Configuration file ([docker.conf](https://github.com/ossrs/srs/blob/develop/trunk/conf/docker.conf)).
```
# docker config for srs.
# @see full.conf for detail config.

listen              1935;
max_connections     1000;
# For docker, please use docker logs to manage the logs of SRS.
# See https://docs.docker.com/config/containers/logging/
srs_log_tank        console;
daemon              off;
http_api {
    enabled         on;
    listen          1985;
}
http_server {
    enabled         on;
    listen          8080;
    dir             ./objs/nginx/html;
}
rtc_server {
    enabled on;
    listen 8000;
    # @see https://ossrs.net/lts/zh-cn/docs/v4/doc/webrtc#config-candidate
    candidate $CANDIDATE;
}
vhost __defaultVhost__ {
    hls {
        enabled         on;
    }
    http_remux {
        enabled     on;
        mount       [vhost]/[app]/[stream].flv;
    }
    rtc {
        enabled     on;
        # @see https://ossrs.net/lts/zh-cn/docs/v4/doc/webrtc#rtmp-to-rtc
        rtmp_to_rtc on;
        # @see https://ossrs.net/lts/zh-cn/docs/v4/doc/webrtc#rtc-to-rtmp
        rtc_to_rtmp on;
    }
}
```