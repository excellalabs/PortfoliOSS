using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortfoliOSS.ModernData.Migrations
{
    public partial class LimitViewToThoseAddedToAnOrg : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
                ALTER VIEW PortfoliOSS as
                    select * from
                    (
                        select orgs.Name UserOrgName, users.UserId, users.Name UserName, prOrgs.Name OrgName, repos.Name RepoName, PullRequestId, CreatedOn, IsMerged, MergedDate 
                        from PullRequests prs
                        left outer join Users users on (prs.AuthorUserId = users.UserId)
                        left outer join Repositories repos on (prs.RepositoryRepoId = repos.RepoId)
                        left outer join Organizations prOrgs on (repos.OrganizationId = prOrgs.OrganizationId)
                        left outer join OrganizationUser orgusers on (users.UserId = orgusers.UsersUserId)
                        left outer join Organizations orgs on (orgusers.OrganizationsOrganizationId = orgs.OrganizationId)
                    )Query
                    WHERE Query.UserOrgName is not null
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
                ALTER VIEW PortfoliOSS as
                    {CreateQueryVIew.ORIGINAL_SQL}");
        }
    }
}
