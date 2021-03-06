﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using AOT;
using QuickJS.Binding;
using QuickJS.Native;
using QuickJS.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QuickJS
{
    public class ScriptContext
    {
        public event Action<ScriptContext> OnDestroy;

        private ScriptRuntime _runtime;
        private JSContext _ctx;
        private AtomCache _atoms;
        private JSValue _moduleCache; // commonjs module cache
        private JSValue _require; // require function object 
        private CoroutineManager _coroutines;
        private bool _isValid;

        private JSValue _globalObject;
        private JSValue _operatorCreate;
        private JSValue _numberConstructor;
        private JSValue _stringConstructor;

        public ScriptContext(ScriptRuntime runtime)
        {
            _isValid = true;
            _runtime = runtime;
            _ctx = JSApi.JS_NewContext(_runtime);
            JSApi.JS_AddIntrinsicOperators(_ctx);
            _atoms = new AtomCache(_ctx);
            _moduleCache = JSApi.JS_NewObject(_ctx);

            _globalObject = JSApi.JS_GetGlobalObject(_ctx);
            _numberConstructor = JSApi.JS_GetProperty(_ctx, _globalObject, JSApi.JS_ATOM_Number);
            _stringConstructor = JSApi.JS_GetProperty(_ctx, _globalObject, JSApi.JS_ATOM_String);
            _operatorCreate = JSApi.JS_UNDEFINED;

            var operators = JSApi.JS_GetProperty(_ctx, _globalObject, JSApi.JS_ATOM_Operators);
            if (!operators.IsNullish())
            {
                if (operators.IsException())
                {
                    _ctx.print_exception();
                }
                else
                {
                    var create = JSApi.JS_GetProperty(_ctx, operators, GetAtom("create"));
                    JSApi.JS_FreeValue(_ctx, operators);
                    if (create.IsException())
                    {
                        _ctx.print_exception();
                    }
                    else
                    {
                        if (JSApi.JS_IsFunction(_ctx, create) == 1)
                        {
                            _operatorCreate = create;
                        }
                        else
                        {
                            JSApi.JS_FreeValue(_ctx, create);
                        }
                    }
                }
            }
        }

        public bool IsValid()
        {
            return _isValid;
        }

        public JSValue Yield(YieldInstruction yieldInstruction)
        {
            if (_isValid)
            {
                if (_coroutines == null)
                {
                    var go = _runtime.GetContainer();
                    if (go != null)
                    {
                        _coroutines = go.AddComponent<CoroutineManager>();
                    }
                }
            }

            if (_coroutines != null)
            {
                return _coroutines.Yield(this, yieldInstruction);
            }

            return JSApi.JS_UNDEFINED;
        }

        public JSValue Yield(System.Threading.Tasks.Task task)
        {
            if (_isValid)
            {
                if (_coroutines == null)
                {
                    var go = _runtime.GetContainer();
                    if (go != null)
                    {
                        _coroutines = go.AddComponent<CoroutineManager>();
                    }
                }
            }

            if (_coroutines != null)
            {
                return _coroutines.Yield(this, task);
            }

            return JSApi.JS_UNDEFINED;
        }

        public TimerManager GetTimerManager()
        {
            return _runtime.GetTimerManager();
        }

        public IScriptLogger GetLogger()
        {
            return _runtime.GetLogger();
        }

        public TypeDB GetTypeDB()
        {
            return _runtime.GetTypeDB();
        }

        public ScriptRuntime GetRuntime()
        {
            return _runtime;
        }

        public bool IsContext(JSContext ctx)
        {
            return ctx == _ctx;
        }

        //NOTE: 返回值不需要释放, context 销毁时会自动释放所管理的 Atom
        public JSAtom GetAtom(string name)
        {
            return _atoms.GetAtom(name);
        }

        public void Destroy()
        {
            _isValid = false;

            try
            {
                OnDestroy?.Invoke(this);
            }
            catch (Exception e)
            {
                _runtime.GetLogger().Error(e);
            }
            _atoms.Clear();

            JSApi.JS_FreeValue(_ctx, _numberConstructor);
            JSApi.JS_FreeValue(_ctx, _stringConstructor);
            JSApi.JS_FreeValue(_ctx, _globalObject);
            JSApi.JS_FreeValue(_ctx, _operatorCreate);

            JSApi.JS_FreeValue(_ctx, _moduleCache);
            JSApi.JS_FreeValue(_ctx, _require);
            JSApi.JS_FreeContext(_ctx);

            if (_coroutines != null)
            {
                Object.DestroyImmediate(_coroutines);
                _coroutines = null;
            }

            _ctx = JSContext.Null;
        }

        public void FreeValue(JSValue value)
        {
            _runtime.FreeValue(value);
        }

        public void FreeValues(JSValue[] values)
        {
            _runtime.FreeValues(values);
        }

        ///<summary>
        /// 获取全局对象 (增加引用计数)
        ///</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public JSValue GetGlobalObject()
        {
            return JSApi.JS_DupValue(_ctx, _globalObject);
        }

        ///<summary>
        /// 获取 string.constructor (增加引用计数)
        ///</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public JSValue GetStringConstructor()
        {
            return JSApi.JS_DupValue(_ctx, _stringConstructor);
        }

        ///<summary>
        /// 获取 number.constructor (增加引用计数)
        ///</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public JSValue GetNumberConstructor()
        {
            return JSApi.JS_DupValue(_ctx, _numberConstructor);
        }

        ///<summary>
        /// 获取 operator.create (增加引用计数)
        ///</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public JSValue GetOperatorCreate()
        {
            return JSApi.JS_DupValue(_ctx, _operatorCreate);
        }

        #region Builtins

        //NOTE: 返回值需要调用者 free 
        public JSValue _get_commonjs_module(string module_id)
        {
            var prop = GetAtom(module_id);
            return JSApi.JS_GetProperty(_ctx, _moduleCache, prop);
        }

        //NOTE: 返回值需要调用者 free
        public JSValue _new_commonjs_module(string module_id, JSValue exports_obj, bool loaded)
        {
            var module_obj = JSApi.JS_NewObject(_ctx);
            var prop = GetAtom(module_id);

            JSApi.JS_SetProperty(_ctx, _moduleCache, prop, JSApi.JS_DupValue(_ctx, module_obj));
            JSApi.JS_SetProperty(_ctx, module_obj, GetAtom("cache"), JSApi.JS_DupValue(_ctx, _moduleCache));
            JSApi.JS_SetProperty(_ctx, module_obj, GetAtom("loaded"), JSApi.JS_NewBool(_ctx, loaded));
            JSApi.JS_SetProperty(_ctx, module_obj, GetAtom("exports"), JSApi.JS_DupValue(_ctx, exports_obj));

            return module_obj;
        }

        [MonoPInvokeCallback(typeof(JSCFunctionMagic))]
        private static JSValue _print(JSContext ctx, JSValue this_obj, int argc, JSValue[] argv, int magic)
        {
            var runtime = ScriptEngine.GetRuntime(ctx);
            if (runtime == null)
            {
                return JSApi.JS_UNDEFINED;
            }
            var logger = runtime.GetLogger();
            if (logger == null)
            {
                return JSApi.JS_UNDEFINED;
            }
            int i;
            var sb = new StringBuilder();
            size_t len;

            for (i = 0; i < argc; i++)
            {
                if (i != 0)
                {
                    sb.Append(' ');
                }

                var pstr = JSApi.JS_ToCStringLen(ctx, out len, argv[i]);
                if (pstr == IntPtr.Zero)
                {
                    return JSApi.JS_EXCEPTION;
                }

                var str = JSApi.GetString(pstr, len);
                if (str != null)
                {
                    sb.Append(str);
                }

                JSApi.JS_FreeCString(ctx, pstr);
            }

            sb.AppendLine();
            runtime.AppendStacktrace(ctx, sb);
            logger.ScriptWrite((LogLevel)magic, sb.ToString());
            return JSApi.JS_UNDEFINED;
        }

        #endregion

        [MonoPInvokeCallback(typeof(JSCFunction))]
        public static JSValue to_js_array(JSContext ctx, JSValue this_obj, int argc, JSValue[] argv)
        {
            if (argc < 1)
            {
                return JSApi.JS_ThrowInternalError(ctx, "array expected");
            }
            if (JSApi.JS_IsArray(ctx, argv[0]) == 1)
            {
                return JSApi.JS_DupValue(ctx, argv[0]);
            }

            Array o;
            if (!Values.js_get_classvalue<Array>(ctx, argv[0], out o))
            {
                return JSApi.JS_ThrowInternalError(ctx, "array expected");
            }
            if (o == null)
            {
                return JSApi.JS_NULL;
            }
            var len = o.Length;
            var rval = JSApi.JS_NewArray(ctx);
            try
            {
                for (var i = 0; i < len; i++)
                {
                    var obj = o.GetValue(i);
                    var elem = Values.js_push_var(ctx, obj);
                    JSApi.JS_SetPropertyUint32(ctx, rval, (uint)i, elem);
                }
            }
            catch (Exception exception)
            {
                JSApi.JS_FreeValue(ctx, rval);
                return JSApi.ThrowException(ctx, exception);
            }
            return rval;
        }

        [MonoPInvokeCallback(typeof(JSCFunction))]
        public static JSValue hotfix_replace_single(JSContext ctx, JSValue this_obj, int argc, JSValue[] argv)
        {
            if (argc < 3)
            {
                return JSApi.JS_ThrowInternalError(ctx, "type_name, func_name, func  expected");
            }
            if (!argv[0].IsString() || !argv[1].IsString() || JSApi.JS_IsFunction(ctx, argv[1]) != 1)
            {
                return JSApi.JS_ThrowInternalError(ctx, "type_name, func_name expected");
            }

            var type_name = JSApi.GetString(ctx, argv[0]);
            var func_name = JSApi.GetString(ctx, argv[1]);
            // var func_val = JSApi.JS
            //TODO: assign field
            return JSApi.JS_UNDEFINED;
        }

        [MonoPInvokeCallback(typeof(JSCFunction))]
        public static JSValue yield_func(JSContext ctx, JSValue this_obj, int argc, JSValue[] argv)
        {
            if (argc < 1)
            {
                return JSApi.JS_ThrowInternalError(ctx, "type YieldInstruction or Task expected");
            }
            object awaitObject;
            if (Values.js_get_cached_object(ctx, argv[0], out awaitObject))
            {
                var context = ScriptEngine.GetContext(ctx);
                var task = awaitObject as System.Threading.Tasks.Task;
                if (task != null)
                {
                    return context.Yield(task);
                }

                var yieldInstruction = awaitObject as YieldInstruction;
                return context.Yield(yieldInstruction);
            }

            return JSApi.JS_ThrowInternalError(ctx, "type YieldInstruction or Task expected");
        }

        public static void Bind(TypeRegister register)
        {
            var ns_jsb = register.CreateNamespace("jsb");
            ns_jsb.AddFunction("Yield", yield_func, 1);
            ns_jsb.AddFunction("ToJSArray", to_js_array, 1);
            {
                var ns_jsb_hotfix = ns_jsb.CreateNamespace("hotfix");
                ns_jsb_hotfix.AddFunction("replace_single", hotfix_replace_single, 2);
                // ns_jsb_hotfix.AddFunction("replace", hotfix_replace, 2);
                // ns_jsb_hotfix.AddFunction("before", hotfix_before);
                // ns_jsb_hotfix.AddFunction("after", hotfix_after);
                ns_jsb_hotfix.Close();
            }
            ns_jsb.Close();
        }

        public unsafe void EvalMain(byte[] input_bytes, string fileName)
        {
            var dirname = PathUtils.GetDirectoryName(fileName);
            var filename_bytes = TextUtils.GetNullTerminatedBytes(fileName);
            var filename_atom = GetAtom(fileName);
            var dirname_atom = GetAtom(dirname);

            var exports_obj = JSApi.JS_NewObject(_ctx);
            var require_obj = JSApi.JS_DupValue(_ctx, _require);
            var module_obj = _new_commonjs_module("", exports_obj, true);
            var filename_obj = JSApi.JS_AtomToString(_ctx, filename_atom);
            var dirname_obj = JSApi.JS_AtomToString(_ctx, dirname_atom);
            var require_argv = new JSValue[5] { exports_obj, require_obj, module_obj, filename_obj, dirname_obj };
            JSApi.JS_SetProperty(_ctx, require_obj, GetAtom("moduleId"), JSApi.JS_DupValue(_ctx, filename_obj));

            fixed (byte* input_ptr = input_bytes)
            fixed (byte* resolved_id_ptr = filename_bytes)
            {
                var input_len = (size_t)(input_bytes.Length - 1);
                var func_val = JSApi.JS_Eval(_ctx, input_ptr, input_len, resolved_id_ptr, JSEvalFlags.JS_EVAL_TYPE_GLOBAL | JSEvalFlags.JS_EVAL_FLAG_STRICT);
                if (func_val.IsException())
                {
                    FreeValues(require_argv);
                    _ctx.print_exception();
                    return;
                }

                if (JSApi.JS_IsFunction(_ctx, func_val) == 1)
                {
                    var rval = JSApi.JS_Call(_ctx, func_val, JSApi.JS_UNDEFINED, require_argv.Length, require_argv);
                    if (rval.IsException())
                    {
                        JSApi.JS_FreeValue(_ctx, func_val);
                        FreeValues(require_argv);
                        _ctx.print_exception();
                        return;
                    }
                }

                JSApi.JS_FreeValue(_ctx, func_val);
                FreeValues(require_argv);
            }
        }

        public void EvalSource(string source, string fileName)
        {
            var jsValue = JSApi.JS_Eval(_ctx, source, fileName);
            if (JSApi.JS_IsException(jsValue))
            {
                _ctx.print_exception();
            }

            JSApi.JS_FreeValue(_ctx, jsValue);
        }

        public void RegisterBuiltins()
        {
            var ctx = (JSContext)this;
            var global_object = this.GetGlobalObject();
            {
                _require = JSApi.JSB_NewCFunction(ctx, ScriptRuntime.module_require, GetAtom("require"), 1, JSCFunctionEnum.JS_CFUNC_generic, 0);
                JSApi.JS_SetProperty(ctx, _require, GetAtom("moduleId"), JSApi.JS_NewString(ctx, ""));
                JSApi.JS_SetProperty(ctx, _require, GetAtom("cache"), JSApi.JS_DupValue(ctx, _moduleCache));
                JSApi.JS_SetProperty(ctx, global_object, GetAtom("require"), JSApi.JS_DupValue(ctx, _require));

                JSApi.JS_SetPropertyStr(ctx, global_object, "print", JSApi.JS_NewCFunctionMagic(ctx, _print, "print", 1, JSCFunctionEnum.JS_CFUNC_generic_magic, 0));
                var console = JSApi.JS_NewObject(ctx);
                {
                    JSApi.JS_SetPropertyStr(ctx, console, "log", JSApi.JS_NewCFunctionMagic(ctx, _print, "log", 1, JSCFunctionEnum.JS_CFUNC_generic_magic, 0));
                    JSApi.JS_SetPropertyStr(ctx, console, "info", JSApi.JS_NewCFunctionMagic(ctx, _print, "info", 1, JSCFunctionEnum.JS_CFUNC_generic_magic, 0));
                    JSApi.JS_SetPropertyStr(ctx, console, "debug", JSApi.JS_NewCFunctionMagic(ctx, _print, "debug", 1, JSCFunctionEnum.JS_CFUNC_generic_magic, 0));
                    JSApi.JS_SetPropertyStr(ctx, console, "warn", JSApi.JS_NewCFunctionMagic(ctx, _print, "warn", 1, JSCFunctionEnum.JS_CFUNC_generic_magic, 1));
                    JSApi.JS_SetPropertyStr(ctx, console, "error", JSApi.JS_NewCFunctionMagic(ctx, _print, "error", 1, JSCFunctionEnum.JS_CFUNC_generic_magic, 2));
                    JSApi.JS_SetPropertyStr(ctx, console, "assert", JSApi.JS_NewCFunctionMagic(ctx, _print, "assert", 1, JSCFunctionEnum.JS_CFUNC_generic_magic, 3));
                }
                JSApi.JS_SetPropertyStr(ctx, global_object, "console", console);
            }
            JSApi.JS_FreeValue(ctx, global_object);
        }

        public static implicit operator JSContext(ScriptContext sc)
        {
            return sc._ctx;
        }
    }
}