FROM node:22-bookworm-slim AS client-build

WORKDIR /src/WEB/Client

COPY WEB/Client/package.json WEB/Client/package-lock.json ./
RUN npm ci

COPY WEB/Client/ ./
COPY WEB/Manual/ /src/WEB/Manual/
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0-bookworm-slim AS server-build

WORKDIR /src

COPY WEB/Server/DigitC2.Server.csproj WEB/Server/
COPY DESKTOP/Engine/Engine.csproj DESKTOP/Engine/
RUN dotnet restore WEB/Server/DigitC2.Server.csproj

COPY WEB/Server/ WEB/Server/
COPY DESKTOP/Engine/ DESKTOP/Engine/
COPY DESKTOP/Transgraphier/Input/Config.txt DESKTOP/Transgraphier/Input/Config.txt
COPY --from=client-build /src/WEB/Client/dist/ WEB/Server/wwwroot/

RUN dotnet publish WEB/Server/DigitC2.Server.csproj \
  --configuration Release \
  --no-restore \
  --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-bookworm-slim AS final

WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 \
  ASPNETCORE_ENVIRONMENT=Production \
  DigitC2__WorkspaceRoot=/data

RUN mkdir /data \
  && chown -R "$APP_UID:$APP_UID" /app /data

COPY --from=server-build --chown=$APP_UID:$APP_UID /app/publish/ ./

USER $APP_UID

EXPOSE 8080

ENTRYPOINT ["dotnet", "DigitC2.Server.dll"]
