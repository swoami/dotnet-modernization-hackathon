# syntax=docker/dockerfile:1
#
# Build context MUST be the repository root (docker build -f src/ContosoInsurance/ContosoInsurance.Web/Dockerfile .)
# because this project references sibling projects (Common, Data, Services).

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/ContosoInsurance/ContosoInsurance.Web/ContosoInsurance.Web.csproj", "ContosoInsurance.Web/"]
COPY ["src/ContosoInsurance/ContosoInsurance.Services/ContosoInsurance.Services.csproj", "ContosoInsurance.Services/"]
COPY ["src/ContosoInsurance/ContosoInsurance.Data/ContosoInsurance.Data.csproj", "ContosoInsurance.Data/"]
COPY ["src/ContosoInsurance/ContosoInsurance.Common/ContosoInsurance.Common.csproj", "ContosoInsurance.Common/"]
RUN dotnet restore "ContosoInsurance.Web/ContosoInsurance.Web.csproj"

COPY ["src/ContosoInsurance/ContosoInsurance.Web/", "ContosoInsurance.Web/"]
COPY ["src/ContosoInsurance/ContosoInsurance.Services/", "ContosoInsurance.Services/"]
COPY ["src/ContosoInsurance/ContosoInsurance.Data/", "ContosoInsurance.Data/"]
COPY ["src/ContosoInsurance/ContosoInsurance.Common/", "ContosoInsurance.Common/"]

WORKDIR /src/ContosoInsurance.Web
RUN dotnet publish "ContosoInsurance.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
USER app
ENTRYPOINT ["dotnet", "ContosoInsurance.Web.dll"]
