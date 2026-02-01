FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env
ENV TZ="Europe/Berlin"
WORKDIR /App

# Copy everything
COPY . ./

# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -f net9.0 -c Release -o out -p:StaticWebAssetsEnabled=false

# Build runtime image using Python 3.12 as base
FROM python:3.12-slim-bookworm
WORKDIR /App

# Install .NET 9.0 runtime
RUN apt-get update && apt-get install -y wget \
    && wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && rm packages-microsoft-prod.deb \
    && apt-get update \
    && apt-get install -y aspnetcore-runtime-9.0 ffmpeg \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

# Copy the built .NET app
COPY --from=build-env /App/out .

# Install tidal-dl-ng from local fork
COPY tidal-dl-ng-For-DJ-master.zip /tmp/
RUN pip install --upgrade pip && pip install --no-cache-dir /tmp/tidal-dl-ng-For-DJ-master.zip && rm /tmp/tidal-dl-ng-For-DJ-master.zip

RUN tidal-dl-ng cfg download_base_path "/data/vid/tmp"
RUN tidal-dl-ng cfg quality_video 1080
RUN tidal-dl-ng cfg path_binary_ffmpeg "/usr/bin/ffmpeg"
RUN tidal-dl-ng cfg video_convert_mp4 true

ENTRYPOINT ["dotnet", "FreezeTune.dll"]
