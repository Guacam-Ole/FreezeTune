# FreezeTune

A game to guess music videos. Can be seen in action on (freezetune.com)

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

It will be required when calling orga.html
