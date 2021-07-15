using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo.FileStorage.Storage.LocalObjectStorage.Shards
{
    public enum ShardMode
    {
        Undefined,
        Active,
        Inactive,
        ReadOnly,
        Fault,
        Evacuate
    }
}
