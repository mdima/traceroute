# See https://github.com/coverlet-coverage/coverlet/issues/1367

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
ENV TRACEROUTE_ENABLEREMOTETRACES=true
ENV TRACEROUTE_HOSTREMOTETRACES=true

RUN apt-get update \
    && apt-get install -y curl traceroute dnsutils

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
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

# FROM build AS testrunner
# WORKDIR "/UnitTests"
# RUN dotnet tool install dotnet-reportgenerator-globaltool --tool-path /dotnetglobaltools
# RUN apt-get update \
#     && apt-get install -y curl traceroute dnsutils
# LABEL unittestlayer=true
# RUN dotnet test -c $BUILD_CONFIGURATION --arch=x64
# RUN dotnet test --logger "trx;LogFileName=dockerunittestspiketestresults.xml" /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=/out/testresults/coverage/ /p:Exclude="[xunit.*]*" --results-directory /out/testresults -c $BUILD_CONFIGURATION /p:UseAppHost=false --arch=x64
# RUN /dotnetglobaltools/reportgenerator "-reports:/out/testresults/coverage/coverage.cobertura.xml" "-targetdir:/out/testresults/coverage/reports" "-reporttypes:HTMLInline;HTMLChart"

# FROM build AS testrunner
# WORKDIR "/UnitTests"
# RUN dotnet tool install dotnet-reportgenerator-globaltool --tool-path /dotnetglobaltools
# RUN dotnet test "UnitTests.dll" --collect:"XPlat Code Coverage" 
# RUN dotnet test -c Development --collect:"XPlat Code Coverage" /p:UseAppHost=false --no-restore
# ENTRYPOINT ["dotnet", "test", "UnitTests.dll", "--collect:XPlat Code Coverage", "--interactive"]