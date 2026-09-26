FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/EnglishLearning.slnx backend/
COPY backend/src/EnglishLearning.Api/EnglishLearning.Api.csproj backend/src/EnglishLearning.Api/
COPY backend/src/EnglishLearning.Application/EnglishLearning.Application.csproj backend/src/EnglishLearning.Application/
COPY backend/src/EnglishLearning.Domain/EnglishLearning.Domain.csproj backend/src/EnglishLearning.Domain/
COPY backend/src/EnglishLearning.Infrastructure/EnglishLearning.Infrastructure.csproj backend/src/EnglishLearning.Infrastructure/
RUN dotnet restore backend/src/EnglishLearning.Api/EnglishLearning.Api.csproj
COPY backend/src backend/src
RUN dotnet publish backend/src/EnglishLearning.Api/EnglishLearning.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
USER app
ENTRYPOINT ["dotnet", "EnglishLearning.Api.dll"]
