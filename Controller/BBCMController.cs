using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IBM.Data.DB2.Core;
using System.Data;
using System.Configuration;
using Basic;
using SQLUI;
using System.Xml;
using HIS_DB_Lib;
namespace DB2VM.Controller
{
    [Route("dbvm/[controller]")]
    [ApiController]
    public class BBCMController : ControllerBase
    {


        static string MySQL_server = $"{ConfigurationManager.AppSettings["MySQL_server"]}";
        static string MySQL_database = $"{ConfigurationManager.AppSettings["MySQL_database"]}";
        static string MySQL_userid = $"{ConfigurationManager.AppSettings["MySQL_user"]}";
        static string MySQL_password = $"{ConfigurationManager.AppSettings["MySQL_password"]}";
        static string MySQL_port = $"{ConfigurationManager.AppSettings["MySQL_port"]}";

        private SQLControl sQLControl_藥檔資料 = new SQLControl(MySQL_server, MySQL_database, "medicine_page_cloud", MySQL_userid, MySQL_password, (uint)MySQL_port.StringToInt32(), MySql.Data.MySqlClient.MySqlSslMode.None);


        [HttpGet]
        public string Get(string Code)
        {
            returnData returnData = new returnData();
            if (Code.StringIsEmpty()) return "[]";
            System.Text.StringBuilder soap = new System.Text.StringBuilder();
            soap.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            soap.Append("<soap:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">");
            soap.Append("<soap:Body>");
            soap.Append("<GetSTK_XML xmlns=\"http://tempuri.org/\">");
            soap.Append($"<prs_stk>{Code}</prs_stk>");
            soap.Append("</GetSTK_XML>");
            soap.Append("</soap:Body>");
            soap.Append("</soap:Envelope>");
            string Xml = Basic.Net.WebServicePost("http://192.168.163.69/TmhtcAdcWS/Service.asmx?op=GetSTK_XML", soap);
            string[] Node_array = new string[] { "soap:Body", "GetSTK_XMLResponse", "GetSTK_XMLResult", "data", "drug" };

            XmlElement xmlElement = Xml.Xml_GetElement(Node_array);
            if(xmlElement == null)
            {
                returnData.Code = -200;
                returnData.Result = $"({Code})HIS下載藥檔錯誤";
                return returnData.JsonSerializationt();
            }
            string prs_stk = xmlElement.Xml_GetInnerXml("prs_stk");
            string prs_id = xmlElement.Xml_GetInnerXml("prs_id");
            string prs_name = xmlElement.Xml_GetInnerXml("prs_name");
            string prs_sc_name = xmlElement.Xml_GetInnerXml("prs_sc_name");
            string prs_spec = xmlElement.Xml_GetInnerXml("prs_spec");
            string prs_prc_unit = xmlElement.Xml_GetInnerXml("prs_prc_unit");
            string prs_srv_unit = xmlElement.Xml_GetInnerXml("prs_srv_unit");
            string med_type = xmlElement.Xml_GetInnerXml("med_type");
            string control_level = xmlElement.Xml_GetInnerXml("control_level");
            string anesthesia = xmlElement.Xml_GetInnerXml("anesthesia");
            string url = xmlElement.Xml_GetInnerXml("url");
            string danger = xmlElement.Xml_GetInnerXml("danger");
            medClass _medClass = medClass.get_med_clouds_by_code("http://127.0.0.1:4433", prs_id);
            if(_medClass == null)
            {
                _medClass = new medClass();
                _medClass.GUID = Guid.NewGuid().ToString();
            }
            _medClass.藥品碼 = prs_stk.Trim();
            _medClass.料號 = prs_id.Trim();
            _medClass.藥品名稱 = prs_name.Trim();
            _medClass.藥品名稱 = _medClass.藥品名稱.Replace("送藥到府計畫", "");
            _medClass.藥品名稱 = _medClass.藥品名稱.Replace("(送藥到府計畫)", "");
            _medClass.藥品名稱 = _medClass.藥品名稱.Replace("送藥到府專案", "");
            _medClass.藥品名稱 = _medClass.藥品名稱.Replace("(送藥到府專案)", "");
            _medClass.藥品名稱 = _medClass.藥品名稱.Replace("(送藥到府)", "");
            _medClass.藥品名稱 = _medClass.藥品名稱.Replace("送藥到府", "");
            _medClass.藥品學名 = prs_sc_name.Trim();
            _medClass.類別 = med_type.Trim();
            _medClass.包裝單位 = prs_srv_unit.Trim();
            _medClass.警訊藥品 = (danger == "Y") ? "True" : "False";


            if (!(control_level == "1"
            || control_level == "2"
            || control_level == "3"
            || control_level == "4"))
            {
                control_level = "N";
            }
            _medClass.管制級別 = control_level;
            _medClass.圖片網址 = url;
            if (_medClass.圖片網址.StringIsEmpty())
            {
                _medClass.圖片網址 = $"http://192.168.161.30/Motion/chart/{_medClass.藥品碼}_1.jpg";
            }
            returnData = medClass.add_med_clouds("http://127.0.0.1:4433", _medClass);
            return returnData.JsonSerializationt();
        }

        [Route("update_all")]
        [HttpGet]
        public string update_all()
        {
            returnData returnData = new returnData();
            List<medClass> medClasses = medClass.get_med_cloud("http://127.0.0.1:4433");
            List<Task> tasks = new List<Task>();
            for (int i = 0; i < medClasses.Count; i++)
            {
                Get(medClasses[i].藥品碼);
            }
            returnData.Code = 200;

            return returnData.JsonSerializationt();
        }
        [Route("check_pic_url")]
        [HttpGet]
        public string check_pic_url()
        {
            returnData returnData = new returnData();
            List<medClass> medClasses = medClass.get_med_cloud("http://127.0.0.1:4433");
            List<medClass> medClasses_replace = new List<medClass>();
            for (int i = 0; i < medClasses.Count; i++)
            {
                if(medClasses[i].圖片網址.StringIsEmpty())
                {
                    medClasses[i].圖片網址 = $"http://192.168.161.30/Motion/chart/{medClasses[i].藥品碼}_1.jpg";
                    medClasses_replace.Add(medClasses[i]);
                }
         
            }
            returnData = medClass.add_med_clouds("http://127.0.0.1:4433", medClasses_replace);

            return returnData.JsonSerializationt();
        }
    }
}
