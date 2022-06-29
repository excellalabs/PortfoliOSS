using System;
using System.Linq;
using Akka.Actor;
using Akka.Event;
using Akka.Persistence;
using PortfoliOSS.ModernDomain.Commands;
using PortfoliOSS.ModernDomain.Events;
using PortfoliOSS.ModernDomain.Messages;
using PortfoliOSS.ModernDomain.State;

namespace PortfoliOSS.ModernDomain.Actors.Persistent;

public class PersistentOrgManager : ReceivePersistentActor
{
    private ILoggingAdapter _logger;
    private OrgManagerState _state;
    private ActorSelection _githubActor;
    public PersistentOrgManager()
    {

        _logger = Context.GetLogger();
        _state = new OrgManagerState();
        _githubActor = Context.ActorSelection(Constants.ActorPaths.GITHUB_CLIENT);

        Recover<SnapshotOffer>(offer =>
        {
            if (offer.Snapshot is OrgManagerState state)
            {
                _state = state;
            }
        });

        Recover<OrgAddedEvent>(Apply);

        Recover<RecoveryCompleted>(msg =>
        {
            _logger.Info("RECOVERY: Completed for OrgManager; {OrgCount} orgs", _state.Orgs.Count);
        });

        Command<IsUserPartOfAnOrg>(msg =>
        {
            var children = Context.GetChildren();
            foreach (var child in children)
            {
                child.Forward(msg);
            }
        });

        Command<AddOrgRequest>(msg =>
        {
            _githubActor.Tell(msg, Self);
        });

        Command<AddOrgCommand>(cmd =>
        {
            if (!_state.Orgs.Contains(cmd.OrgName, StringComparer.InvariantCultureIgnoreCase))
            {
                Persist(new OrgAddedEvent(cmd.OrgName.ToLowerInvariant(), cmd.OrgId), Apply);
            }
        });
    }

    public void Apply(object @event)
    {
        switch (@event)
        {
            case OrgAddedEvent evt: 
                _state.Orgs.Add(evt.OrgName);
                Context.ActorOf(Props.Create<PersistentOrganization>(evt.OrgName, evt.OrgId), evt.OrgName);
                break;
        }
    }

    /// <summary>
    /// Id of the persistent entity for which messages should be replayed.
    /// </summary>
    public override string PersistenceId => "OrgManager";
}