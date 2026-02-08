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
      - /dockervolumes/freezetune/config.json:/App/config.json
      - /dockervolumes/freezetune/tidal:/root/.config/tidal_dl_ng-dev
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

The config will contain the config, the tidal directory is used to store the credentials. This is optional but makes sure you dont have to re-authenticate each time you deploy the container. 

## Tidal
If you want to use Tidal (instead of Youtube) as a video source you have to authenticate once in a while. Just call
```
docker exec -it freezetune-freezetune-1 tidal-dl-ng login
```

(The docker name might vary a bit) and follow the instructions. You need a payed subscription!. Using Tidal is optional. You can just stick to YouTube if you want

## Configuration
Configuration is done via config.json

```
{
  "BasePath": "/data",
  "Width": 640,
  "Height": 480,
  "MaxDistance": 0.2,
  "Categories":["80s","90s","Hamburg"],
  "CategoryKeys": [
    {
      "Key":"80s", 
      "Value":"supermagicsecretfor80s"
    }
  ]
  
}
```

Where `BasePath` is the folder where the data is stored, width + height for display of images, and CategoryKeys (optional) are specific secrets per category.
