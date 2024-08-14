FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ShippingService/ShippingService.csproj", "ShippingService/"]
COPY ["ShippingService.PublicApi/ShippingService.PublicApi.csproj", "ShippingService.PublicApi/"]
COPY ["ShippingService.Shared/ShippingService.Shared.csproj", "ShippingService.Shared/"]
RUN dotnet restore "ShippingService/ShippingService.csproj"
COPY . .
WORKDIR "/src/ShippingService"
RUN dotnet build "ShippingService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ShippingService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ShippingService.dll"]
