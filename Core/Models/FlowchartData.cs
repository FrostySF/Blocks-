using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Blocks_.Core.Models
{
    // Класс-контейнер для всей блок-схемы
    [XmlRoot("Flowchart")]
    public class FlowchartData
    {
        // ВАЖНО: XmlSerializer требует public параметрless конструктор
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

    // Статический класс для удобной работы с XML-сериализацией
    public static class XmlDataSerializer
    {
        // Метод для сохранения (сериализации) объекта в XML-файл
        public static async Task SaveToFileAsync<T>(T data, Windows.Storage.StorageFile file)
        {
            var serializer = new XmlSerializer(typeof(T));
            using (var stream = await file.OpenStreamForWriteAsync())
            {
                // Принудительное обнуление файла перед записью
                stream.SetLength(0);
                serializer.Serialize(stream, data);
            }
        }

        // Метод для загрузки (десериализации) объекта из XML-файла
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