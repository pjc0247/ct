using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reflection;
using System.Reflection.Emit;

namespace Oven
{
    public class Bakery
    {
        private class BakeInfo
        {
            public FieldBuilder Target { get; set; }
            public TypeBuilder TypeBuilder { get; set; }
        }

        private Bakery()
        {
        }

        private static TypeBuilder CreateType(
            BakeInfo info,
            Type intf)
        {
            var implName = intf.Name + "Impl";

            var assemblyBuilder =
                AppDomain.CurrentDomain.DefineDynamicAssembly(
                    new AssemblyName(implName),
                    AssemblyBuilderAccess.Run);
            var moduleBuilder =
                assemblyBuilder.DefineDynamicModule("Module");
            var typeBuilder = moduleBuilder.DefineType(
                implName,
                TypeAttributes.Public |
                TypeAttributes.Class |
                TypeAttributes.AutoClass |
                TypeAttributes.AnsiClass |
                TypeAttributes.BeforeFieldInit |
                TypeAttributes.AutoLayout,
                null,
                new Type[] { intf });

            info.Target = typeBuilder.DefineField(
                "target", typeof(object), FieldAttributes.Private);

            ConstructorBuilder ctor =
                typeBuilder.DefineConstructor(
                    MethodAttributes.Public | MethodAttributes.SpecialName,
                    CallingConventions.Standard,
                    new Type[] { typeof(object) });
            ILGenerator ilGen =
                ctor.GetILGenerator();

            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldarg_1);
            ilGen.Emit(OpCodes.Stfld, info.Target);
            ilGen.Emit(OpCodes.Ret);

            return typeBuilder;
        }
        private static MethodBuilder CreateMethod(
            BakeInfo info,
            Type intf, Type impl,
            TypeBuilder typeBuilder, MethodInfo method)
        {
            var paramTypes =
                    method.GetParameters().Select(m => m.ParameterType).ToArray();
            var methodBuilder = typeBuilder.DefineMethod(
                method.Name,
                MethodAttributes.Public |
                MethodAttributes.Virtual |
                MethodAttributes.NewSlot |
                MethodAttributes.HideBySig |
                MethodAttributes.Final,
                method.ReturnType,
                paramTypes);
            var ilGen = methodBuilder.GetILGenerator();

            /* args... -> object[] */
            int argc = 0;
            var args = ilGen.DeclareLocal(typeof(object[]));
            var typeInfo = ilGen.DeclareLocal(typeof(Type));
            var methodInfo = ilGen.DeclareLocal(typeof(MethodInfo));

            ilGen.Emit(OpCodes.Ldc_I4, paramTypes.Length);
            ilGen.Emit(OpCodes.Newarr, typeof(object));
            ilGen.Emit(OpCodes.Stloc, args);

            foreach (var param in method.GetParameters())
            {
                ilGen.Emit(OpCodes.Ldloc, args);
                ilGen.Emit(OpCodes.Ldc_I4, argc);
                ilGen.Emit(OpCodes.Ldarg, argc + 1);
                if (paramTypes[argc].IsValueType)
                    ilGen.Emit(OpCodes.Box, paramTypes[argc]);
                ilGen.Emit(OpCodes.Stelem_Ref);

                argc++;
            }

            /* ld_this */
            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldfld, info.Target);

            var getTypeFromHandle =
                typeof(Type).GetMethod("GetTypeFromHandle");

            var getMethodFromHandle =
                typeof(MethodBase).GetMethod(
                    "GetMethodFromHandle",
                    new[] { typeof(RuntimeMethodHandle), typeof(RuntimeTypeHandle) });
            ilGen.Emit(OpCodes.Ldtoken, intf);
            ilGen.Emit(OpCodes.Call, getTypeFromHandle);
            ilGen.Emit(OpCodes.Castclass, typeof(Type));
            ilGen.Emit(OpCodes.Ldtoken, method);
            ilGen.Emit(OpCodes.Ldtoken, method.DeclaringType);
            ilGen.Emit(OpCodes.Call, getMethodFromHandle);
            ilGen.Emit(OpCodes.Castclass, typeof(MethodInfo));

            ilGen.Emit(OpCodes.Ldloc, args);

            /* performs proxy call */
            ilGen.Emit(
                OpCodes.Call,
                impl.GetMethod(
                    "OnMethod",
                    BindingFlags.Instance | BindingFlags.Public));
            if (method.ReturnType != typeof(void))
            {
                if (method.ReturnType.IsValueType)
                    ilGen.Emit(OpCodes.Unbox_Any, method.ReturnType);
                else
                    ilGen.Emit(OpCodes.Castclass, method.ReturnType);
            }
            else
                ilGen.Emit(OpCodes.Pop);

            ilGen.Emit(OpCodes.Ret);

            typeBuilder.DefineMethodOverride(
                methodBuilder,
                method);

            return methodBuilder;
        }
        private static MethodBuilder CreateProperty(
            BakeInfo info,
            Type intf, Type impl,
            TypeBuilder typeBuilder, PropertyInfo prop)
        {
            var name = prop.Name;
            var type = prop.PropertyType;
            var backingField = typeBuilder.DefineField(
                    "_" + name, type, FieldAttributes.Private);
            var propertyBuilder = typeBuilder.DefineProperty(
                name,
                PropertyAttributes.HasDefault,
                type, new Type[] { type });

            if (prop.CanRead)
            {
                var methodBuilder = typeBuilder.DefineMethod(
                    "get_" + name,
                    MethodAttributes.Public |
                    MethodAttributes.Virtual |
                    MethodAttributes.NewSlot |
                    MethodAttributes.HideBySig |
                    MethodAttributes.SpecialName |
                    MethodAttributes.Final,
                    type,
                    null);
                var ilGen = methodBuilder.GetILGenerator();

                ilGen.Emit(OpCodes.Ldarg_0);
                ilGen.Emit(OpCodes.Ldfld, info.Target);

                var getTypeFromHandle =
                    typeof(Type).GetMethod("GetTypeFromHandle");
                ilGen.Emit(OpCodes.Ldtoken, type);
                ilGen.Emit(OpCodes.Call, getTypeFromHandle);
                ilGen.Emit(OpCodes.Castclass, typeof(Type));

                ilGen.Emit(OpCodes.Ldstr, name);

                ilGen.Emit(
                    OpCodes.Callvirt,
                    impl.GetMethod(
                        nameof(IFilling.OnGetProperty),
                        BindingFlags.Instance | BindingFlags.Public));
                //ilGen.Emit(OpCodes.Pop);
                if (type.IsValueType)
                    ilGen.Emit(OpCodes.Unbox_Any, type);
                else
                    ilGen.Emit(OpCodes.Castclass, type);
                ilGen.Emit(OpCodes.Ret);

                propertyBuilder.SetGetMethod(methodBuilder);
            }
            if (prop.CanWrite)
            {
                var methodBuilder = typeBuilder.DefineMethod(
                    "set_" + name,
                    MethodAttributes.Public |
                    MethodAttributes.Virtual |
                    MethodAttributes.NewSlot |
                    MethodAttributes.HideBySig |
                    MethodAttributes.SpecialName |
                    MethodAttributes.Final,
                    null,
                    new Type[] { type });
                var ilGen = methodBuilder.GetILGenerator();

                ilGen.Emit(OpCodes.Ldarg_0);
                ilGen.Emit(OpCodes.Ldfld, info.Target);

                var getTypeFromHandle = 
                    typeof(Type).GetMethod("GetTypeFromHandle");
                ilGen.Emit(OpCodes.Ldtoken, type);
                ilGen.Emit(OpCodes.Call, getTypeFromHandle);
                ilGen.Emit(OpCodes.Castclass, typeof(Type));

                ilGen.Emit(OpCodes.Ldstr, name);

                ilGen.Emit(OpCodes.Ldarg_1);
                ilGen.Emit(OpCodes.Box, type);

                ilGen.Emit(
                    OpCodes.Call,
                    impl.GetMethod(
                        nameof(IFilling.OnSetProperty),
                        BindingFlags.Instance | BindingFlags.Public));
                ilGen.Emit(OpCodes.Ret);
                
                propertyBuilder.SetSetMethod(methodBuilder);
            }

            return null;
        }

        static List<PropertyInfo> GetProperties(Type intf)
        {
            List<PropertyInfo> props = new List<PropertyInfo>();
            HashSet<Type> processed = new HashSet<Type>();
            var q = new Queue<Type>();

            q.Enqueue(intf);
            while (q.Count > 0)
            {
                var v = q.Dequeue();

                processed.Add(v);
                foreach (var i in v.GetInterfaces())
                {
                    if (processed.Contains(i))
                        continue;

                    q.Enqueue(i);
                }

                props.AddRange(v.GetProperties());
            }

            return props.Distinct().ToList();
        }
        static List<MethodInfo> GetMethods(Type intf)
        {
            List<MethodInfo> props = new List<MethodInfo>();
            HashSet<Type> processed = new HashSet<Type>();
            var q = new Queue<Type>();

            q.Enqueue(intf);
            while (q.Count > 0)
            {
                var v = q.Dequeue();

                processed.Add(v);
                foreach (var i in v.GetInterfaces())
                {
                    if (processed.Contains(i))
                        continue;

                    q.Enqueue(i);
                }

                props.AddRange(v.GetMethods());
            }

            return props.Distinct().ToList();
        }

        public static TBakeInterface Bake<TBakeInterface, TBakeImpl>()
            where TBakeImpl : IFilling
        {
            object trash;
            return (TBakeInterface)Bake(typeof(TBakeInterface), typeof(TBakeImpl), out trash);
        }
        public static object Bake(Type bakeInterface, Type bakeImpl, out object impl)
        {
            var info = new BakeInfo();
            var typeBuilder = CreateType(info, bakeInterface);
            info.TypeBuilder = typeBuilder;

            foreach (var prop in GetProperties(bakeInterface))
            {
                CreateProperty(
                    info,
                    bakeInterface, bakeImpl,
                    typeBuilder, prop);
            }
            foreach (var method in GetMethods(bakeInterface))
            {
                if (method.IsSpecialName)
                    continue;

                CreateMethod(
                    info,
                    bakeInterface, bakeImpl,
                    typeBuilder, method);
            }

            Type type = typeBuilder.CreateType();
            impl = Activator.CreateInstance(bakeImpl);
            object obj = Activator.CreateInstance(type, new object[] { impl });

            return obj;
        }
    }
}