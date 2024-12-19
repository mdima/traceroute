FROM mcr.microsoft.com/dotnet/sdk:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
ENV TRACEROUTE_ENABLEREMOTETRACES=true
ENV TRACEROUTE_HOSTREMOTETRACES=true

RUN apt-get update \
    && apt-get install -y curl traceroute dnsutils

RUN wget https://aka.ms/getvsdbgsh && sh getvsdbgsh -v latest -l /vsdbg
# RUN echo "nameserver 8.8.8.8" > /etc/resolv.conf 