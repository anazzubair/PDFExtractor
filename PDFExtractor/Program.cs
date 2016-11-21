using org.pdfclown.documents;
using org.pdfclown.documents.contents;
using org.pdfclown.documents.contents.fonts;
using org.pdfclown.documents.contents.objects;
using org.pdfclown.files;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace ConsoleApplication1
{
    class Program
    {

        private static string employee;
        private const string SEPARATOR = ",";
        private const string ENTREE = "ENTREE";
        private const string SORTIE = "SORTIE";
        private static string directoryName = "Documents";
        private static string outDirectoryName = "Processed";
        private static string DIRSEPARATOR = "\\";
        private static string PDFEXTENSION = ".pdf";
        private static string OUTEXTENSION = ".csv";
        private static string ReportHeader = "Mouvements Traités";
        private static string BEAC = "BEAC";
        private static string _ = "_";

        static void Main(string[] args)
        {
            if (!Directory.Exists(directoryName)) return;
            SetupOutputDirectory();

            var dateSegment = DateTime.Now.ToString().Replace(":", _).Replace(" ", _).Replace("/", _);
            var outputFileName = BEAC + _ + dateSegment + OUTEXTENSION;
            var outputFilePath = outDirectoryName + DIRSEPARATOR + outputFileName;
            using (var outStream = System.IO.File.CreateText(outputFilePath))
            {
                foreach (var filePath in Directory.GetFiles(directoryName))
                {
                    Processfile(filePath, outStream);
                }
            }

            SendOutputFileToServer(outputFilePath, outputFileName);
        }

        private static void SendOutputFileToServer(string outputFilePath, string outputFileName)
        {
            var host = ConfigurationManager.AppSettings["ftpServer"];
            var username = ConfigurationManager.AppSettings["ftpUsername"];
            var password = ConfigurationManager.AppSettings["ftpPassword"];
            var filePath = ConfigurationManager.AppSettings["ftpFilePath"];
            var ftpURI = "ftp://" + host + "/" + filePath + "/" + outputFileName;
            Console.WriteLine("FTP URI: " + ftpURI);
            var ftpRequest = FtpWebRequest.Create(ftpURI);
            ftpRequest.Method = WebRequestMethods.Ftp.UploadFile;
            ftpRequest.Credentials = new NetworkCredential(username, password);

            var sourceStream = new StreamReader(outputFilePath);
            byte[] fileContents = Encoding.UTF8.GetBytes(sourceStream.ReadToEnd());
            sourceStream.Close();
            ftpRequest.ContentLength = fileContents.Length;

            Stream requestStream = ftpRequest.GetRequestStream();
            requestStream.Write(fileContents, 0, fileContents.Length);
            requestStream.Close();

            var response = (FtpWebResponse)ftpRequest.GetResponse();

            Console.WriteLine("Upload File Complete, status {0}", response.StatusDescription);

            response.Close();
        }

        private static void SetupOutputDirectory()
        {
            if (!Directory.Exists(outDirectoryName))
            {
                Directory.CreateDirectory(outDirectoryName);
            }
            else
            {
                System.IO.DirectoryInfo di = new DirectoryInfo(outDirectoryName);
                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }
                foreach (DirectoryInfo dir in di.GetDirectories())
                {
                    dir.Delete(true);
                }
            }
        }

        private static void Processfile(string filePath, StreamWriter outStream)
        {
            if (!filePath.ToLower().EndsWith(PDFEXTENSION)) return;
            var document = new org.pdfclown.files.File(filePath).Document;
            var fileName = System.IO.Path.GetFileName(filePath);
            ProcessDocument(document, outStream);
        }

        private static void ProcessDocument(Document document, StreamWriter writer)
        {
            foreach (var page in document.Pages)
            {
                var scanner = new ContentScanner(page);
                Extract(scanner, writer);
            }
        }

        private static void Extract(ContentScanner level, StreamWriter writer)
        {
            if (level == null) return;

            while (level.MoveNext())
            {
                ContentObject content = level.Current;
                if (content is ShowText)
                {
                    Font font = level.State.Font;
                    var line = font.Decode(((ShowText)content).Text);

                    ProcessLine(line, writer);

                    // Extract the current text chunk, decoding it!
                    //Console.WriteLine(font.Decode(((ShowText)content).Text) + " : Index - " + index);
                }
                else if (content is Text || content is ContainerObject)
                {
                    // Scan the inner level!
                    Extract(level.ChildLevel, writer);
                }
            }
        }

        private static void ProcessLine(string line, StreamWriter writer)
        {
            if (line.Contains(ReportHeader))
            {
                var pieces = line.Split(new string[] { ":", "*" }, StringSplitOptions.None);
                employee = pieces[1].Trim();
                return;
            }

            if (line.Contains(ENTREE))
            {
                var pieces = line.Split(new string[] { ENTREE }, StringSplitOptions.None);
                writer.WriteLine(employee + SEPARATOR + ENTREE + SEPARATOR + pieces[1].Trim());
                return;
            }

            if (line.Contains(SORTIE))
            {
                var pieces = line.Split(new string[] { SORTIE }, StringSplitOptions.None);
                writer.WriteLine(employee + SEPARATOR + SORTIE + SEPARATOR + pieces[1].Trim());
                return;
            }
        }
    }
}
