using System;
using System.Collections.Generic;

namespace QuickJS.Utils
{
    using UnityEngine;

    public interface IFileSystem
    {
        bool Exists(string path);

        /// <summary>
        /// 读取文件内容, 无法读取时返回 null (不应该抛异常)
        /// </summary>
        byte[] ReadAllBytes(string path);

        /// <summary>
        /// 读取文件内容, 无法读取时返回 null (不应该抛异常)
        /// </summary>
        string ReadAllText(string path);
    }

    public class DefaultFileSystem : IFileSystem
    {
        public bool Exists(string path)
        {
            return System.IO.File.Exists(path);
        }

        public byte[] ReadAllBytes(string path)
        {
            try
            {
                return System.IO.File.ReadAllBytes(path);
            }
            catch (Exception exception)
            {
                var logger = ScriptEngine.GetLogger();
                logger.Write(LogLevel.Error, "{0}: {1}\n{2}", path, exception.Message, exception.StackTrace);
                return null;
            }
        }

        public string ReadAllText(string path)
        {
            try
            {
                return System.IO.File.ReadAllText(path);
            }
            catch (Exception exception)
            {
                var logger = ScriptEngine.GetLogger();
                logger.Write(LogLevel.Error, "{0}: {1}\n{2}", path, exception.Message, exception.StackTrace);
                return null;
            }
        }
    }
}
