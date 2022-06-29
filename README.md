# PortfoliOSS

A way to catalog your org's contributions to open source.

## Introduction and Notes

### On the Current Approach and Future Vision

This project helps answer an interesting question ("Who in our GitHub orgs contributes PRs to OSS projects?"). It currently solves it in a rudimentary way:

* It's a console application
* It uses a persistent stateful actor model to crawl GitHub (based on user-supplied token(s))
* It persists events to a SQL server (in our current use case, SQL Server via Docker)
* It replays those events into a view model (more tables in SQL Server and a view for reporting)
* It uses PowerBI to pull from those tables and show results
* It logs along the way (to Seq via Serilog)
  * Fun fact: this was actually how the first version worked entirely. We scraped the log messages to an Excel file and generated a chart.

I'd love to get this to a place where we could publish a shiny front-end supported by the back-end running as a cluster and have things continually updated. I'd love even more to figure out a way to host this for many organizations to participate in, perhaps for a small fee to cover the hosting costs where all participating orgs pay an equal or proportionate version. But right now, let's be honest; this is a console app. It has a ways to go on that front.

### On the Current State

A personal note from @SeanKilleen:

Some of this is...a little more rough than I'd normally publish. But, with limited time and resources, it was too easy to let the tool sit in a private repo and allow perfect to be the enemy of the good. This tool is solving a problem and it had been sitting too long in a private repo where only I could run it and improve it. It's time to put it out into the light, which I hope will drive more contributions and improvements.

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
