FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["BrackeysBot/BrackeysBot.csproj", "BrackeysBot/"]
RUN dotnet restore "BrackeysBot/BrackeysBot.csproj"
COPY . .
WORKDIR "/src/BrackeysBot"
RUN dotnet build "BrackeysBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BrackeysBot.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BrackeysBot.dll"]
