using org.pdfclown.documents;
using org.pdfclown.documents.contents;
using org.pdfclown.documents.contents.fonts;
using org.pdfclown.documents.contents.objects;
using org.pdfclown.files;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            using (var outStream = System.IO.File.CreateText(outDirectoryName + DIRSEPARATOR + outputFileName))
            {
                foreach (var filePath in Directory.GetFiles(directoryName))
                {
                    Processfile(filePath, outStream);
                }
            }
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
