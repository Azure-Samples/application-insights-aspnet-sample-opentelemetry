#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["src/Sample.Common/Sample.Common.csproj", "src/Sample.Common/"]
COPY ["src/Sample.RabbitMQCollector/Sample.RabbitMQCollector.csproj", "src/Sample.RabbitMQCollector/"]
COPY ["NuGet.config", "./"]
COPY ["src/Sample.MainApi/Sample.MainApi.csproj", "src/Sample.MainApi/"]

RUN dotnet restore "src/Sample.MainApi/Sample.MainApi.csproj"
COPY . .
WORKDIR "/src/src/Sample.MainApi"
RUN dotnet build "Sample.MainApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Sample.MainApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Sample.MainApi.dll"]