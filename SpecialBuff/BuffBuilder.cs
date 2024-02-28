using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Utils;
using MonoMod.Utils.Cil;
using RandomBuff.Core.Buff;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using SystemOpCodes = System.Reflection.Emit.OpCodes;
using MonoOpCodes = Mono.Cecil.Cil.OpCodes;
using PropertyAttributes = Mono.Cecil.PropertyAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using CustomAttributeNamedArgument = Mono.Cecil.CustomAttributeNamedArgument;
using SecurityAction = System.Security.Permissions.SecurityAction;


namespace BuffExtend
{
    public static class BuffBuilder
    {

        public static (TypeDefinition buffType, TypeDefinition dataType) GenerateBuffType(string modId,string usedId,
            Action<ILProcessor> buffCtor = null,
            Action<ILProcessor> dataCtor = null)
        {
            if (!assemblyDefs.ContainsKey(modId))
            {



                assemblyDefs.Add(modId, AssemblyDefinition.CreateAssembly(
                    new AssemblyNameDefinition($"DynamicBuff_{modId}", new Version(1, 0)),
                    $"Main", ModuleKind.Dll));
                var attrType = typeof(SecurityPermissionAttribute);
             
                var ctor = attrType.GetConstructor(new[] {typeof(SecurityAction)});
                var attr = new CustomAttribute(assemblyDefs[modId].MainModule.ImportReference(ctor));

#pragma warning disable CS0618
                var attrArg = new CustomAttributeArgument(
                    assemblyDefs[modId].MainModule.ImportReference(typeof(SecurityAction)), SecurityAction.RequestMinimum);
#pragma warning restore CS0618
                attr.ConstructorArguments.Add(attrArg);
                attr.Properties.Add(new CustomAttributeNamedArgument("SkipVerification",
                    new CustomAttributeArgument(assemblyDefs[modId].MainModule.TypeSystem.Boolean,true)));
                assemblyDefs[modId].CustomAttributes.Add(attr);
            }


            var moduleDef = assemblyDefs[modId].MainModule;

            var buffType = new TypeDefinition(modId, $"{usedId}Buff", TypeAttributes.Public | TypeAttributes.Class,
                moduleDef.ImportReference(typeof(RuntimeBuff)));
            moduleDef.Types.Add(buffType);

            var staticField = new FieldDefinition(usedId, FieldAttributes.Static, moduleDef.ImportReference(typeof(BuffID)));
            buffType.Fields.Add(staticField);
            {
                buffType.DefineStaticConstructor((cctorIl) =>
                {
                    cctorIl.Emit(MonoOpCodes.Ldstr, usedId);
                    cctorIl.Emit(MonoOpCodes.Ldc_I4_1);
                    cctorIl.Emit(MonoOpCodes.Newobj,buffType.Module.ImportReference(
                        typeof(BuffID).GetConstructor(new[]{typeof(string),typeof(bool)})));
                    cctorIl.Emit(MonoOpCodes.Stsfld, staticField);
                    cctorIl.Emit(MonoOpCodes.Ret);
                });
              

                buffType.DefinePropertyOverride("ID",typeof(BuffID), MethodAttributes.Public, 
                     buffType == null ? null : (il) => BuildIdGet(il, staticField));
                buffType.DefineConstructor(buffType.Module.ImportReference(typeof(RuntimeBuff).
                        GetConstructors(BindingFlags.NonPublic|BindingFlags.Instance).First()), (il) => buffCtor?.Invoke(il));
            }


            var dataType = new TypeDefinition(modId, $"{usedId}BuffData", TypeAttributes.Public | TypeAttributes.Class, 
                moduleDef.ImportReference(typeof(BuffData)));

            moduleDef.Types.Add(dataType);
            {
                dataType.DefinePropertyOverride("ID", typeof(BuffID), MethodAttributes.Public,
                        (il) => BuildIdGet(il, staticField));

                dataType.DefineConstructor(dataType.Module.ImportReference(typeof(BuffData)
                    .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).First()),
                    dataCtor == null ? null : (il) => dataCtor?.Invoke(il));
            }

            return (buffType,dataType);
        }

        public static MethodDefinition DefineMethodOverride(this TypeDefinition type, string methodName
            ,Type returnType, Type[] argTypes, MethodAttributes extAttr,[NotNull] Action<ILProcessor> builder)
        {
            var method = new MethodDefinition(methodName, MethodAttributes.HideBySig | MethodAttributes.Virtual | extAttr,
                type.Module.ImportReference(returnType));
            type.Methods.Add(method);
            foreach(var arg in argTypes)
                method.Parameters.Add(new ParameterDefinition(type.Module.ImportReference(arg)));
            builder(method.Body.GetILProcessor());
            return method;
        }

        public static MethodDefinition DefineConstructor(this TypeDefinition type,
            [NotNull] MethodReference baseConstructor, Action<ILProcessor> builder)
        {
            const MethodAttributes methodAttributes =
                MethodAttributes.Public | MethodAttributes.HideBySig |
                MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
            var method = new MethodDefinition(".ctor", methodAttributes, type.Module.TypeSystem.Void);
            type.Methods.Add(method);
            if (builder != null)
            {
                builder(method.Body.GetILProcessor());
            }
            else
            { 
                var il = method.Body.GetILProcessor();
                il.Emit(MonoOpCodes.Ldarg_0);
                il.Emit(MonoOpCodes.Call, baseConstructor);
                il.Emit(MonoOpCodes.Ret); 
            }
            return method;
        }

        public static MethodDefinition DefineStaticConstructor(this TypeDefinition type,
            [NotNull] Action<ILProcessor> builder)
        {
            const MethodAttributes methodAttributes =
                MethodAttributes.Private | MethodAttributes.HideBySig |
                MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.Static;
            var method = new MethodDefinition(".cctor", methodAttributes, type.Module.TypeSystem.Void);
            type.Methods.Add(method);
            builder(method.Body.GetILProcessor());
            return method;
        }


        public static PropertyDefinition DefinePropertyOverride(this TypeDefinition type, string propertyName,Type returnType
            ,MethodAttributes extAttr, Action<ILProcessor> getBuilder = null, Action<ILProcessor> setBuilder = null)
        {
            MethodAttributes attr =
                MethodAttributes.SpecialName |
                MethodAttributes.HideBySig | MethodAttributes.Virtual | extAttr;

            var property = new PropertyDefinition($"{propertyName}", PropertyAttributes.None, type.Module.ImportReference(returnType));
            if (getBuilder != null)
            {
                var method = type.DefineMethodOverride($"get_{propertyName}", returnType, Type.EmptyTypes, attr, getBuilder);
                property.GetMethod = method;
            }
            if (setBuilder != null)
            {
                var method = type.DefineMethodOverride($"set_{propertyName}", returnType, Type.EmptyTypes, attr, setBuilder);
                property.SetMethod = method;
            }

            return property;
        }

        public static Assembly FinishGenerate(string modId,string debugOutputPath = null)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                if (assemblyDefs.ContainsKey(modId))
                {
                    assemblyDefs[modId].Write(ms);
                    if(debugOutputPath != null)
                        assemblyDefs[modId].Write(debugOutputPath);
                }

                return Assembly.Load(ms.GetBuffer());
            }

            return null;
        }


        private static void BuildIdGet(ILProcessor il, FieldDefinition staticField)
        {
            il.Emit(MonoOpCodes.Ldsfld,staticField);
            il.Emit(MonoOpCodes.Ret);
        }

        private static readonly Dictionary<string, AssemblyDefinition> assemblyDefs = new();

    }
}
