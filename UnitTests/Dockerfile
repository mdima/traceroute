FROM mcr.microsoft.com/dotnet/sdk:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
ENV ASPNETCORE_ENVIRONMENT=Development
ENV TRACEROUTE_ENABLEREMOTETRACES=true
ENV TRACEROUTE_HOSTREMOTETRACES=true

RUN apt-get update \
    && apt-get install -y curl traceroute dnsutils

### Add a new user "traceroute" with user id 8877
#RUN useradd -r traceroute
### I change the log folder ownership
#RUN mkdir logs && chown -R traceroute:traceroute logs
### Change to non-root user
#USER traceroute