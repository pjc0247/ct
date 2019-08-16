using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace Oven
{
    public interface IObserved
    {
        bool HasChanges { get; }

        void ConfirmChanges();

        object InnerImpl { get; set; }

        #region RECORDING
        ChangeRevision UncommitedRevision { get; }
        ChangeRevision[] Revisions { get; }
        #endregion
    }
    public interface IObservedCollection<T> : IObserved, ICollection<T>
    {
    }
    public interface IObservedList<T> : IObserved, IList<T>
    {
    }
}