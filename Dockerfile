FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env
ENV TZ="Europe/Berlin"
WORKDIR /App

# Copy only project files first to cache dotnet restore separately
COPY FreezeTune/FreezeTune.csproj FreezeTune/
RUN dotnet restore FreezeTune/FreezeTune.csproj

# Now copy everything and build
COPY . ./
RUN dotnet publish FreezeTune/FreezeTune.csproj -f net9.0 -c Release -o out -p:StaticWebAssetsEnabled=false

# Install Playwright browsers in the SDK stage
RUN dotnet tool install --global Microsoft.Playwright.CLI \
    && /root/.dotnet/tools/playwright install chromium

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /App

# --- Stable layers first (cached as long as these don't change) ---

RUN apt-get update && apt-get install -y ffmpeg \
    build-essential zlib1g-dev libncurses5-dev libgdbm-dev libnss3-dev \
    libssl-dev libreadline-dev libffi-dev libsqlite3-dev wget libbz2-dev \
    && wget https://www.python.org/ftp/python/3.12.8/Python-3.12.8.tgz \
    && tar -xf Python-3.12.8.tgz \
    && cd Python-3.12.8 && ./configure --enable-optimizations --prefix=/usr/local \
    && make -j$(nproc) && make altinstall \
    && cd .. && rm -rf Python-3.12.8 Python-3.12.8.tgz \
    && apt-get purge -y build-essential && apt-get autoremove -y \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

# Install Playwright dependencies and Chromium
RUN apt-get update && apt-get install -y \
    libnss3 libnspr4 libatk1.0-0 libatk-bridge2.0-0 libcups2 libdrm2 \
    libxkbcommon0 libxcomposite1 libxdamage1 libxfixes3 libxrandr2 \
    libgbm1 libasound2 libpango-1.0-0 libcairo2 libatspi2.0-0 \
    curl \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

# Copy Playwright browsers from build stage
COPY --from=build-env /root/.cache/ms-playwright /root/.cache/ms-playwright

# Install tidal-dl-ng from local fork
COPY tidal-dl-ng-For-DJ-master.zip /tmp/
RUN python3.12 -m venv /opt/tidal-dl-ng \
    && /opt/tidal-dl-ng/bin/pip install --no-cache-dir /tmp/tidal-dl-ng-For-DJ-master.zip \
    && ln -s /opt/tidal-dl-ng/bin/tidal-dl-ng /usr/local/bin/tidal-dl-ng \
    && rm /tmp/tidal-dl-ng-For-DJ-master.zip

RUN tidal-dl-ng cfg download_base_path "/data/vid/tmp"
RUN tidal-dl-ng cfg quality_video 1080
RUN tidal-dl-ng cfg path_binary_ffmpeg "/usr/bin/ffmpeg"
RUN tidal-dl-ng cfg video_convert_mp4 true

RUN python3.12 -m venv /opt/yt-dlp \
    && /opt/yt-dlp/bin/pip install --no-cache-dir "yt-dlp @ https://github.com/yt-dlp/yt-dlp-nightly-builds/releases/latest/download/yt-dlp.tar.gz" curl_cffi \
    && ln -s /opt/yt-dlp/bin/yt-dlp /usr/local/bin/yt-dlp

# --- App output last (changes every deploy, but nothing below gets invalidated) ---
COPY --from=build-env /App/out .

ENTRYPOINT ["dotnet", "FreezeTune.dll"]
