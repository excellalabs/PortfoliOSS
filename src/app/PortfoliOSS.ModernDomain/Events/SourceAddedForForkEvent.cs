namespace PortfoliOSS.ModernDomain.Events;

public record SourceAddedForForkEvent(long ForkRepoId, long SourceRepoId);