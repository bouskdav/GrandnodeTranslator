using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Linq;

namespace GrandNodeTranslator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Grandnode translator!");

            //var translationsRepoBase = new Uri("https://raw.githubusercontent.com/bouskdav/grandnode2_translations/main/");
            var baseAddress = new Uri("https://translate.google.com");
            var cookieContainer = new CookieContainer();

            XDocument originalDocument = null;
            XmlDocument document = new XmlDocument();
            FileInfo xmlFile = new FileInfo(Path.Combine(@"C:\Users\Anastazka\Downloads", "language_pack.xml"));

            using (StreamReader sr = new StreamReader(xmlFile.FullName, true))
            {
                document.Load(sr);
            }

            string fromLanguage = "en";
            string toLanguage = "ru";

            using (var handler = new HttpClientHandler())
            using (var client = new HttpClient(handler))
            {
                string originalFileName = $"https://raw.githubusercontent.com/bouskdav/grandnode2_translations/main/language_pack_{toLanguage}.xml";

                // fetch result
                var result = await client.GetAsync(originalFileName);

                if (result.IsSuccessStatusCode)
                {
                    string content = await result.Content.ReadAsStringAsync();

                    originalDocument = XDocument.Parse(content);
                }
            }

            var originalLanguageNode = originalDocument.Element("Language");
            var languageNode = document.SelectSingleNode("//Language");

            using (var handler = new HttpClientHandler() { CookieContainer = cookieContainer })
            using (var client = new HttpClient(handler) { BaseAddress = baseAddress })
            {
                //cookieContainer.Add(baseAddress, new Cookie("token", "4e8882380194fc62a2126831562d251a"));
                int i = 1;

                foreach (XmlNode item in languageNode.ChildNodes)
                {
                    string itemName = item.Attributes["Name"]?.Value;
                    string inputString = item.FirstChild.InnerText;

                    string originalValue = originalLanguageNode?
                        .Descendants()
                        .FirstOrDefault(i => i.Attribute("Name")?.Value == itemName)?.Value;

                    if (!String.IsNullOrEmpty(originalValue))
                    {
                        Console.WriteLine($"Skipping line {i} as already contains value.");

                        item.FirstChild.InnerText = originalValue;
                    }
                    else
                    {
                        // fetch result
                        var result = await client.GetAsync($"m?hl={fromLanguage}&sl={fromLanguage}&tl={toLanguage}&ie=UTF-8&prev=_m&q={HttpUtility.UrlEncode(inputString)}");

                        // continue if not successful
                        if (!result.IsSuccessStatusCode)
                            throw new Exception("Chyba!");

                        string content = await result.Content.ReadAsStringAsync();

                        HtmlDocument pageDocument = new HtmlDocument();
                        pageDocument.LoadHtml(content);

                        var translationResult = pageDocument.DocumentNode.SelectSingleNode("(//div[contains(@class,'result-container')][1])")?.InnerText;

                        item.FirstChild.InnerText = HttpUtility.UrlDecode(translationResult);

                        Console.WriteLine($"Translated line: #{i} - {inputString} / {item.FirstChild.InnerText}");
                    }

                    i++;
                }
            }

            using (StreamWriter sw = new StreamWriter(Path.Combine(@"C:\Users\Anastazka\Downloads", $"language_pack_{toLanguage}.xml"), false))
            {
                document.Save(sw);
            }
        }
    }
}
