using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oven
{
    public enum ChangeType
    {
        Set,

        AddToCollection,
        RemoveFromCollection,
        ClearCollection
    }
    public struct ChangeData
    {
        public ChangeType Type;
        public object Prev;
        public object After;
    }
    public struct ChangeRevision
    {
        public int Revision;
        public ChangeData[] Changes;
    }
}
