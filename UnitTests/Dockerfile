FROM mcr.microsoft.com/dotnet/sdk:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
ENV ASPNETCORE_ENVIRONMENT=Development
ENV TRACEROUTE_ENABLEREMOTETRACES=true
ENV TRACEROUTE_HOSTREMOTETRACES=true

RUN apt-get update \
    && apt-get install -y curl traceroute dnsutils