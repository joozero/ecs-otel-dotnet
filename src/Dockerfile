FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /source

COPY webapp/*.csproj ./webapp/
RUN dotnet restore webapp

# copy everything else and build app
COPY webapp/. ./webapp/
WORKDIR /source/webapp
RUN dotnet publish -c release -o /app --self-contained false --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine
WORKDIR /app
COPY --from=build /app ./
EXPOSE 80
ENTRYPOINT ["dotnet", "integration-test-app.dll"]