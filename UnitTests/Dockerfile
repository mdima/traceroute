# See https://github.com/coverlet-coverage/coverlet/issues/1367

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
ENV TRACEROUTE_ENABLEREMOTETRACES=true
ENV TRACEROUTE_HOSTREMOTETRACES=true

RUN apt-get update \
    && apt-get install -y curl traceroute dnsutils

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Development
COPY ["TraceRoute/TraceRoute.csproj", "TraceRoute/"]
COPY . .
COPY ["UnitTests/UnitTests.csproj", "UnitTests/"]
COPY . .

WORKDIR "/UnitTests"
RUN dotnet restore "UnitTests.csproj"

FROM build AS publish
WORKDIR "/UnitTests"
RUN dotnet publish "UnitTests.csproj" -c $BUILD_CONFIGURATION -o /tests/publish /p:UseAppHost=false --no-restore

FROM base AS final
WORKDIR /tests
COPY --from=publish /tests/publish .
#RUN dotnet test "UnitTests.dll" --collect:"XPlat Code Coverage" 
# ENTRYPOINT ["dotnet", "test", "UnitTests.dll", "--collect:XPlat Code Coverage", "--interactive"]