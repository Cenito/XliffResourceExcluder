using System.Linq;
using System.Xml.Linq;

namespace XliffResourceExcluder
{
    public class TranslationUnit
    {
        public string ResourceFile { get; set; }

        public string Key { get; set; }

        public string SourceLanguage { get; set; }

        public string TargetLanguage { get; set; }

        public string SourceText { get; set; }

        public string TargetText { get; set; }

        public XElement XliffNode { get; set; }
    }
}
