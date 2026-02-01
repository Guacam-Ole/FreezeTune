FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env
ENV TZ="Europe/Berlin"
WORKDIR /App

# Copy everything
COPY . ./

# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -f net9.0 -c Release -o out -p:StaticWebAssetsEnabled=false

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /App

RUN apt-get update && apt-get install -y ffmpeg python3 python3-pip python3-venv \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

# Copy the built .NET app
COPY --from=build-env /App/out .

# Install tidal-dl-ng from local fork
COPY tidal-dl-ng-For-DJ-master.zip /tmp/
RUN python3 -m venv /opt/tidal-dl-ng \
    && /opt/tidal-dl-ng/bin/pip install --no-cache-dir /tmp/tidal-dl-ng-For-DJ-master.zip \
    && ln -s /opt/tidal-dl-ng/bin/tidal-dl-ng /usr/local/bin/tidal-dl-ng \
    && rm /tmp/tidal-dl-ng-For-DJ-master.zip

RUN tidal-dl-ng cfg download_base_path "/data/vid/tmp"
RUN tidal-dl-ng cfg quality_video 1080
RUN tidal-dl-ng cfg path_binary_ffmpeg "/usr/bin/ffmpeg"
RUN tidal-dl-ng cfg video_convert_mp4 true

ENTRYPOINT ["dotnet", "FreezeTune.dll"]
