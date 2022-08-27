FROM mcr.microsoft.com/dotnet/runtime:6.0-alpine3.16 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine3.16 AS build
WORKDIR /src
COPY ["CarrotHome.Mqtt/CarrotHome.Mqtt.csproj", "CarrotHome.Mqtt/"]
RUN dotnet restore "CarrotHome.Mqtt/CarrotHome.Mqtt.csproj"
COPY . .
WORKDIR "/src/CarrotHome.Mqtt"
RUN dotnet build "CarrotHome.Mqtt.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CarrotHome.Mqtt.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CarrotHome.Mqtt.dll"]
