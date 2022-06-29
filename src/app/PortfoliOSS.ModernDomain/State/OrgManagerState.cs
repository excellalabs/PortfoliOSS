using System.Collections.Generic;

namespace PortfoliOSS.ModernDomain.State;

public class OrgManagerState
{
    public List<string> Orgs { get; set; }

    public OrgManagerState()
    {
        Orgs = new List<string>();
    }
}