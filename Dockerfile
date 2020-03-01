FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /
COPY ["/Dauer.BlazorApp.Server/Dauer.BlazorApp.Server.csproj", "Dauer.BlazorApp.Server/"]
COPY ["/Dauer.BlazorApp.Shared/Dauer.BlazorApp.Shared.csproj", "Dauer.BlazorApp.Shared/"]
COPY ["/Dauer.BlazorApp.Client/Dauer.BlazorApp.Client.csproj", "Dauer.BlazorApp.Client/"]
COPY ["/Dauer.Data/Dauer.Data.csproj", "Dauer.Data/"]

COPY . .
WORKDIR "/Dauer.BlazorApp.Server"
RUN dotnet build "Dauer.BlazorApp.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Dauer.BlazorApp.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Dauer.BlazorApp.Server.dll"]
