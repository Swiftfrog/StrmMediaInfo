using MediaBrowser.Model.Dto;

namespace Evermedia
{
    /// <summary>
    /// 用于序列化到 .medinfo 文件的 数据模型。
    /// 严格按照您的设计，只包含核心 MediaSourceInfo。
    /// </summary>
    public class MediaInfoModel
    {
        public MediaSourceInfo MediaSource { get; set; }
        
        // 未来可以根据您的设计，在这里添加 Chapters 等其他信息
        // public List<ChapterInfo> Chapters { get; set; }
    }
}

