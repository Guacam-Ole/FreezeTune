# FreezeTune

A game to guess music videos. Can be seen in action on [freezetune.com](https://freezetune.com)

Installation using Docker compose:

```
services:
  freezetune:
    build: .
    ports:
      - "8821:8080"
    volumes:
      - /data:/data
    environment:
      - TZ=Europe/Berlin
      - FREEZEAPIKEY=thisIsVerySecret
    restart: unless-stopped
```

Replace FREEZEAPIKEY with something meaningful.

It will be required when calling org.html

the `data` directory must contain the following subfolders:
`vid`
`img`
`tmp`
`db`

## Tidal
If you want to use Tidal (instead of Youtube) as a video source you have to authenticate once in a while. Just call
```
docker exec -it freezetune-freezetune-1 tidal-dl-ng login
```

(The docker name might vary a bit) and follow the instructions
