ARG BUILD_FROM
ARG config=Release

FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine3.16 AS build
WORKDIR /src
COPY ["CarrotHome.Mqtt/CarrotHome.Mqtt.csproj", "/src/CarrotHome.Mqtt/"]
RUN dotnet restore CarrotHome.Mqtt/CarrotHome.Mqtt.csproj
COPY . .
WORKDIR /src/CarrotHome.Mqtt
ARG config
RUN dotnet build CarrotHome.Mqtt.csproj -c $config -o /dist

FROM $BUILD_FROM as final

# Copy data for add-on
COPY docker-entrypoint.sh /
RUN apk add aspnetcore6-runtime
COPY --from=build /dist /app
CMD [ "/docker-entrypoint.sh" ]