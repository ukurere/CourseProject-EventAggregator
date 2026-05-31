# ── Stage 1: Build Angular ────────────────────────────────────────────────
FROM node:20-alpine AS angular-build
WORKDIR /client
COPY event-aggregator-client/package*.json ./
RUN npm ci --legacy-peer-deps
COPY event-aggregator-client/ ./
RUN npm run build -- --configuration production

# ── Stage 2: Build .NET ───────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS dotnet-build
WORKDIR /src
COPY EventAggregator/ ./
# Copy Angular build output into wwwroot
COPY --from=angular-build /client/dist/event-aggregator-client/browser ./wwwroot
RUN dotnet publish -c Release -o /app/publish

# ── Stage 3: Runtime ──────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=dotnet-build /app/publish ./

# SQLite db lives in a persistent volume mounted at /data
ENV DB_PATH=/data/events.db
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "EventAggregator.dll"]
