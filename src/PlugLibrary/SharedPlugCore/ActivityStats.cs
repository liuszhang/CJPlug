using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedPlugCore
{
    public class ActivityStats
    {
        //
        // 摘要:
        //     The number of times the activity has been started.
        public long Started { get; set; }

        //
        // 摘要:
        //     The number of times the activity has been completed.
        public long Completed { get; set; }

        //
        // 摘要:
        //     The number of times the activity has been uncompleted.
        public long Uncompleted { get; set; }

        //
        // 摘要:
        //     Whether the activity is blocked.
        public bool Blocked { get; set; }

        //
        // 摘要:
        //     Whether the activity has faulted.
        public bool Faulted { get; set; }
    }
}
