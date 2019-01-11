FROM microsoft/dotnet:2.2-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.2-sdk AS build
WORKDIR /src
COPY EventhubToInfluxDB/EventhubToInfluxDB.csproj EventhubToInfluxDB/
RUN dotnet restore EventhubToInfluxDB/EventhubToInfluxDB.csproj
COPY . .
WORKDIR /src/EventhubToInfluxDB
RUN dotnet build EventhubToInfluxDB.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish EventhubToInfluxDB.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "EventhubToInfluxDB.dll"]
