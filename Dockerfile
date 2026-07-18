# Use ASP.NET Core 5.0 Runtime
FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:80

# Use SDK to compile
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["Quanan.csproj", "./"]
RUN dotnet restore "./Quanan.csproj"
COPY . .
RUN dotnet build "Quanan.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Quanan.csproj" -c Release -o /app/publish

# Copy publish files to final runtime
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Quanan.dll"]
