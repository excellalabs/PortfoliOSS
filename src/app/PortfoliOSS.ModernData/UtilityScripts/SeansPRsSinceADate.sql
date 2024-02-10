Select orgs.Name, repos.Name, MergedDate from PullRequests prs
    left outer join Repositories repos on (prs.RepositoryRepoId = repos.RepoId)
    left outer join Organizations orgs on (repos.OrganizationId = orgs.OrganizationId)
    where AuthorUserId = (Select UserId from Users where Name ='SeanKilleen') 
    and orgs.Name != 'SeanKilleen'
    and IsMerged = 1
    and MergedDate >= '2023-03-01'
    order by MergedDate