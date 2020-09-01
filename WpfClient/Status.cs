using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfClient
{
    public enum Status
    {
        WaitingToStart,
        PriorGameAborted,
        WonByWhite,
        WonByBlack,
        Tie,
        NextTurnByHumanWhite,
        NextTurnByHumanBlack,
        NextTurnByComputerWhite,
        NextTurnByComputerBlack,
    }
}
