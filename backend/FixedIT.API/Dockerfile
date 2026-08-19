FROM mcr.microsoft.com/dotnet/sdk:10.0.302 AS build
WORKDIR /src

COPY ["FixedIT.API/FixedIT.API.csproj", "FixedIT.API/"]
COPY ["FixedIT.Shared/FixedIT.Shared.csproj", "FixedIT.Shared/"]
RUN dotnet restore "FixedIT.API/FixedIT.API.csproj"

COPY . .
RUN cp "FixedIT.API/appsettings.json.example" "FixedIT.API/appsettings.json"
RUN dotnet publish "FixedIT.API/FixedIT.API.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0.10 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FixedIT.API.dll"]
