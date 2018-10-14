FROM microsoft/dotnet:2.1-sdk as build

WORKDIR /app
COPY src ./src
COPY fs-graphql-example.sln ./
RUN dotnet restore

WORKDIR /app/src/Cli
RUN dotnet publish -c Release -o /app/out/

FROM microsoft/dotnet:2.1-runtime

RUN apt-get install -y tzdata

WORKDIR /app/out
COPY --from=build /app/out ./
RUN ls -l /app/out

CMD [ "dotnet", "Cli.dll" ]