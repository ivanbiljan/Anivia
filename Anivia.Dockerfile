FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Anivia/Anivia.csproj", "Anivia/"]
RUN dotnet restore "Anivia/Anivia.csproj"
COPY . .
COPY ["Anivia/appsettings.Development.json", "Anivia/appsettings.json"]
WORKDIR "/src/Anivia"
RUN dotnet build "Anivia.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Anivia.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Anivia.dll"]
