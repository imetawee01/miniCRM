FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Crm.sln ./
COPY src/Crm.Domain/Crm.Domain.csproj src/Crm.Domain/
COPY src/Crm.Application/Crm.Application.csproj src/Crm.Application/
COPY src/Crm.Infrastructure/Crm.Infrastructure.csproj src/Crm.Infrastructure/
COPY src/Crm.Api/Crm.Api.csproj src/Crm.Api/
RUN dotnet restore src/Crm.Api/Crm.Api.csproj
COPY . .
RUN dotnet publish src/Crm.Api/Crm.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Crm.Api.dll"]
