FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim-arm32v7 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster-arm32v7 AS build
WORKDIR /src
COPY ["/BlazorApp.Server/BlazorApp.Server.csproj", "BlazorApp.Server/"]
COPY ["/BlazorApp.Shared/BlazorApp.Shared.csproj", "BlazorApp.Shared/"]
COPY ["/BlazorApp.Client/BlazorApp.Client.csproj", "BlazorApp.Client/"]
COPY . .
WORKDIR "/src/BlazorApp.Server"
RUN dotnet build "BlazorApp.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BlazorApp.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BlazorApp.Server.dll"]
