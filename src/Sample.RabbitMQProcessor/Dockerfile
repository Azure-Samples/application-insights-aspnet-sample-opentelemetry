#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["src/Sample.Common/Sample.Common.csproj", "src/Sample.Common/"]
COPY ["src/Sample.RabbitMQCollector/Sample.RabbitMQCollector.csproj", "src/Sample.RabbitMQCollector/"]
COPY ["NuGet.config", "./"]
COPY ["src/Sample.RabbitMQProcessor/Sample.RabbitMQProcessor.csproj", "src/Sample.RabbitMQProcessor/"]
RUN dotnet restore "src/Sample.RabbitMQProcessor/Sample.RabbitMQProcessor.csproj"
COPY . .
WORKDIR "/src/src/Sample.RabbitMQProcessor"
RUN dotnet build "Sample.RabbitMQProcessor.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Sample.RabbitMQProcessor.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Sample.RabbitMQProcessor.dll"]