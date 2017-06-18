using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TTPDF
{
    class PDFMaker
    {
        static int page_Count = 1;
        static int obj_Count = 1;
        static Page[] pages = new Page[20];
        static string[] fonts = new string[10];
        //files
        private string outputStreamPath = App.outputDirectory + @"\" + App.outputFileName;
        public FileStream outFileStream;

        //string fileStart = "%PDF-1.6\r\n%%EOF\r\n";
        public static string pagesRefObj = "";
        public static string resourceRefObj = "";
        public static string catalogRefObj = "";

        public int Write(string filePath)
        {
            StreamReader sr;
            try
            {
                sr = new StreamReader(filePath);
            }
            catch (Exception)
            {
                Environment.Exit(1);
                throw;
            }
            outFileStream = new FileStream(outputStreamPath, FileMode.Create, FileAccess.Write);

            //Begin the PDF file
            FileStreamWrite(outFileStream, "%PDF-1.6\r\n");         

            //Create font objects.
            FileStreamWrite(outFileStream, CreateFontObject());
            FileStreamWrite(outFileStream, "\r\n");

            //Create resource objects with the fonts.
            FileStreamWrite(outFileStream, CreateResourceObject());
            FileStreamWrite(outFileStream, "\r\n");

            //Create starting text content object and the containing Page class instance.
            pages[0] = new Page(480, 640);
            FileStreamWrite(outFileStream, pages[0].StartContentObj());
            string strLine = string.Empty;
            int height = 600;

            //Create Header for the pages
            if (sr.Peek() >= 0){
                strLine = sr.ReadLine();
                CreateHeader(strLine, 0, outFileStream);
            }

            //Create Footer for the pages
            if (sr.Peek() >= 0){
                strLine = sr.ReadLine();
                CreateFooter(strLine, 0, outFileStream);
            }

            //Complete the remainder of the text content objects and Page class instances
            while (sr.Peek() >= 0)
            {
                strLine = sr.ReadLine();
                if (strLine.Contains(@"<PDF_NEW_PAGE>") || height <= 40) {
                    FileStreamWrite(outFileStream, pages[page_Count-1].EndContentObj()+ "\r\n");
                    IncrementPageCount();
                    pages[page_Count-1] = new Page(480, 640);
                    FileStreamWrite(outFileStream, pages[page_Count-1].StartContentObj());
                    height = 600;
                    pages[page_Count-1].SetFontSize(pages[page_Count - 2].GetFontSize());
                    CreateHeader(pages[page_Count-2].GetHeader(), page_Count - 1, outFileStream);
                    CreateFooter(pages[page_Count-2].GetFooter(), page_Count - 1, outFileStream);
                    strLine = strLine.Replace(@"<PDF_NEW_PAGE>", "");
                }
                if (strLine.Contains(@"<PDF_FONT_TAG_S>")){
                    strLine = pages[page_Count-1].ChangeFontSize(strLine);
                }
                height = height - pages[page_Count - 1].GetFontSize();
                FileStreamWrite(outFileStream, pages[page_Count - 1].InsertContentLine(strLine, height));
            }
            FileStreamWrite(outFileStream, pages[page_Count-1].EndContentObj());
            FileStreamWrite(outFileStream, "\r\n");

            //Create Pages object from Page class instances found during text content creation. Update Page objects to have Pages object ID
            FileStreamWrite(outFileStream, CreatePagesObject());
            FileStreamWrite(outFileStream, "\r\n");

            //Create Page objects to reference Pages objects.
            foreach (Page p in pages) {
                if (p != null){
                    FileStreamWrite(outFileStream, p.CreatePageObject());
                    FileStreamWrite(outFileStream, "\r\n");
                }
            }

            //Create Catalog object to reference Pages object.
            FileStreamWrite(outFileStream, CreateCatalogObject() + "\r\n");

            //Create Trailer Object
            FileStreamWrite(outFileStream, CreateTrailerObject() + "\r\n");

            //End the PDF file
            FileStreamWrite(outFileStream, @"%%EOF");               
            outFileStream.Close();

            return 0;
        }
        //Writes the string on the end of the output file.
        private static void FileStreamWrite(FileStream outFileStream, string str1)
        {
            Byte[] buffer = null;
            buffer = ASCIIEncoding.ASCII.GetBytes(str1);
            outFileStream.Write(buffer, 0, buffer.Length);

        }
        public static void CreateHeader(string content, int page_ID, FileStream oFS) {
            pages[page_ID].SetHeader(content);
            FileStreamWrite(oFS, pages[page_ID].CreateHeader(content));
        }
        public static void CreateFooter(string content, int page_ID, FileStream oFS)
        {
            pages[page_ID].SetFooter(content);
            FileStreamWrite(oFS, pages[page_ID].CreateFooter(content));
        }
        public static string CreateResourceObject() {
            string objContent = PDFMaker.GetObjCount() + " 0 obj\r\n<<\r\n/ProcSet[/PDF/Text]\r\n/Font <</F1 " + fonts[0] + " >>\r\n>>\r\nendobj\r\n";
            resourceRefObj = PDFMaker.GetObjCount() + " 0 R";
            PDFMaker.IncrementObjCount();
            return objContent;
        }
        public static string CreateFontObject() {
            int fontID = PDFMaker.GetObjCount();
            string objContent = fontID + " 0 obj\r\n<<\r\n/Type /Font\r\n/Subtype /Type1\r\n/Name /F1\r\n/BaseFont /Courier\r\n>>\r\nendobj\r\n";
            PDFMaker.IncrementObjCount();
            fonts[0] = fontID + " 0 R";
            return objContent;
        }
        public static string CreatePagesObject() {
            int obj_ID = PDFMaker.GetObjCount();
            PDFMaker.pagesRefObj = obj_ID + " 0 R";
            PDFMaker.IncrementObjCount();
            string obj = obj_ID + " 0 obj\r\n<<\r\n/Type /Pages\r\n/Kids [ ";
            foreach (Page p in pages) {
                if (p != null){
                    obj = obj + p.GetID() + " 0 R ";
                    p.SetParentRefObj(obj_ID + " 0 R");
                }
            }
            obj = obj + "]\r\n/Count " + PDFMaker.GetPageCount() + "\r\n>>\r\nendobj\r\n";
            return obj;
        }
        private static string CreateCatalogObject() {
            int obj_ID = PDFMaker.GetObjCount();
            catalogRefObj = obj_ID + " 0 R";
            PDFMaker.IncrementObjCount();
            string obj = obj_ID + " 0 obj\r\n<<\r\n/Type /Catalog\r\n/Pages " + pagesRefObj + "\r\n>>\r\nendobj\r\n";
            return obj;
        }
        public static string CreateTrailerObject(){
            string obj = "trailer\r\n<<\r\n/Root " + catalogRefObj + "\r\n/Size " + obj_Count + "\r\n>>\r\n";
            return obj;
        }
        public static void IncrementObjCount(){
            obj_Count++;
        }
        public static int GetObjCount() {
            return obj_Count;
        }
        public static void IncrementPageCount(){
            page_Count++;
        }
        public static int GetPageCount(){
            return page_Count;
        }
        public static string CreateXRef() {
            string obj = "xref\r\n0 8\r\n0000000000 65535 f\r\n";
            for (int i = 0; i< obj_Count; i++) {
                obj = obj + "0000000042 00000 n\r\n";
            }
            //Trailer
            obj = "startxref\r\n42\r\n";
            return obj;
        }
    }
    public class Page {
        string header;
        int headerSize = 20;
        string footer;
        int footerSize = 10;
        int obj_ID;
        int fontSize = 12;
        int width;
        int height;
        string parentRefObj;
        string contentRefObj;
        string content;
       
        public Page(int width, int height) {
            this.width = width;
            this.height = height;
            this.obj_ID = PDFMaker.GetObjCount();
            PDFMaker.IncrementObjCount();
        }
        public string CreatePageObject() {
            string pageObj = obj_ID + " 0 obj\r\n<<\r\n/Type /Page\r\n/Parent " + parentRefObj + "\r\n/MediaBox [ 0 0 " + width + " " + height + " ]\r\n/Resources " + PDFMaker.resourceRefObj + "\r\n/Contents " + contentRefObj + "\r\n>>\r\nendobj\r\n";
            return pageObj;
        }
        public string StartContentObj(){
            contentRefObj = PDFMaker.GetObjCount() + " 0 R";
            string objDeclaration = PDFMaker.GetObjCount() + " 0 obj\r\n<<\r\n/Length 53\r\n>>\r\nstream\r\nBT\r\n";
            PDFMaker.IncrementObjCount();
            return objDeclaration;
        }
        public string EndContentObj(){
            string objCloser = "ET\r\nendstream\r\nendobj\r\n";
            return objCloser;
        }
        public string InsertContentLine(string line, int yHeight) {
            int indentPixels = 20;                   //0 = Left edge of the page
            double fontWidth = 1;                   //Scale Multiplier. 1 = Normal size
            double fontHeight = 1;                  //Scale Multiplier. 1 = Normal size
            double italics = 0;                     //Multiplier. 0 = No italics, 1 = EXTREME italics
            line = line.Replace(@"(", @"\(");
            line = line.Replace(@")", @"\)");
            string setup = "/F1 " + fontSize + " Tf\r\n" + fontWidth + " 0 " + italics + " " + fontHeight + " " + indentPixels + " " + yHeight + " Tm\r\n";
            string lineContent = "(" + line + ")Tj" + "\r\n";
            return setup + lineContent;
        }
        public string CreateHeader(string line)
        {
            int indentPixels = 20;                   //0 = Left edge of the page
            double fontWidth = 1;                   //Scale Multiplier. 1 = Normal size
            double fontHeight = 1;                  //Scale Multiplier. 1 = Normal size
            double italics = 0;                     //Multiplier. 0 = No italics, 1 = EXTREME italics
            string setup = "/F1 " + headerSize + " Tf\r\n" + fontWidth + " 0 " + italics + " " + fontHeight + " " + indentPixels + " " + (640 - headerSize - 10) + " Tm\r\n";
            string lineContent = "(" + line + ")Tj\r\n";
            return setup + lineContent;
        }
        public string CreateFooter(string line)
        {
            int indentPixels = 20;                   //0 = Left edge of the page
            double fontWidth = 1;                   //Scale Multiplier. 1 = Normal size
            double fontHeight = 1;                  //Scale Multiplier. 1 = Normal size
            double italics = 0;                     //Multiplier. 0 = No italics, 1 = EXTREME italics
            string setup = "/F1 " + footerSize + " Tf\r\n" + fontWidth + " 0 " + italics + " " + fontHeight + " " + indentPixels + " " + (footerSize + 10) + " Tm\r\n";
            string lineContent = "(" + line + ")Tj\r\n";
            string drawLine = "20 " + (footerSize + 25) + " m 460 35 l h S\r\n";
            return setup + lineContent + drawLine;
        }
        public void SetContent(string cont) {
            this.content = cont;
        }
        public void SetParentRefObj(string pRO) {
            this.parentRefObj = pRO;
        }
        public string ChangeFontSize(string fsTagLine)
        {
            int i = fsTagLine.IndexOf("<PDF_FONT_TAG_S>");
            int j = fsTagLine.IndexOf("<PDF_FONT_TAG_E>");
            string fsS = fsTagLine.Substring(i + 16, j - 16);
            int newFS = int.Parse(fsS);
            fsTagLine = fsTagLine.Remove(i, 32 + fsS.Length);
            fontSize = newFS;
            return fsTagLine;
        }
        public void SetFontSize(int fs) {
            fontSize = fs;
        }
        public int GetFontSize() {
            return fontSize;
        }
        public int GetID() {
            return obj_ID;
        }
        public void SetHeader(string hdr) {
            header = hdr;
        }
        public string GetHeader() {
            return header;
        }
        public void SetFooter(string ftr){
            footer = ftr;
        }
        public string GetFooter(){
            return footer;
        }
    }
}
