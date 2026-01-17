docker build -t freezetube-image .


using docker compose (recommened):

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
    restart: unless-stopped
```

