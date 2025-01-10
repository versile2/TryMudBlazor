# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy the solution file and the specific project file for TryMudBlazor.Client
COPY ./TryMudBlazor.Server/TryMudBlazor.Server.csproj ./TryMudBlazor.Server/

# Copy dependencies for TryMudBlazor.Client
COPY ./MudBlazor/MudBlazor.csproj ./MudBlazor/
COPY ./MudBlazor.Analyzers/MudBlazor.Analyzers.csproj ./MudBlazor.Analyzers/
COPY ./MudBlazor.SourceGenerator/MudBlazor.SourceGenerator.csproj ./MudBlazor.SourceGenerator/
COPY ./MudBlazor.Examples.Data/MudBlazor.Examples.Data.csproj ./MudBlazor.Examples.Data/
COPY ./TryMudBlazor.Client/TryMudBlazor.Client.csproj ./TryMudBlazor.Client/
COPY ./Try.Core/Try.Core.csproj ./Try.Core/
COPY ./UserComponents/Try.UserComponents.csproj ./UserComponents/
COPY nuget.config ./

# Restore only for TryMudBlazor.Client
RUN dotnet restore ./TryMudBlazor.Server/TryMudBlazor.Server.csproj
RUN dotnet restore ./TryMudBlazor.Client/TryMudBlazor.Client.csproj

# Copy the rest of the application source code
COPY . ./

# Publish the TryMudBlazor.Client project
RUN dotnet publish ./TryMudBlazor.Client/TryMudBlazor.Client.csproj -c Release -o out -f net9.0
RUN dotnet publish ./TryMudBlazor.Server/TryMudBlazor.Server.csproj -c Release -o outserver -f net9.0

# Stage 2: Serve with NGINX
FROM nginx:1.23.0-alpine
WORKDIR /app
EXPOSE 8080

# Copy the custom nginx configuration
COPY nginx.conf /etc/nginx/nginx.conf

# Copy the published output to NGINX's html directory
COPY --from=build /app/out/wwwroot /usr/share/nginx/html
COPY --from=build /app/outserver /app/server

ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

# Set up entrypoint to start both NGINX and the server
# CMD /app/server/TryMudBlazor.Server & nginx -g "daemon off;"
CMD dotnet /app/server/TryMudBlazor.Server.dll --urls "http://+:5000" & nginx -g "daemon off;"

# docker build -t trymudblazorserver .
# docker run --name trymudblazorserver -d -p 5002:8080 trymudblazorserver
# docker tag trymudblazorserver:latest amyraymoving/trymudblazorserver:latest
# docker login -u amyraymoving -p <password>exit
# docker push amyraymoving/customtryblazor:latest