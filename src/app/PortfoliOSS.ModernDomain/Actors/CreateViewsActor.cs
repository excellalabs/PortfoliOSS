using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Akka.Persistence.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Octokit;
using PortfoliOSS.ModernData;
using PortfoliOSS.ModernDomain.Actors.Persistent;
using PortfoliOSS.ModernDomain.Events;
using Organization = PortfoliOSS.ModernData.Organization;
using PullRequest = PortfoliOSS.ModernData.PullRequest;
using Repository = PortfoliOSS.ModernData.Repository;
using User = PortfoliOSS.ModernData.User;

namespace PortfoliOSS.ModernDomain.Actors
{
    public class CreateViewsActor : ReceiveActor
    {
        private ILoggingAdapter _logger;
        private readonly IDbContextFactory<PortfoliOSSDBContext> _contextFactory;
        public CreateViewsActor(IDbContextFactory<PortfoliOSSDBContext> contextFactory)
        {
            _contextFactory = contextFactory;
            _logger = Context.GetLogger();
            var capturedEvents = 0;

            ReceiveAsync<EventEnvelope>(async message =>
            {
                var ctx = await _contextFactory.CreateDbContextAsync();
                capturedEvents++;
                _logger.Warning("RECEIVED EVENT # {EventNumber} in my processing. PersistenceId {PersistenceId} Sequence number {SequenceNumber}", capturedEvents, message.PersistenceId, message.SequenceNr);
                switch (message.Event)
                {
                    case OrgAddedEvent evt:
                        await Apply(evt, ctx);
                        break;
                    case UserAddedEvent evt:
                        await Apply(evt, ctx);
                        break;
                    case OrgAddedForUserEvent evt:
                        await Apply(evt, ctx);
                        break;
                    case PrAddedEvent evt:
                        await Apply(evt, ctx);
                        break;
                    case ForkRepoAddedEvent evt:
                        await Apply(evt, ctx);
                        break;
                    case SourceRepoAddedEvent evt:
                        await Apply(evt, ctx);
                        break;
                }
            });

            _logger.Info("View writer created");
        }

        private async Task Apply(ForkRepoAddedEvent evt, PortfoliOSSDBContext ctx)
        {
            if (ctx.Repositories.Any(x => x.RepoId == evt.RepoId))
            {
                return;
            }
            var orgToAssociate = ctx.Organizations.FirstOrDefault(x => x.OrganizationId == evt.OrgId);
            if (orgToAssociate == null)
            {
                orgToAssociate = new Organization()
                    { Name = evt.OrganizationName, OrganizationId = evt.OrgId };
                ctx.Organizations.Add(orgToAssociate);
            }
            var addedForkRepo = new Repository
            {
                RepoId = evt.RepoId,
                Name = evt.RepoName,
            };
            orgToAssociate.Repositories.Add(addedForkRepo);
            ctx.Add(addedForkRepo);
            await ctx.SaveChangesAsync();
        }

        private async Task Apply(PrAddedEvent evt, PortfoliOSSDBContext ctx)
        {
            if (ctx.PullRequests.Any(x =>
                    x.PullRequestId == evt.PullRequestId
                    && x.Repository.RepoId == evt.RepoId))
            {
                return;
            }
            var author = ctx.Users.FirstOrDefault(x => x.UserId == evt.AuthorId);
            var repo = ctx.Repositories.FirstOrDefault(x => x.RepoId == evt.RepoId);
            if (author == null)
            {
                _logger.Warning("PrAddedEvent: Author was null. Creating. {PRId}, {RepoFullName}", evt.PullRequestId, evt.RepoFullName);
                author = new User() { Name = evt.AuthorLogin, UserId = evt.AuthorId };
                ctx.Users.Add(author);
            }
            if (repo == null)
            {
                _logger.Warning("PrAddedEvent: Repo was null. Creating. {PRId}, {RepoFullName}", evt.PullRequestId, evt.RepoFullName);
                repo = new Repository() { Name = evt.RepoFullName, RepoId = evt.RepoId };
                ctx.Repositories.Add(repo);
            }

            var pr = new PullRequest
            {
                Author = author,
                CreatedOn = evt.createdDate,
                IsMerged = evt.isMerged,
                Repository = repo,
                MergedDate = evt.mergedDate,
                PullRequestId = evt.PullRequestId
            };
            ctx.PullRequests.Add(pr);
            await ctx.SaveChangesAsync();

        }

        private async Task Apply(OrgAddedForUserEvent evt, PortfoliOSSDBContext ctx)
        {
            if (ctx.Users.Any(x => x.UserId == evt.UserId && x.Organizations.Any(o => o.OrganizationId == evt.OrgId)))
            {
                return;
            }
            var user = ctx.Users.FirstOrDefault(x => x.UserId == evt.UserId);
            if (user == null)
            {
                _logger.Warning("OrgAddedForUserEvent: User was null. Creating. {UserId}, {OrgId}", evt.UserId, evt.OrgId);
                user = new User() { Name = evt.UserName, UserId = evt.UserId };
                ctx.Users.Add(user);
            }
            if (user.Organizations.Any(x => x.OrganizationId == evt.OrgId))
            {
                return;
            }

            var org = ctx.Organizations.FirstOrDefault(x => x.OrganizationId == evt.OrgId);
            if (org == null)
            {
                _logger.Warning("OrgAddedForUserEvent: Org was null. Creating. {UserId}, {OrgId}", evt.UserId, evt.OrgId);
                org = new Organization() { Name = evt.OrgName, OrganizationId = evt.OrgId };
                ctx.Organizations.Add(org);
            }

            user.Organizations.Add(org);
            await ctx.SaveChangesAsync();
        }

        private async Task Apply(UserAddedEvent evt, PortfoliOSSDBContext ctx)
        {
            var userExists = ctx.Users.Any(x => x.UserId == evt.UserId);
            if (!userExists)
            {
                ctx.Users.Add(new User() { Name = evt.Username, UserId = evt.UserId });
                await ctx.SaveChangesAsync();
            }
        }

        private async Task Apply(OrgAddedEvent evt, PortfoliOSSDBContext ctx)
        {
            var orgExists = ctx.Organizations.Any(x => x.OrganizationId == evt.OrgId);
            if (!orgExists)
            {
                ctx.Organizations.Add(new Organization()
                    { Name = evt.OrgName, OrganizationId = evt.OrgId });
                await ctx.SaveChangesAsync();
            }
        }

        private async Task Apply(SourceRepoAddedEvent evt, PortfoliOSSDBContext ctx)
        {
            if (ctx.Repositories.Any(x => x.RepoId == evt.RepoId))
            {
                return;
            }
            var orgToAssociateWith = ctx.Organizations.FirstOrDefault(x => x.OrganizationId == evt.OrgId);
            if (orgToAssociateWith == null)
            {
                orgToAssociateWith = new Organization()
                    { Name = evt.OrganizationName, OrganizationId = evt.OrgId };
                ctx.Organizations.Add(orgToAssociateWith);
            }

            var addedSourceRepo = new Repository
            {
                RepoId = evt.RepoId,
                Name = evt.RepoName
            };
            orgToAssociateWith.Repositories.Add(addedSourceRepo);
            await ctx.SaveChangesAsync();
        }
    }
}
