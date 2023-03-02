FROM mcr.microsoft.com/dotnet/sdk:7.0-bullseye-slim-amd64 AS build

# https://github.com/dotnet/sdk/issues/28971
ARG TARGETARCH
ARG TARGETOS
RUN arch=$TARGETARCH \
    && if [ "$arch" = "amd64" ]; then arch="x64"; fi \
    && echo $TARGETOS-$arch > /tmp/rid

WORKDIR /App
COPY . ./
RUN dotnet restore -r $(cat /tmp/rid)
RUN dotnet publish -c Release -o out -r $(cat /tmp/rid) --self-contained false --no-restore

FROM mcr.microsoft.com/dotnet/runtime:7.0
WORKDIR /App
COPY --from=build /App/out .
ENTRYPOINT ["dotnet", "Corner.Failsafe.dll"]