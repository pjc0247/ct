using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace Oven
{
    public class ObservedCollection<T> : TrackingGraph, IFilling, IObserved
    {
        private List<T> Items = new List<T>();

        public ObservedCollection()
        {
        }

        public object OnGetProperty(Type type, string key)
        {
            if (key == nameof(HasChanges))
                return HasChanges;

            return typeof(List<T>).GetProperty(key)
                .GetValue(Items);
        }

        public object OnMethod(Type type, MethodInfo method, object[] args)
        {
            if (method.Name == nameof(ICollection<T>.Add))
            {
                HasChanges = true;

                if (args[0] is IObserved)
                {
                    ((TrackingGraph)((IObserved)args[0]).InnerImpl).AddReference(this);
                }

                PushChange(new ChangeData()
                {
                    Type = ChangeType.AddToCollection,
                    After = args[0]
                });
            }
            else if (method.Name == nameof(ICollection<T>.Clear))
            {
                HasChanges = true;

                if (typeof(T).GetInterface(nameof(IObserved)) != null)
                {
                    foreach (var item in Items)
                        ((TrackingGraph)((IObserved)item).InnerImpl).RemoveReference(this);
                }

                PushChange(new ChangeData()
                {
                    Type = ChangeType.ClearCollection
                });
            }
            else if (method.Name == nameof(ICollection<T>.Remove))
            {
                HasChanges = true;

                if (args[0] is IObserved)
                {
                    ((TrackingGraph)((IObserved)args[0]).InnerImpl).RemoveReference(this);
                }

                PushChange(new ChangeData()
                {
                    Type = ChangeType.RemoveFromCollection,
                    After = args[0]
                });
            }

            return typeof(List<T>)
                .GetMethod(
                    method.Name,
                    method.GetParameters().Select(x => x.ParameterType).ToArray())
                .Invoke(Items, args);
        }

        public void OnSetProperty(Type type, string key, object value)
        {
        }
    }
}
