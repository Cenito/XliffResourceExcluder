using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Linq;
using System.Xml.Linq;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Globalization;
using System.Collections.Generic;
using System;

namespace XliffResourceExcluder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string DefaultNamespace = "urn:oasis:names:tc:xliff:document:1.2";
        private string[] m_XliffFiles;
        private string[] m_ResourceFiles;
        private Dictionary<string, XDocument> m_XliffDocuments;

        public MainWindow()
        {
            TranslationUnits = new ObservableCollection<TranslationUnit>();

            DataContext = this;

            InitializeComponent();
        }

        public ObservableCollection<TranslationUnit> TranslationUnits { get; private set; }

        private void OnLoadXliff(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.DefaultExt = ".xlf";
            openFileDialog.Filter = "XLIFF files (.xlf)|*.xlf";
            openFileDialog.Multiselect = true;

            if (!(openFileDialog.ShowDialog() ?? false))
                return;

            m_XliffFiles = openFileDialog.FileNames;
        }

        private void OnSelectResourceFile(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.DefaultExt = ".resx";
            openFileDialog.Filter = "Resource files|*.resx;*.resw";
            openFileDialog.Multiselect = true;

            if (!(openFileDialog.ShowDialog() ?? false))
                return;

            m_ResourceFiles = openFileDialog.FileNames.Where(file => IsDefaultCultureResource(file)).Select(file => file.Replace('\\', '/').ToUpperInvariant()).ToArray();
        }

        private bool IsDefaultCultureResource(string resourceFile)
        {
            string fileName = Path.GetFileNameWithoutExtension(resourceFile);
            string cultureExtension = Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(cultureExtension))
                return true;

            return false;
        }

        private void OnPreview(object sender, RoutedEventArgs e)
        {
            TranslationUnits.Clear();
            m_XliffDocuments = new Dictionary<string, XDocument>();

            m_ProgressBar.IsIndeterminate = true;

            foreach (var xliffFile in m_XliffFiles)
            {
                if (!File.Exists(xliffFile))
                    continue;

                var xliffDocument = XDocument.Load(xliffFile);
                m_XliffDocuments.Add(xliffFile, xliffDocument);

                if (m_ExcludeNonStrings.IsChecked ?? false)
                {
                    AddTranslationUnits(xliffDocument.Root, unit => string.Compare(unit.Attribute("translate").Value, "yes", true) == 0 && unit.Attribute("extype") != null);
                }
                else if (m_ResourceFiles != null)
                {
                    foreach (var resourceFile in m_ResourceFiles)
                    {
                        var fileNodes = xliffDocument.Descendants(XName.Get("file", DefaultNamespace)).Where(file => resourceFile.EndsWith(file.Attribute("original").Value.ToUpperInvariant()));
                        foreach (var fileNode in fileNodes)
                        {
                            AddTranslationUnits(fileNode, unit => string.Compare(unit.Attribute("translate").Value, "yes", true) == 0, fileNode);
                        }
                    }
                }
            }

            m_ProgressBar.IsIndeterminate = false;
        }

        private void AddTranslationUnits(XElement element, Func<XElement, bool> matcher, XElement fileNode = null)
        {
            var transUnits = element.Descendants(XName.Get("trans-unit", DefaultNamespace)).Where(unit => matcher(unit));
            foreach (var transUnit in transUnits)
            {
                AddTranslationUnit(transUnit, fileNode);
            }
        }

        private void AddTranslationUnit(XElement transUnit, XElement fileNode = null)
        {
            if (fileNode == null)
            {
                fileNode = FindFileNode(transUnit);
            }

            var translationUnit = new TranslationUnit()
            {
                ResourceFile = fileNode.Attribute("original").Value,
                Key = transUnit.Attribute("id").Value,
                SourceLanguage = fileNode.Attribute("source-language").Value,
                TargetLanguage = fileNode.Attribute("target-language").Value,
                SourceText = transUnit.Element(XName.Get("source", DefaultNamespace)).Value,
                TargetText = transUnit.Element(XName.Get("target", DefaultNamespace)).Value,
                XliffNode = transUnit
            };

            TranslationUnits.Add(translationUnit);
        }

        private XElement FindFileNode(XElement transUnit)
        {
            var parent = transUnit.Parent;
            while (parent != null)
            {
                if (string.Compare(parent.Name.LocalName, "file", true) == 0)
                    return parent;

                parent = parent.Parent;
            }

            throw new InvalidOperationException("Missing file node for translation unit.");
        }

        private void OnApply(object sender, RoutedEventArgs e)
        {
            m_ProgressBar.IsIndeterminate = false;
            m_ProgressBar.Maximum = TranslationUnits.Count + m_XliffDocuments.Count;
            m_ProgressBar.Value = 0;

            foreach (var translationUnit in TranslationUnits)
            {
                translationUnit.XliffNode.SetAttributeValue("translate", "no");
                m_ProgressBar.Value++;
            }

            if (m_XliffDocuments != null)
            {
                foreach (var xliffDocument in m_XliffDocuments)
                {
                    xliffDocument.Value.Save(xliffDocument.Key);
                    m_ProgressBar.Value++;
                }
            }

            TranslationUnits.Clear();
        }

        private void OnExcludeTypesCheckChanged(object sender, RoutedEventArgs e)
        {
            var checkbox = (CheckBox)sender;
            m_ResourceButton.IsEnabled = !(checkbox.IsChecked ?? false);
        }
    }
}
