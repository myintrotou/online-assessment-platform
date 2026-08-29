# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY AssessmentPlatform.sln ./
COPY AssessmentPlatform.Api/AssessmentPlatform.Api.csproj AssessmentPlatform.Api/
RUN dotnet restore AssessmentPlatform.Api/AssessmentPlatform.Api.csproj

COPY . .
RUN dotnet publish AssessmentPlatform.Api/AssessmentPlatform.Api.csproj -c Release -o /app --no-restore

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

# Render (and most PaaS) inject PORT; bind to it, default to 8080 locally.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "AssessmentPlatform.Api.dll"]
