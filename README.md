# PortfoliOSS

A way to show off your org's contributions to open source.

## Containers

SQL DB for persistence:

```cmd
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=yourStrong(!)Password" -p 1433:1433 --name portfolioss -d mcr.microsoft.com/mssql/server:2019-latest
```

Seq for logging:

```cmd
docker run -p:5342:80 -p 5341:5341 -e "ACCEPT_EULA=Y" --name seq -d datalust/seq:latest
```

## Create DB and Run Migrations within SQL Server

By default, the SQL Server container creates a `master` database. Probably best we don't operate in there though. We'll run the ef core migrations and set up our table structure in a `portfolioss` database.

* Open a command prompt
* Head to the `src/app/PortfoliOSS.ModernData` directory
* Run the migrations against the SQL server on localhost (your container) by running `dotnet ef database update`.

At this point, you'll have the migrations applied for our table structure, but Akka will still need to create its event sourcing tables (which it will do because it has the SA password to the container at this point.)
