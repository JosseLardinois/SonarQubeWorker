# Base image for runtime
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 8080

# Build image
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Install SonarScanner and Java in the build stage
RUN apt-get update && \
    apt-get install -y openjdk-11-jre-headless && \
    dotnet tool install --global dotnet-sonarscanner

# Set the path to include dotnet global tools
ENV PATH="${PATH}:/root/.dotnet/tools"

# Copy csproj and restore as distinct layers
COPY ["SonarQubeWorker/SonarQubeWorker.csproj", "SonarQubeWorker/"]
RUN dotnet restore "SonarQubeWorker/SonarQubeWorker.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/SonarQubeWorker"
RUN dotnet build "SonarQubeWorker.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "SonarQubeWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS final
WORKDIR /app

# Install Java in the final stage
RUN apt-get update && \
    apt-get install -y openjdk-11-jre-headless

# Find Java installation path and set JAVA_HOME
RUN JAVA_HOME_DIR=$(dirname $(dirname $(readlink -f $(which java)))) && \
    echo "JAVA_HOME=$JAVA_HOME_DIR" >> /etc/environment && \
    export JAVA_HOME=$JAVA_HOME_DIR

# Set the path to include dotnet global tools and Java
ENV PATH="${PATH}:/root/.dotnet/tools:${JAVA_HOME}/bin"

# Copy the global tools directory from the build stage
COPY --from=build /root/.dotnet /root/.dotnet

# Copy the published application
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "SonarQubeWorker.dll"]