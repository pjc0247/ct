using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace Oven
{
    public interface IFilling
    {
        object OnMethod(Type type, MethodInfo method, object[] args);

        void OnSetProperty(Type type, string key, object value);
        object OnGetProperty(Type type, string prop);
    }
}