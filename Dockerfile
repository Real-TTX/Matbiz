FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Matbiz.Web/Matbiz.Web.csproj src/Matbiz.Web/
RUN dotnet restore src/Matbiz.Web/Matbiz.Web.csproj

COPY src/ src/
RUN dotnet publish src/Matbiz.Web/Matbiz.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Matbiz.Web.dll"]
