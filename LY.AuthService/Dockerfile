FROM mcr.microsoft.com/dotnet/core/aspnet:3.0-buster-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-buster AS build
WORKDIR /src
COPY ["LY.AuthService/LY.AuthService.csproj", "LY.AuthService/"]
RUN dotnet restore "LY.AuthService/LY.AuthService.csproj"
COPY . .
WORKDIR "/src/LY.AuthService"
RUN dotnet build "LY.AuthService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LY.AuthService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LY.AuthService.dll"]