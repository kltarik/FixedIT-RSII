FROM mcr.microsoft.com/dotnet/sdk:10.0.302 AS build
WORKDIR /src

COPY ["FixedIT.NotificationService/FixedIT.NotificationService.csproj", "FixedIT.NotificationService/"]
COPY ["FixedIT.Shared/FixedIT.Shared.csproj", "FixedIT.Shared/"]
RUN dotnet restore "FixedIT.NotificationService/FixedIT.NotificationService.csproj"

COPY . .
RUN cp "FixedIT.NotificationService/appsettings.json.example" "FixedIT.NotificationService/appsettings.json"
RUN dotnet publish "FixedIT.NotificationService/FixedIT.NotificationService.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0.10 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FixedIT.NotificationService.dll"]
