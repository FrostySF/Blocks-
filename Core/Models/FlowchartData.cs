using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Blocks_.Core.Models
{
    [XmlRoot("Flowchart")]
    public class FlowchartData
    {
        public FlowchartData()
        {
            Blocks = new ObservableCollection<BlockItem>();
            Connections = new List<ConnectionLine>();
        }

        [XmlArray("Blocks")]
        [XmlArrayItem("Block")]
        public ObservableCollection<BlockItem> Blocks { get; set; }

        [XmlArray("Connections")]
        [XmlArrayItem("Connection")]
        public List<ConnectionLine> Connections { get; set; }
    }

    public static class XmlDataSerializer
    {
        public static async Task SaveToFileAsync<T>(T data, Windows.Storage.StorageFile file)
        {
            var serializer = new XmlSerializer(typeof(T));
            using (var stream = await file.OpenStreamForWriteAsync())
            {
                stream.SetLength(0);
                serializer.Serialize(stream, data);
            }
        }

        public static async Task<T> LoadFromFileAsync<T>(Windows.Storage.StorageFile file)
        {
            var serializer = new XmlSerializer(typeof(T));
            using (var stream = await file.OpenStreamForReadAsync())
            {
                return (T)serializer.Deserialize(stream);
            }
        }
    }
}