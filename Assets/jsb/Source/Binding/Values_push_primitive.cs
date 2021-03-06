using AOT;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace QuickJS.Binding
{
    using Native;
    using UnityEngine;


    public partial class Values
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JSValue js_push_primitive(JSContext ctx, IntPtr o)
        {
            return js_push_classvalue(ctx, o);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JSValue js_push_primitive(JSContext ctx, bool o)
        {
            return JSApi.JS_NewBool(ctx, o);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JSValue js_push_primitive(JSContext ctx, sbyte o)
        {
            return JSApi.JS_NewInt32(ctx, o);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JSValue js_push_primitive(JSContext ctx, byte o)
        {
            return JSApi.JS_NewInt32(ctx, o);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JSValue js_push_primitive(JSContext ctx, char o)
        {
            return JSApi.JS_NewInt32(ctx, o);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JSValue js_push_primitive(JSContext ctx, short o)
        {
            return JSApi.JS_NewInt32(ctx, o);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JSValue js_push_primitive(JSContext ctx, ushort o)
        {
            return JSApi.JS_NewInt32(ctx, o);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JSValue js_push_primitive(JSContext ctx, int o)
        {
            return JSApi.JS_NewInt32(ctx, o);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JSValue js_push_primitive(JSContext ctx, uint o)
        {
            return JSApi.JS_NewUint32(ctx, o);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JSValue js_push_primitive(JSContext ctx, long o)
        {
            return JSApi.JSB_NewInt64(ctx, o);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JSValue js_push_primitive(JSContext ctx, ulong o)
        {
            return JSApi.JSB_NewInt64(ctx, (long)o);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JSValue js_push_primitive(JSContext ctx, float o)
        {
            return JSApi.JS_NewFloat64(ctx, o);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JSValue js_push_primitive(JSContext ctx, double o)
        {
            return JSApi.JS_NewFloat64(ctx, o);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JSValue js_push_primitive(JSContext ctx, string o)
        {
            return JSApi.JS_NewString(ctx, o);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JSValue js_push_enumvalue<T>(JSContext ctx, T o)
                where T : Enum
        {
            return js_push_primitive(ctx, Convert.ToInt32(o));
        }
    }
}
