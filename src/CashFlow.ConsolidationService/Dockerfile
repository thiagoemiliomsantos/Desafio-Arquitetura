FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY CashFlow.SharedKernel/CashFlow.SharedKernel.csproj CashFlow.SharedKernel/
COPY CashFlow.ConsolidationService/CashFlow.ConsolidationService.csproj CashFlow.ConsolidationService/
RUN dotnet restore CashFlow.ConsolidationService/CashFlow.ConsolidationService.csproj
COPY CashFlow.SharedKernel/ CashFlow.SharedKernel/
COPY CashFlow.ConsolidationService/ CashFlow.ConsolidationService/
RUN dotnet publish CashFlow.ConsolidationService/CashFlow.ConsolidationService.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CashFlow.ConsolidationService.dll"]
