using System;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;

namespace vMixStreamDeckLibrary
{
	// Token: 0x02000007 RID: 7
	public class JSON
	{
		// Token: 0x06000012 RID: 18 RVA: 0x000023CC File Offset: 0x000005CC
		public static XmlDocument ConvertToXML(string jsonData)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(jsonData);
			XmlDictionaryReaderQuotas quotas = new XmlDictionaryReaderQuotas();
			XmlDictionaryReader reader = System.Runtime.Serialization.Json.JsonReaderWriterFactory.CreateJsonReader(bytes, quotas);
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.Load(reader);
			return xmlDocument;
		}
	}
}
