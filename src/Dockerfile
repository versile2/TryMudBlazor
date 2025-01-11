# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy project files
COPY ./TryMudBlazor.Server/TryMudBlazor.Server.csproj ./TryMudBlazor.Server/
COPY ./MudBlazor/MudBlazor.csproj ./MudBlazor/
COPY ./MudBlazor.Analyzers/MudBlazor.Analyzers.csproj ./MudBlazor.Analyzers/
COPY ./MudBlazor.SourceGenerator/MudBlazor.SourceGenerator.csproj ./MudBlazor.SourceGenerator/
COPY ./MudBlazor.Examples.Data/MudBlazor.Examples.Data.csproj ./MudBlazor.Examples.Data/
COPY ./TryMudBlazor.Client/TryMudBlazor.Client.csproj ./TryMudBlazor.Client/
COPY ./Try.Core/Try.Core.csproj ./Try.Core/
COPY ./UserComponents/Try.UserComponents.csproj ./UserComponents/
COPY nuget.config ./

# Copy all source code
COPY . ./

# Publish both Client and Server
RUN dotnet publish ./TryMudBlazor.Client/TryMudBlazor.Client.csproj -c Release -o /app/client-publish
RUN dotnet publish ./TryMudBlazor.Server/TryMudBlazor.Server.csproj -c Release -o /app/server-publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Copy both published outputs
COPY --from=build /app/server-publish .
COPY --from=build /app/client-publish/wwwroot ./wwwroot

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

CMD ["dotnet", "TryMudBlazor.Server.dll"]

#              *** Don't forget to update appsettings.json with your own connection string ***

# docker build -t trymudblazorserver .
# docker tag trymudblazorserver:latest amyraymoving/trymudblazorserver:latest
# docker push amyraymoving/trymudblazorserver:latest

# docker run --name trymudblazorserver -d -p 5002:8080 trymudblazorserver