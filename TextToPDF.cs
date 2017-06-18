using System;
using System.Text;
using System.Collections;
using System.IO;


namespace TTPDF
{
    class App {
        public static string inputDirectory;
        public static string inputFileName;
        public static string outputDirectory;
        public static string outputFileName;
        static int Main(string[] args)
        {
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            //string path = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
            var exeDirectory = System.IO.Path.GetDirectoryName(exePath);
            inputDirectory = exeDirectory;
            outputDirectory = exeDirectory;

            if (args.Length == 2)
            {
                inputFileName = args[0];
                outputFileName = args[1];
            }
            else
            {
                inputFileName = @"input.txt";
                outputFileName = @"output.pdf";
            }
            string filePath = exeDirectory + @"\" + inputFileName;

            // Create a new PdfWriter
            PDFMaker pdf = new TTPDF.PDFMaker();

            if (filePath.Length > 0)
            {
                //Write to a PDF format file
                pdf.Write(filePath);
            }
            return 0;
        }
    }
}