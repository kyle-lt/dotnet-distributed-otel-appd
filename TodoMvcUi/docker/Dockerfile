FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS base

WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /src
COPY . .

WORKDIR /src/TodoMvcUi
RUN dotnet restore
RUN dotnet build --no-restore -c Release -o /app

FROM build AS publish
RUN dotnet publish --no-restore -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .

# Install AppD Install Script Dependencies
RUN apt-get update
RUN apt-get install -y jq
RUN apt-get install -y unzip

# Copy AppD agent binaries
#RUN mkdir -p /opt/AppDynamics/dotnet
COPY downloadDotNetLinuxAgentLatest.sh .
RUN ./downloadDotNetLinuxAgentLatest.sh 
#ADD libappdprofiler.so /opt/AppDynamics/dotnet/libappdprofiler.so
#ADD AppDynamics.Agent.netstandard.dll /opt/AppDynamics/dotnet/AppDynamics.Agent.netstandard.dll

# set profiler environment variables
ENV CORECLR_PROFILER={57e1aa68-2229-41aa-9931-a6e93bbc64d8}
ENV CORECLR_ENABLE_PROFILING=1
ENV CORECLR_PROFILER_PATH=/app/libappdprofiler.so

# Set Hosting Environment to Development
ENV ASPNETCORE_ENVIRONMENT=Development
# Set listening port and wildcare hostname
ENV ASPNETCORE_URLS=http://*:60000

EXPOSE 60000

# Changed the startup command to set the AppD Node Name dynamically using the Container ID, AKA, $HOSTNAME of container
#CMD export APPDYNAMICS_AGENT_NODE_NAME=dotnetcore-linux-BethanysPieShop-node-$HOSTNAME && dotnet BethanysPieShop.dll
ENTRYPOINT ["dotnet", "TodoMvcUi.dll"]
