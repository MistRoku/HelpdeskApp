FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/HelpdeskAPI/HelpdeskAPI.csproj backend/HelpdeskAPI/
RUN dotnet restore backend/HelpdeskAPI/HelpdeskAPI.csproj
COPY . .
RUN dotnet publish backend/HelpdeskAPI/HelpdeskAPI.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "HelpdeskAPI.dll"]
