FROM mcr.microsoft.com/dotnet/core/sdk:2.2-alpine AS build-env
WORKDIR /app

# Copy and build
COPY . ./
RUN pwd
RUN ls -la
RUN dotnet publish src/Nats.App/Nats.App.csproj -c Release -o /out 

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/runtime:2.2
WORKDIR /app
COPY --from=build-env /out .
ENTRYPOINT ["dotnet", "Nats.App.dll"]