#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Miya.NET/Miya.NET.csproj", "src/Miya.NET/"]
COPY ["src/MatrixBot.Core/MatrixBot.Core.csproj", "src/MatrixBot.Core/"]
RUN dotnet restore "./src/Miya.NET/Miya.NET.csproj"
COPY . .
WORKDIR "/src/src/Miya.NET"
RUN dotnet build "./Miya.NET.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Miya.NET.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Miya.NET.dll"]