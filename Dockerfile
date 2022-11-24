FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["AniviaWeb2/AniviaWeb2.csproj", "AniviaWeb2/"]
RUN dotnet restore "AniviaWeb2/AniviaWeb2.csproj"
COPY . .
COPY ["AniviaWeb2/appsettings.Development.json", "AniviaWeb2/appsettings.json"]
WORKDIR "/src/AniviaWeb2"
RUN dotnet build "AniviaWeb2.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AniviaWeb2.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AniviaWeb2.dll"]
