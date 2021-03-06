﻿// -----------------------------------------------------------------------
// <copyright company="Fireasy"
//      email="faib920@126.com"
//      qq="55570729">
//   (c) Copyright Fireasy. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Fireasy.Common.Security;

namespace Fireasy.Common.Serialization
{
    /// <summary>
    /// 基于二进制的加密序列化方法，对象序列化后进行加密处理，以确定数据的安全性。
    /// </summary>
    public sealed class BinaryCryptoSerializer : BinarySerializer
    {
        private readonly ICryptoProvider cryptoProvider;

        /// <summary>
        /// 初始化 <see cref="BinaryCryptoSerializer"/> 类的新实例。
        /// </summary>
        /// <param name="cryptoProvider">数据加密算法提供者。</param>
        public BinaryCryptoSerializer(ICryptoProvider cryptoProvider)
        {
            this.cryptoProvider = cryptoProvider;
        }

        /// <summary>
        /// 将一个对象序列化为字节数组。
        /// </summary>
        /// <param name="obj">要序列化的对象。</param>
        /// <returns>序列化后的字节数组。</returns>
        public override byte[] Serialize<T>(T obj)
        {
            try
            {
                using (var stream = new MemoryStream())
                {
                    var bin = new BinaryFormatter();
                    bin.Serialize(stream, obj);
                    var bytes = cryptoProvider.Encrypt(stream.ToArray());

                    using (var nstream = new MemoryStream())
                    {
                        if (Token != null && Token.Data != null && Token.Data.Length > 0)
                        {
                            nstream.Write(Token.Data, 0, Token.Data.Length);
                        }

                        nstream.Write(bytes, 0, bytes.Length);

                        return nstream.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new SerializationException(SR.GetString(SRKind.SerializationError), ex);
            }
        }

        /// <summary>
        /// 从一个字节数组中反序列化对象。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bytes">字节数组。</param>
        /// <returns>反序列化后的对象。</returns>
        public override T Deserialize<T>(byte[] bytes)
        {
            byte[] data;

            if (Token != null && Token.Data != null && Token.Data.Length > 0)
            {
                data = new byte[bytes.Length - Token.Data.Length];
                Array.Copy(bytes, Token.Data.Length, data, 0, data.Length);

                if (Token.Data.Where((t, i) => t != bytes[i]).Any())
                {
                    throw new SerializationException(SR.GetString(SRKind.SerializationTokenInvalid));
                }
            }
            else
            {
                data = bytes;
            }

            T obj;
            try
            {
                using (var stream = new MemoryStream(cryptoProvider.Decrypt(data)))
                {
                    var bin = new BinaryFormatter
                        {
                            Binder = new IgnoreSerializationBinder()
                        };
                    obj = (T)bin.Deserialize(stream);
                }
            }
            catch (Exception ex)
            {
                throw new SerializationException(SR.GetString(SRKind.DeserializationError), ex);
            }

            return obj;
        }
    }
}