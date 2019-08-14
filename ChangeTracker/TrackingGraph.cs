using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oven
{
    public class TrackingGraph : IObserved
    {
        private bool _HasChanges;
        public bool HasChanges
        {
            get
            {
                return _HasChanges;
            }
            set
            {
                // ISSUE : 쌍방참조
                _HasChanges = value;

                if (value)
                {
                    foreach (var parent in Parents)
                        parent.HasChanges = true;
                }
            }
        }

        public object InnerImpl { get; set; }
        public ChangeRevision UncommitedRevision
        {
            get
            {
                UpdateLastRevision();
                return _Revisions.Last();
            }
        }
        public ChangeRevision[] Revisions
        {
            get
            {
                UpdateLastRevision();
                return _Revisions.ToArray();
            }
        }

        private List<ChangeRevision> _Revisions = new List<ChangeRevision>();
        private List<ChangeData> Changes = new List<ChangeData>();
        private List<TrackingGraph> Parents = new List<TrackingGraph>();
        //private TrackingGraph Children { get; set; }

        public TrackingGraph()
        {
            _Revisions.Add(new ChangeRevision());
        }

        public void AddReference(TrackingGraph parent)
        {
            Parents.Add(parent);
        }
        public void RemoveReference(TrackingGraph parent)
        {
            Parents.Remove(parent);
        }
        public void ConfirmChanges()
        {
            UpdateLastRevision();
            Changes.Clear();

            _Revisions.Add(new ChangeRevision());

            _HasChanges = false;
        }

        public void PushChange(ChangeData cd)
        {
            foreach (var p in Parents)
                p.PushChange(cd);
            Changes.Add(cd);
        }
        private void UpdateLastRevision()
        {
            var rev = _Revisions.Last();
            rev.Changes = Changes.ToArray();
            rev.Revision = _Revisions.Count - 1;
            _Revisions[_Revisions.Count - 1] = rev;
        }
    }
}
