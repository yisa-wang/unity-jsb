﻿using System;
using System.Collections.Generic;
using System.Reflection;

namespace QuickJS.Editor
{
    // 生成成员方法绑定代码
    public class OperatorCodeGen : MethodBaseCodeGen<MethodInfo>
    {
        protected OperatorBindingInfo bindingInfo;

        protected override Type GetReturnType(MethodInfo method)
        {
            return method.ReturnType;
        }

        private string OpArgsConcat(List<string> args, string sp = ", ")
        {
            if (args.Count == 1)
            {
                return sp + args[0];
            }
            var len = args.Count;
            var res = "";
            for (var i = 0; i < len; i++)
            {
                res += args[i];
                if (i != len - 1)
                {
                    res += sp;
                }
            }

            return res;
        }

        protected override string GetInvokeBinding(string caller, MethodInfo method, bool hasParams, bool isExtension, string nargs, ParameterInfo[] parameters, List<ParameterInfo> parametersByRef)
        {
            var arglist = OpArgsConcat(AppendGetParameters(hasParams, nargs, parameters, parametersByRef), " " + bindingInfo.cs_op + " ");
            var transform = cg.bindingManager.GetTypeTransform(method.DeclaringType);
            if (transform == null || !transform.OnBinding(BindingPoints.METHOD_BINDING_BEFORE_INVOKE, method, cg))
            {
            }

            return arglist;
        }

        public OperatorCodeGen(CodeGenerator cg, OperatorBindingInfo bindingInfo)
            : base(cg)
        {
            this.bindingInfo = bindingInfo;
            WriteAllVariants(this.bindingInfo);
            // WriteTSAllVariants(this.bindingInfo);
        }
    }

    public class TSOperatorCodeGen : MethodBaseCodeGen<MethodInfo>
    {
        protected OperatorBindingInfo bindingInfo;

        protected override Type GetReturnType(MethodInfo method)
        {
            return method.ReturnType;
        }

        protected override string GetInvokeBinding(string caller, MethodInfo method, bool hasParams, bool isExtension, string nargs, ParameterInfo[] parameters, List<ParameterInfo> parametersByRef)
        {
            return null;
        }

        public TSOperatorCodeGen(CodeGenerator cg, OperatorBindingInfo bindingInfo)
            : base(cg)
        {
            this.bindingInfo = bindingInfo;
            WriteTSAllVariants(this.bindingInfo);
        }
    }
}