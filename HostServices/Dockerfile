#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:latest AS win
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/aspnet:7.0.5-bullseye-slim-arm64v8 AS arm
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["HostServices/HostServices.csproj", "HostServices/"]
COPY ["DiscordBot.Application/DiscordBot.Application.csproj", "DiscordBot.Application/"]
COPY ["Auth.Database/Auth.Database.csproj", "Auth.Database/"]
COPY ["Crosscutting/Crosscutting.csproj", "Crosscutting/"]
COPY ["DiscordSaga.Components/DiscordSaga.Components.csproj", "DiscordSaga.Components/"]
COPY ["DiscordBot.Infrastructure/DiscordBot.Infrastructure.csproj", "DiscordBot.Infrastructure/"]
COPY ["BoundBot/BoundBot.csproj", "BoundBot/"]
COPY ["BoundBot.Connection/BoundBot.Connection.csproj", "BoundBot.Connection/"]
RUN dotnet restore "HostServices/HostServices.csproj"
COPY . .
WORKDIR "/src/HostServices"
RUN dotnet build "HostServices.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "HostServices.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM win AS finalWin
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HostServices.dll"]

FROM arm AS finalArm
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HostServices.dll"]