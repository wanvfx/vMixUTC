using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace vMixAPI
{
    [Serializable]
    public class Output : DependencyObject, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [XmlAttribute("type")]
        public string Type {  get; set; }
        [XmlAttribute("number")]
        public int Number { get; set; }
        [XmlAttribute("source")]
        public string Source { get; set; }
        [XmlAttribute("ndi")]
        public bool NDI { get; set; }

    }
}
