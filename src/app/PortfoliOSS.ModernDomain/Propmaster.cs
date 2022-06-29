using Akka.Actor;
using PortfoliOSS.ModernDomain.Actors;

namespace PortfoliOSS.ModernDomain
{
    public static class Propmaster
    {
        public static Props GithubWorkerActor(string token) => Props.Create(() => new GithubWorkerActor(Constants.APP_NAME, token));
    }
}