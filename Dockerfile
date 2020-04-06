FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine as build-ENV
COPY ./Operator ./app/Operator
COPY ./Controller ./app/Controller
WORKDIR /app/Operator
RUN dotnet publish -c Release -r linux-musl-x64 -o publish-folder

FROM mcr.microsoft.com/dotnet/core/runtime-deps:3.1-alpine as runtime
COPY --from=build-ENV /app/Operator/publish-folder ./app

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true
WORKDIR /app
ENTRYPOINT [ "./Operator" ]