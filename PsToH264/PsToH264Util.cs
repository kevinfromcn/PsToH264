#region  版权说明
/*------------------------------------------------------------------------
//Copyright:Copyright © 2019 All Rights Reserved. ZHAOKE
//ProjectName：PsToH264
//FileName：PsToH264Util
//Description：PS流转H.264
//CLRVersion：4.0.30319.42000
//GUID:1d6dfc46-1a44-4435-aa8f-4d087705c31f
//
//Author：zhaoke
//CreateDate：2019/3/23 18:02:53
//Email:kevinfromcn@outlook.com
//
//ChangeAuthor:
//ChangeDate：
//Description：
------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;

namespace PsToH264
{
    /// <summary>
    /// PS流转H.264
    /// </summary>
    public class PsToH264Util : IDisposable
    {
        #region 构造析构
        /// <summary>
        /// 构造函数
        /// </summary>
        public PsToH264Util()
        {
            this.psBuffer = new List<byte>();
            this.h264Buffer = new List<byte>();
            this.parseState = 0;
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        public void Dispose()
        {
            this.psBuffer.Clear();
            this.h264Buffer.Clear();
            this.parseState = 0;
        }
        #endregion

        #region 字段属性
        /// <summary>
        /// ps流缓存
        /// </summary>
        private List<byte> psBuffer;

        /// <summary>
        /// h264流缓存
        /// </summary>
        private List<byte> h264Buffer;

        /// <summary>
        /// 解析状态
        /// 
        /// 0 默认状态，寻找包头
        /// 1 验证系统头状态
        /// 2 验证Program Stream Map状态
        /// 3 解析PS流状态
        /// </summary>
        private byte parseState;

        /// <summary>
        /// ps流缓存长度
        /// </summary>
        public int PsBufferCount
        {
            get
            {
                return this.psBuffer.Count;
            }
        }

        /// <summary>
        /// h264流缓存长度
        /// </summary>
        public int H264BufferCount
        {
            get
            {
                return this.h264Buffer.Count;
            }
        }
        #endregion

        #region 对外函数
        /// <summary>
        /// 写入流
        /// </summary>
        /// <param name="buffer">待写入的数据</param>
        public void Write(byte[] buffer)
        {
            if (buffer != null && buffer.Length > 0)
            {
                this.psBuffer.AddRange(new List<byte>(buffer));
            }
        }

        /// <summary>
        /// 读取流
        /// </summary>
        /// <returns>所有H254缓存流的数据</returns>
        public byte[] Read()
        {
            if (this.H264BufferCount > 0)
            {
                byte[] buffer = this.h264Buffer.ToArray();
                this.h264Buffer.Clear();
                return buffer;
            }

            return null;
        }

        /// <summary>
        /// 读取流
        /// 
        /// 若H264流缓存为空或读取长度大于流总长度，则返回空
        /// </summary>
        /// <param name="length">读取的长度</param>
        /// <returns>length长度的H254缓存流的数据</returns>
        public byte[] Read(int length)
        {
            //流大于0切小于等于length
            if (this.H264BufferCount > 0 && this.H264BufferCount <= length)
            {
                //读取length长度的流并从缓存中移除
                byte[] buffer = new byte[length];
                this.h264Buffer.CopyTo(0, buffer, 0, length);
                this.h264Buffer.RemoveRange(0, length);
                return buffer;
            }

            return null;
        }

        /// <summary>
        /// 执行解析
        /// </summary>
        public void ExecuteParsing()
        {
            //循环解析
            while (this.PsBufferCount >= 4)
            {
                //判断是否包含包头
                if (this.psBuffer[0] == 0x00 && this.psBuffer[1] == 0x00
                    && this.psBuffer[2] == 0x01 && this.psBuffer[3] == 0xBA)
                {
                    this.parseState = 0;
                }

                switch (this.parseState)
                {
                    case 0:
                        if (!FindAndRemoveHeader()) return;
                        break;
                    case 1:
                        if (!RemoveSystemHeader()) return;
                        break;
                    case 2:
                        if (!RemoveProgramStreamMap()) return;
                        break;
                    case 3:
                        if (!ParsingPakcet()) return;
                        break;
                    default:
                        this.parseState = 0;
                        return;
                }
            }
        }
        #endregion

        #region 对内函数
        /// <summary>
        /// 找寻并去除包头
        /// </summary>
        /// <returns>True解析继续 False解析停止</returns>
        private bool FindAndRemoveHeader()
        {
            for (int i = 0; i < PsBufferCount - 4; i++)
            {
                //判断包头
                if (this.psBuffer[i] == 0x00 && this.psBuffer[i + 1] == 0x00
                    && this.psBuffer[i + 2] == 0x01 && this.psBuffer[i + 3] == 0xBA)
                {
                    if (i > 0)
                    {
                        //找到包头，把包头之前部分遗弃
                        this.psBuffer.RemoveRange(0, i);
                    }

                    int length;
                    //判断包头长度是否为完整包头
                    if (this.PsBufferCount >= 14
                        && this.PsBufferCount >= (length = 14 + (this.psBuffer[13] & 0x07)))
                    {
                        //移除包头并继续下一步
                        this.psBuffer.RemoveRange(0, length);
                        this.parseState = 1;
                        return true;
                    }
                }
            }

            //找寻失败，解析停止
            this.parseState = 0;
            return false;
        }

        /// <summary>
        /// 找寻并移除系统头
        /// </summary>
        /// <returns>True解析继续 False解析停止</returns>
        private bool RemoveSystemHeader()
        {
            //判断是否包含系统头
            if (this.psBuffer[0] == 0x00 && this.psBuffer[1] == 0x00
               && this.psBuffer[2] == 0x01 && this.psBuffer[3] == 0xBB)
            {
                int length;
                //包含系统头，判断系统头是否完整
                if (this.PsBufferCount >= 12
                    && this.PsBufferCount > (length = this.psBuffer[4] * 256 + this.psBuffer[5] + 6))
                {
                    //系统头完整，去除系统头，解析继续
                    this.psBuffer.RemoveRange(0, length);
                    this.parseState = 2;
                    return true;
                }
                else
                {
                    //系统头不完整，解析停止
                    this.parseState = 1;
                    return false;
                }
            }
            else
            {
                //不包含系统头，解析继续
                this.parseState = 2;
                return true;
            }
        }

        /// <summary>
        /// 找寻并移除Program Stream Map
        /// </summary>
        /// <returns>True解析继续 False解析停止</returns>
        private bool RemoveProgramStreamMap()
        {
            //判断是否包含Program Stream Map
            if (this.psBuffer[0] == 0x00 && this.psBuffer[1] == 0x00
                && this.psBuffer[2] == 0x01 && this.psBuffer[3] == 0xBC)
            {
                int length;
                //包含Program Stream Map，判断Program Stream Map是否完整
                if (this.PsBufferCount >= 16
                    && this.PsBufferCount > (length = this.psBuffer[4] * 256 + this.psBuffer[5] + 6))
                {
                    //Program Stream Map完整，去除Program Stream Map，解析继续
                    this.psBuffer.RemoveRange(0, length);
                    this.parseState = 3;
                    return true;
                }
                else
                {
                    //Program Stream Map不完整，解析停止
                    this.parseState = 2;
                    return false;
                }
            }
            else
            {
                // 不包含Program Stream Map，解析继续
                this.parseState = 3;
                return true;
            }
        }

        private bool ParsingPakcet()
        {
            //判断包头
            if (this.psBuffer[0] == 0x00 && this.psBuffer[1] == 0x00
                    && this.psBuffer[2] == 0x01)
            {
                int length;
                //判断是否具有完整的PES包
                if (this.PsBufferCount >= 9
                    && this.PsBufferCount > (length = this.psBuffer[4] * 256 + this.psBuffer[5] + 6))
                {
                    //获取一个完整的PES包
                    List<byte> buffer = this.psBuffer.GetRange(0, length);
                    this.psBuffer.RemoveRange(0, length);

                    //判断是否符合，符合写入H264流
                    if (buffer[3] == 0xE0 && length > 0)
                    {
                        this.h264Buffer.AddRange(buffer.GetRange(9 + buffer[8], buffer.Count - 9 - buffer[8]));
                    }
                    //else if (buffer[3] == 0xC0 && length > 0) { }

                    //解析完成，寻找下一个包，解析继续
                    this.parseState = 1;
                    return true;
                }
                else
                {
                    //PES不完整，解析停止
                    this.parseState = 3;
                    return false;
                }
            }
            else
            {
                //数据包错误，寻找包头,解析继续
                this.parseState = 0;
                return true;
            }
        }
        #endregion
    }
}
