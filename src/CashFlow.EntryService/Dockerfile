FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY CashFlow.SharedKernel/CashFlow.SharedKernel.csproj CashFlow.SharedKernel/
COPY CashFlow.EntryService/CashFlow.EntryService.csproj CashFlow.EntryService/
RUN dotnet restore CashFlow.EntryService/CashFlow.EntryService.csproj
COPY CashFlow.SharedKernel/ CashFlow.SharedKernel/
COPY CashFlow.EntryService/ CashFlow.EntryService/
RUN dotnet publish CashFlow.EntryService/CashFlow.EntryService.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CashFlow.EntryService.dll"]
