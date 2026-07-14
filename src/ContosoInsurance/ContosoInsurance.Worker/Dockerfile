# syntax=docker/dockerfile:1
#
# Build context MUST be the repository root (docker build -f src/ContosoInsurance/ContosoInsurance.Worker/Dockerfile .)
# because this project references sibling projects (Common, Data).
# No ASP.NET base image: the Worker is a BackgroundService host, not a web app.

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/ContosoInsurance/ContosoInsurance.Worker/ContosoInsurance.Worker.csproj", "ContosoInsurance.Worker/"]
COPY ["src/ContosoInsurance/ContosoInsurance.Data/ContosoInsurance.Data.csproj", "ContosoInsurance.Data/"]
COPY ["src/ContosoInsurance/ContosoInsurance.Common/ContosoInsurance.Common.csproj", "ContosoInsurance.Common/"]
RUN dotnet restore "ContosoInsurance.Worker/ContosoInsurance.Worker.csproj"

COPY ["src/ContosoInsurance/ContosoInsurance.Worker/", "ContosoInsurance.Worker/"]
COPY ["src/ContosoInsurance/ContosoInsurance.Data/", "ContosoInsurance.Data/"]
COPY ["src/ContosoInsurance/ContosoInsurance.Common/", "ContosoInsurance.Common/"]

WORKDIR /src/ContosoInsurance.Worker
RUN dotnet publish "ContosoInsurance.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
USER app
ENTRYPOINT ["dotnet", "ContosoInsurance.Worker.dll"]
