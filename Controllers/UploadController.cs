using Newtonsoft.Json;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Runtime.InteropServices;
using Excel = Microsoft.Office.Interop.Excel;
using System.Reflection;
using HumanaApp.Models;
using System.Data.SqlClient;

namespace HumanaApp.Controllers
{
    public class UploadController : Controller
    {
        private readonly HumanaEntities humana;

        public UploadController()
        {
            humana = new HumanaEntities();
        }



        [HttpGet]
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Index(HttpPostedFileBase file)
        {
            if (file != null && file.ContentLength > 0)
            {
                try
                {
                    string fileName = Path.GetFileName(file.FileName);
                    string filePath = Path.Combine(Server.MapPath("~/App_Data/Uploads"), fileName);

                    if (!Directory.Exists(Server.MapPath("~/App_Data/Uploads")))
                    {
                        Directory.CreateDirectory(Server.MapPath("~/App_Data/Uploads"));
                    }

                    // Save the file on the server
                    file.SaveAs(filePath);

                    // Save file information to the database
                    using (var db = new HumanaEntities()) // Replace YourDbContext with your actual DbContext
                    {
                        tblExcelfile fileModel = new tblExcelfile
                        {
                            Filename = fileName,
                        };

                        db.tblExcelfiles.Add(fileModel);
                        db.SaveChanges();
                    }

                    ViewBag.Message = fileName + "File uploaded successfully!";
                }
                catch (Exception ex)
                {
                    ViewBag.Message = "Error while uploading the file: " + ex.Message;
                }
            }
            else
            {
                ViewBag.Message = "Please select a file to upload.";
            }

            return View("Index");
        }



        public ActionResult ShowFiles()
        {
            return View();
        }


        public ActionResult GetExcelFiles()
        {
            List<string> fileNames = GetExcelFilesList();
            return Json(fileNames, JsonRequestBehavior.AllowGet);
        }




        public List<string> GetExcelFilesList()
        {
            string uploadFolderPath = Server.MapPath("~/App_Data/Uploads");
            string[] files = Directory.GetFiles(uploadFolderPath, "*.xlsx");

            List<string> fileNames = new List<string>();
            foreach (string filePath in files)
            {
                fileNames.Add(Path.GetFileName(filePath));
            }

            return fileNames;
        }

        public ActionResult ViewData(string fileName)
        {
            return View();
        }


        [HttpGet]
        public ActionResult GetExcelData(string fileName)
        {
            string filePath = Server.MapPath("~/App_Data/Uploads/") + Uri.UnescapeDataString(fileName);
            Excel.Application excelApp = null;
            Excel.Workbook workbook = null;
            try
            {
                excelApp = new Excel.Application();
                workbook = excelApp.Workbooks.Open(filePath);
                Excel.Worksheet worksheet = (Excel.Worksheet)workbook.Sheets[1]; // Accessing the first sheet
                List<Dictionary<string, string>> excelData = new List<Dictionary<string, string>>();
                for (int row = 2; row <= worksheet.UsedRange.Rows.Count; row++) // Assuming data starts from row 2
                {
                    Dictionary<string, string> rowData = new Dictionary<string, string>();
                    for (int col = 1; col <= worksheet.UsedRange.Columns.Count; col++)
                    {
                        var columnNameRange = (Excel.Range)worksheet.Cells[1, col];
                        var cellValueRange = (Excel.Range)worksheet.Cells[row, col];

                        string columnName = columnNameRange.Value != null ? columnNameRange.Value.ToString() : "";
                        string cellValue = cellValueRange.Value != null ? cellValueRange.Value.ToString() : "";


                        rowData[columnName] = cellValue;
                    }
                    excelData.Add(rowData);
                }
                return Json(excelData, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = "An error occurred while loading Excel data." });
            }
            finally
            {
                if (workbook != null)
                    workbook.Close(false, Missing.Value, Missing.Value);



                if (excelApp != null)
                {
                    excelApp.Quit();
                    Marshal.ReleaseComObject(excelApp);
                }
            }
        }
        public ActionResult LoadData(string fileName)
        {
            string filePath = Server.MapPath("~/App_Data/Uploads/") + Uri.UnescapeDataString(fileName);
            List<List<string>> excelData = new List<List<string>>();
            List<string> columnNames = new List<string>();

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets[0];

                // Read column names from the first row
                for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                {
                    columnNames.Add(worksheet.Cells[1, col].Value.ToString());
                }

                // Create a DbContext instance (replace YourDbContext with your actual DbContext)
                using (var db = new HumanaEntities())
                {
                    var names = db.tblemployees.Select(x => x.SS_ID).ToList();
                    ViewBag.NameList = new SelectList(names);

                    // Start reading data from the second row
                    for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                    {
                        var rowData = new List<string>();
                        for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                        {
                            var cellValue = worksheet.Cells[row, col].Value;
                            rowData.Add(cellValue != null ? cellValue.ToString() : null);
                        }

                        // Create an instance of tblClaimsdata and populate its properties
                        var dataToSave = new tblClaimsdata
                        {
                            ProcessDate = DateTime.TryParse(rowData[0], out var processDateValue) ? (DateTime?)processDateValue : null,
                            AuditID = rowData[1],
                            ProcessorName = rowData[2],
                            BatchNo = rowData[3],
                            Leads = rowData[4],
                            ClientID = rowData[5],
                            TotalChargeAmount = decimal.TryParse(rowData[6], out var totalChargeAmountValue) ? (decimal?)totalChargeAmountValue : null,
                            TotalPaidAmount = decimal.TryParse(rowData[7], out var totalPaidAmountValue) ? (decimal?)totalPaidAmountValue : null,
                            Claimnumber = rowData[8],
                            OriginalCorrectedClaim = rowData[9],
                            Auditor = rowData[10],
                            AuditDate = DateTime.TryParse(rowData[11], out var auditDateValue) ? (DateTime?)auditDateValue : null,
                            IHT_nonIHT = bool.TryParse(rowData[12], out var ihtNonIhtValue) ? (bool?)ihtNonIhtValue : null,
                            ErrorComment = rowData[13],
                            ErrorType = rowData[14],
                            OverUnderPayment = rowData[15],
                            ErrorCode = rowData[16],
                            ErrorCategory = rowData[17],
                            ErrorSubCategory = rowData[18],
                            CorrectionStatus = rowData[19],
                            RebuttalComment = rowData[20],
                            AuditorAgreeDisagreeonRebuttal = rowData[21],
                            Auditorcommentifdisagree = rowData[22],
                            AssignName = rowData[23],
                            Date = DateTime.Now,
                            Comments = rowData[25],
                            Status = rowData[26],

                            // Add mappings for other properties
                        };

                        // Add the data to the DbContext and save changes to the database
                        db.tblClaimsdatas.Add(dataToSave);
                    }

                    db.SaveChanges();
                }
            }

            ViewBag.ColumnNames = columnNames;
            return View(excelData);
        }





        public JsonResult DeleteExcelFile(string fileName)
        {
            try
            {
                string filePath = Server.MapPath("~/App_Data/Uploads/") + Uri.UnescapeDataString(fileName); // Replace with the actual file path

                // Check if the file exists and delete it
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);

                    // Delete the associated data from the database
                    using (var db = new HumanaEntities()) // Replace YourDbContext with your actual DbContext
                    {
                        var dataToDelete = db.tblExcelfiles.Where(data => data.Filename == fileName).ToList();
                        db.tblExcelfiles.RemoveRange(dataToDelete);
                        db.SaveChanges();
                    }

                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, message = "File not found." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public ActionResult Assign(string fileName)
        {
            ViewBag.FileName = fileName;
            return View();
        }


        public ActionResult GetAuditData()
        {
            using (var db = new HumanaEntities()) // Replace YourDbContext with your actual DbContext
            {
                var data = db.tblClaimsdatas.ToList();
                return View(data);
            }
            
        }
        [HttpPost]
        public ActionResult AssignData(string selectedIds, string assignName, string comment)
        {
            // Split the selectedIds into an array of integers
            var idArray = selectedIds.Split(',').Select(int.Parse).ToArray();

            // Call the stored procedure for each ID
            using (var connection = new SqlConnection(@"Data Source=DESKTOP-J262VFN\SQLEXPRESS;Initial Catalog=HumanaDB;Integrated Security=True;MultipleActiveResultSets=True;Application Name=EntityFramework"))
            {
                connection.Open();

                foreach (var id in idArray)
                {
                    using (var command = new SqlCommand("Claimsaudit_Assign_Update", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@Id", id);
                        command.Parameters.AddWithValue("@Assignname", assignName);
                        command.Parameters.AddWithValue("@Comments", comment);

                        command.ExecuteNonQuery();
                    }
                }

                connection.Close();
            }

            // Redirect to a success page or return a success message
            return RedirectToAction("GetAuditData");

        }

    }

}


