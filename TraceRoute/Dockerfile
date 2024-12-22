FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

RUN apt-get update \
    && apt-get install -y curl traceroute dnsutils 

## Add a new user "traceroute" with user id 8877
RUN useradd -r traceroute
## I change the log folder ownership
RUN mkdir logs && chown -R traceroute:traceroute logs
## Change to non-root user
USER traceroute

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["TraceRoute/TraceRoute.csproj", "TraceRoute/"]
RUN dotnet restore "./TraceRoute/TraceRoute.csproj"
COPY . .
WORKDIR "/src/TraceRoute"
RUN dotnet build "./TraceRoute.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TraceRoute.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:80
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TraceRoute.dll"]