FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS publish
WORKDIR /src

COPY ["src/App.Domain/App.Domain.csproj", "src/App.Domain/"]
COPY ["src/App.Application/App.Application.csproj", "src/App.Application/"]
COPY ["src/App.Infrastructure/App.Infrastructure.csproj", "src/App.Infrastructure/"]
COPY ["src/App.Web/App.Web.csproj", "src/App.Web/"]
COPY ["tests/App.Domain.UnitTests/App.Domain.UnitTests.csproj", "tests/App.Domain.UnitTests/"]
COPY ["tests/App.Application.UnitTests/App.Application.UnitTests.csproj", "tests/App.Application.UnitTests/"]
COPY ["tests/App.Infrastructure.UnitTests/App.Infrastructure.UnitTests.csproj", "tests/App.Infrastructure.UnitTests/"]
COPY ["App.sln", ""]

ARG DOTNET_RESTORE_CLI_ARGS=
RUN dotnet restore "App.sln" $DOTNET_RESTORE_CLI_ARGS

COPY . .
RUN dotnet build "App.sln" -c Release --no-restore

RUN dotnet publish -c Release --no-build -o /app "src/App.Web/App.Web.csproj"

FROM base AS final
WORKDIR /app
COPY --from=publish /app .

ARG BUILD_NUMBER=
ENV BUILD_NUMBER=$BUILD_NUMBER

ENTRYPOINT ["dotnet", "App.Web.dll"]