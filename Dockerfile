# Build Stage
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0.316-alpine3.23 AS base
ARG TARGETARCH
WORKDIR /build
COPY . .
RUN dotnet restore -a $TARGETARCH
WORKDIR /build/src/Zilean.ApiService
RUN dotnet publish -c Release --no-restore -a $TARGETARCH -o /app/out
WORKDIR /build/src/Zilean.Scraper
RUN dotnet publish -c Release --no-restore -a $TARGETARCH -o /app/out

# Run Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0.18-alpine3.23
RUN apk update
RUN apk add --update --no-cache \
    python3=~3.12 \
    py3-pip=~25.1 \
    curl \
    git \
    icu-libs \
    tzdata \
    && ln -sf python3 /usr/bin/python
RUN addgroup -S zilean && adduser -S -G zilean zilean
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV PYTHONUNBUFFERED=1
ENV ZILEAN_PYTHON_PYLIB=/usr/lib/libpython3.12.so.1.0
ENV ASPNETCORE_URLS=http://+:8181

WORKDIR /app
COPY --from=base /app/out .
COPY --from=base /build/requirements.txt .
RUN rm -rf /app/python || true && \
    mkdir -p /app/python /app/data || true
RUN pip3 install -r /app/requirements.txt -t /app/python

RUN chown -R zilean:zilean /app
USER zilean

HEALTHCHECK --interval=30s --timeout=5s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8181/healthchecks/ready || exit 1

ENTRYPOINT ["./zilean-api"]
