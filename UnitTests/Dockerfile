FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

RUN apt-get update \
    && apt-get install -y curl traceroute dnsutils

# RUN echo "nameserver 8.8.8.8" > /etc/resolv.conf 