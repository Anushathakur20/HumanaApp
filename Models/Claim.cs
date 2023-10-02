using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HumanaApp.Models
{
    public class Claim
    {
        public int id { get; set; }
        public string Title { get; set; }
        public string Claim_Number { get; set; }
        public int Client_ID { get; set; }
        public string Name { get; set; }
        public string Processor_Name_Asper_Report { get; set; }
        public int Original_Corrected_Claims { get; set; }
        public string Claims_Type { get; set; }
        public string Ex_code { get; set; }
        public int Total_Paid_Amount { get; set; }
        public string Defect_Status { get; set; }
        public string Over_Payment { get; set; }
        public int Under_Payment { get; set; }
        public string File { get; set; }
    }
}