using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DRRCommon
{
    /// <summary>
    /// XML 序列化配置文件管理
    /// </summary>
    public class ParaXML
    {
        private string path;

        /// <summary>
        /// 传入 XML 文件路径
        /// </summary>
        /// <param name="path"></param>
        public ParaXML(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("Input Path is NULL or Empty!");
            }

            this.path = path;
        }

        /// <summary>
        /// 返回 Object 的反序列化后对象，具体使用需要强制类型转换或使用反射
        /// 传入参数为待反序列化对象的 Type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public object LoadObjectFromFile(Type type)
        {
            try
            {
                using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    using (StreamReader streamReader = new StreamReader(stream))
                    {
                        XmlSerializer serializer = new XmlSerializer(type);
                        return serializer.Deserialize(streamReader);
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// 将对象序列化写入文件中
        /// </summary>
        /// <param name="obj"></param>
        public void SaveObjectToFile(object obj)
        {
            try
            {
                using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    using (StreamWriter streamWriter = new StreamWriter(stream))
                    {
                        XmlSerializer serializer = new XmlSerializer(obj.GetType());
                        serializer.Serialize(streamWriter, obj);
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}

namespace ESUtils.ParaManager
{
    public class XML<T> where T : class
    {
        private string _path;
        private T _buff;
        private DateTime _time;
        public XML(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("Input Path is NULL or Empty!");
            }

            _path = path;
        }

        public T Load()
        {
            try
            {
                using (FileStream stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    using (StreamReader streamReader = new StreamReader(stream))
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(T));
                        return serializer.Deserialize(streamReader) as T;
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public void Save(T para)
        {
            _buff = para;
            var now = DateTime.UtcNow;
            _time = now;

            Save();

            //Task.Factory.StartNew(() =>
            //{
            //    Thread.Sleep(500);

            //    if (now == _time)
            //    {
            //        Save();
            //    }
            //});
        }

        private void Save()
        {
            if (_buff == null)
            {
                return;
            }

            try
            {
                using (FileStream stream = new FileStream(_path, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    using (StreamWriter streamWriter = new StreamWriter(stream))
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(T));
                        serializer.Serialize(streamWriter, _buff);
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        ~XML()
        {
            Save();
        }
    }
}
