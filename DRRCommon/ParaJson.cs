using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DRRCommon
{
    public class ParaJSON
    {
        private string path;

        public ParaJSON(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("Input Path is NULL or Empty!");
            }

            this.path = path;
        }
        public object LoadObjectFromFile(Type type)
        {
            try
            {
                using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    using (StreamReader streamReader = new StreamReader(stream))
                    {
                        //实例化DataContractJsonSerializer对象，需要待序列化的对象类型
                        DataContractJsonSerializer serializer = new DataContractJsonSerializer(type);
                        return serializer.ReadObject(streamReader.BaseStream);
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public void SaveObjectToFile(object obj)
        {
            try
            {
                using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    using (StreamWriter streamWriter = new StreamWriter(stream))
                    {
                        //实例化DataContractJsonSerializer对象，需要待序列化的对象类型
                        DataContractJsonSerializer serializer = new DataContractJsonSerializer(obj.GetType());
                        serializer.WriteObject(streamWriter.BaseStream, obj);
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
