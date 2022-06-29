-- PURPOSE: Show the counts across various items in one succinct table

DECLARE @ViewCount int
DECLARE @Orgs INT
DECLARE @Repos INT
DECLARE @Users INT
DECLARE @PRs INT
DECLARE @TotalEventsPersisted INT

SELECT @ViewCount = count(*) from PortfoliOSS
SELECT @Orgs = count(*) from Organizations
SELECT @Repos = count(*) from Repositories
SELECT @Users = count(*) from Users
SELECT @PRs = count(*) from PullRequests
SELECT @TotalEventsPersisted = count(*) from EventJournal

SELECT @ViewCount as ViewCount, @Orgs as Orgs, @Repos as Repos, @Users as Users, @PRs as PRs, @TotalEventsPersisted as TotalEventsPersisted


