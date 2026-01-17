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
COPY --from=build-env /App/out .

RUN apt-get update && apt-get install -y ffmpeg

ENTRYPOINT ["dotnet", "FreezeTune.dll"]
