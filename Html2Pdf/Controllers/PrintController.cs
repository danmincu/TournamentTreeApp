using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using TuesPechkin;

namespace Html2Pdf.Controllers
{
    public class PrintController : Controller
    {
        const string fileContentType = "application/pdf";
        // GET: Print
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Print(Uri uri, double zoom = 1)
        {
            if (uri == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var document = new HtmlToPdfDocument
            {
                GlobalSettings =
                {
                    ProduceOutline = true,
                    DocumentTitle = uri.Host.ToString(),
                    PaperSize = PaperKind.Letter,                                       
                    Margins =
                    {
                        All = 1.375,
                        Unit = Unit.Centimeters
                    }
                },
                Objects = {
                    //new ObjectSettings { HtmlText = "<h1>Pretty Websites</h1><p>This might take a bit to convert!</p>" },
                    new ObjectSettings {
                        PageUrl = uri.ToString(),
                        WebSettings = new WebSettings() {EnableJavascript=true, LoadImages=true,PrintMediaType=false},
                        LoadSettings = new LoadSettings() { ZoomFactor = zoom == 0 ? 1 : zoom} }                    
                }
            };
            var converter = MvcApplication.Converter;
            // convert document
            byte[] result = converter.Convert(document);
            return new FileContentResult(result, fileContentType);

        }

        [HttpPost]
        public ActionResult PrintDoc(PrintCommand command)
        {            
            if (command == null || !command.Documents.Any())
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var pdfDocs = new List<byte[]>();

            foreach (var doc in command.Documents)
            {
                var document = new HtmlToPdfDocument
                {
                    GlobalSettings =
                {
                    ProduceOutline = true,
                    DocumentTitle = command.Name,
                    PaperSize = doc.PageSize.Equals("A4", StringComparison.OrdinalIgnoreCase) ? PaperKind.A4 :
                                  doc.PageSize.Equals("A2", StringComparison.OrdinalIgnoreCase) ?  PaperKind.A2 : PaperKind.Letter,                                       
                    Orientation = doc.Orientation.Equals("Landscape", StringComparison.OrdinalIgnoreCase)? GlobalSettings.PaperOrientation.Landscape : GlobalSettings.PaperOrientation.Portrait,
                    Margins =
                    {
                        All = doc.MarginAll == 0 ? 1.375 : doc.MarginAll,
                        //Top = doc.MarginAll == 0 ? 1.375 : doc.MarginAll,
                        //Left =  doc.MarginAll == 0 ? 1.375 : doc.MarginAll,
                        //Right =  doc.MarginAll == 0 ? 1.375 : doc.MarginAll,
                        //Bottom = 10,
                        Unit = Unit.Centimeters
                    }
                },
                    Objects = {                  
                    new ObjectSettings {
                        PageUrl = doc.GetAddress(),
                        WebSettings = new WebSettings() {EnableJavascript=true, LoadImages=true,PrintMediaType=false},
                        LoadSettings = new LoadSettings() { ZoomFactor = doc.ZoomFactor == 0 ? 1 : doc.ZoomFactor} }                    
                }
                };
                var converter = MvcApplication.Converter;

                var retries = 2;
                while (retries > 0)
                {
                    try
                    {
                        pdfDocs.Add(converter.Convert(document));
                        retries = 0;
                    }
                    catch
                    {
                        //burry any error while there are still retries available
                        retries--;
                    }
                }
            }
            if (pdfDocs.Count > 1)
            {
                using (var ms = new MemoryStream())
                {
                    //I wish I "know" white documents so I can ignore them...
                    MergePdfFiles(ms, pdfDocs.Select(ba => new MemoryStream(ba)).ToArray());
                    var fcr = new FileContentResult(ms.ToArray(), fileContentType);
                    fcr.FileDownloadName = command.Name + ".pdf";
                    return fcr;
                }
            }
            else
                if (pdfDocs.Count == 1)
                  return new FileContentResult(pdfDocs.First(), fileContentType);
            return new HttpStatusCodeResult(HttpStatusCode.RequestedRangeNotSatisfiable);
        }
        
        private static void MergePdfFiles(MemoryStream outputPdf, Stream[] sourcePdfs)
        {
            PdfReader reader = null;
            Document document = new Document();
            PdfImportedPage page = null;
            PdfCopy pdfCpy = null;
            int n = 0;
            int totalPages = 0;
            int page_offset = 0;
            List<Dictionary<string, object>> bookmarks = new List<Dictionary<string, object>>();
            IList<Dictionary<string, object>> tempBookmarks;
            for (int i = 0; i <= sourcePdfs.GetUpperBound(0); i++)
            {
                reader = new PdfReader(sourcePdfs[i]);
                reader.ConsolidateNamedDestinations();
                n = reader.NumberOfPages;
                tempBookmarks = SimpleBookmark.GetBookmark(reader);

                if (i == 0)
                {
                    document = new iTextSharp.text.Document(reader.GetPageSizeWithRotation(1));
                    pdfCpy = new PdfCopy(document, outputPdf);
                    document.Open();
                    SimpleBookmark.ShiftPageNumbers(tempBookmarks, page_offset, null);
                    page_offset += n;
                    if (tempBookmarks != null)
                        bookmarks.AddRange(tempBookmarks);                    
                    totalPages = n;
                }
                else
                {
                    SimpleBookmark.ShiftPageNumbers(tempBookmarks, page_offset, null);
                    if (tempBookmarks != null)
                        bookmarks.AddRange(tempBookmarks);

                    page_offset += n;
                    totalPages += n;
                }

                for (int j = 1; j <= n; j++)
                {
                    page = pdfCpy.GetImportedPage(reader, j);
                    pdfCpy.AddPage(page);

                }
                reader.Close();

            }
            pdfCpy.Outlines = bookmarks;
            document.Close();
        }
    }
}