ARG BUILD_FROM
ARG config=Release

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG TARGETARCH
WORKDIR /src
COPY ["CarrotHome.Mqtt/CarrotHome.Mqtt.csproj", "/src/CarrotHome.Mqtt/"]
RUN dotnet restore CarrotHome.Mqtt/CarrotHome.Mqtt.csproj
COPY . .
WORKDIR /src/CarrotHome.Mqtt
ARG config
RUN dotnet build CarrotHome.Mqtt.csproj  \
    --self-contained true \
    --arch $TARGETARCH  \
    -c $config  \
    -o /dist

FROM --platform=$TARGETPLATFORM ghcr.io/hassio-addons/base:15.0.7 as final

# Copy data for add-on
COPY docker-entrypoint.sh /
COPY --from=build /dist /app
CMD [ "/docker-entrypoint.sh" ]