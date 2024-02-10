using System.Collections.Generic;
using System.Configuration;
using Akka.Actor;
using Serilog;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Akka.Persistence.Query;
using Akka.Persistence.Query.Sql;
using Akka.Streams;
using Akka.Streams.Dsl;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using PortfoliOSS.ModernDomain;
using PortfoliOSS.ModernDomain.Actors;
using PortfoliOSS.ModernDomain.Actors.Persistent;
using PortfoliOSS.ModernDomain.Commands;
using PortfoliOSS.ModernData;

namespace PortfoliOSS.ModernConsole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using IHost host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddDbContextFactory<PortfoliOSSDBContext>();
                })
                .ConfigureAppConfiguration(c =>
                {
                    c.AddUserSecrets(Assembly.GetExecutingAssembly());
                })
                .ConfigureLogging(x => x.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning))
                // TODO: ConfigureLogging with the serilog config below
                .Build();

            var logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.Seq("http://localhost:5341") // TODO: Pull from config
                .MinimumLevel.Debug()
                .CreateLogger();

            IConfiguration config = host.Services.GetRequiredService<IConfiguration>();

            var peopleAndTokens = config.GetSection("PeopleAndTokens").GetChildren(); // TODO: convert to strongly typed config
            List<DonatedGitHubToken> tokens = new();

            foreach (var item in peopleAndTokens)
            {
                tokens.Add(new DonatedGitHubToken() { Person = item.Key, Token = item.Value });
            }

            Log.Logger = logger;
            var configText = await File.ReadAllTextAsync("Config.Hocon"); // TODO: Extract DB connection string into config/secret
            var actorSystem = ActorSystem.Create(Constants.APP_NAME, configText);
            var writer = actorSystem.ActorOf(Props.Create<CreateViewsActor>(host.Services.GetRequiredService<IDbContextFactory<PortfoliOSSDBContext>>()), "writer");

            var readJournal = PersistenceQuery.Get(actorSystem)
                .ReadJournalFor<SqlReadJournal>("akka.persistence.query.my-read-journal");
            var materializer = actorSystem.Materializer();
            Log.Logger.Information("Trying to stream the events");

            // TODO: This is an issue currently. If you have 8 million events, it will replay them all. 
            // The operations shouldn't be a problem, but it will certainly take time. If you need to offset, you'll have to 
            // Change the offset manually for a given run. We have an item in the backlog to tackle this.
            readJournal.AllEvents(Offset.NoOffset()).RunForeach(env => writer.Tell(env), materializer);

            var githubClientActor = actorSystem.ActorOf(Props.Create<GithubClientActor>(tokens), Constants.ActorNames.GITHUB_CLIENT_ACTOR_NAME);
            var orgManager = actorSystem.ActorOf(Props.Create<PersistentOrgManager>(), "OrgManager");
            var userManager = actorSystem.ActorOf(Props.Create<PersistentUserManager>(), "UserManager");
            var RepoManager = actorSystem.ActorOf(Props.Create<PersistentRepoManager>(), "RepoManager");

            // TODO: This is hard-coded right now. Will need to fix in the future.
            orgManager.Tell(new AddOrgRequest("killeencode"));

            await actorSystem.WhenTerminated;

            await host.RunAsync();
        }
    }
}
