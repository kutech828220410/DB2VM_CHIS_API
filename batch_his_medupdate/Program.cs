using System;
using System.Collections.Generic;
using Basic;
using HIS_DB_Lib;
using System.Xml;
using System.Threading.Tasks;
using SQLUI;
namespace batch_his_medupdate
{
    class Program
    {
        static void Main(string[] args)
        {
            Logger.Log("HIS藥檔更新開始...");
            try
            {
                List<medClass> medClasses = medClass.get_med_cloud("http://127.0.0.1:4433");
                Dictionary<string, List<medClass>> keyValuePairs_medcloud =  medClasses.CoverToDictionaryByCode();
                List<medClass> medClasses_buf = new List<medClass>();
                List<medClass> medClasses_HIS = new List<medClass>();
                List<medClass> medClasses_delete = new List<medClass>();
                List<medClass> medClasses_add = new List<medClass>();
                List<Task> tasks = new List<Task>();
                for(int i = 0; i < medClasses.Count; i++)
                {
                    string Code = medClasses[i].藥品碼;
                    string Name = medClasses[i].藥品名稱;
                    int index = i;
                    tasks.Add(Task.Run(new Action(delegate
                    {
                        
                    })));
  
                    medClass medClass = GetHISMed(Code);

                    if (medClass != null)
                    {
                        Console.WriteLine($"{index}.({Code}){Name}".StringLength(100) + "取得成功");
                        medClasses_HIS.Add(medClass);
                    }
                    else
                    {
                        //Console.WriteLine($"{index}.({Code}){Name}".StringLength(120) + "取得失敗");
                        medClasses_delete.Add(medClass);
                    }
                }
                Task.WhenAll(tasks).Wait();
                for (int i = 0; i < medClasses_HIS.Count; i++)
                {
                    medClasses_buf =  keyValuePairs_medcloud.SortDictionaryByCode(medClasses_HIS[i].藥品碼);
                    if (medClasses_buf.Count > 0)
                    {
                        medClasses_HIS[i].GUID = medClasses_buf[0].GUID;
                        medClasses_add.Add(medClasses_HIS[i]);
                    }
                    else
                    {
                        medClasses_HIS[i].GUID = Guid.NewGuid().ToString();
                        medClasses_add.Add(medClasses_HIS[i]);
                    }
                   
                }
                medClass.add_med_clouds("http://127.0.0.1:4433", medClasses_add);
            }
            catch (Exception e)
            {

            }
            finally
            {

            }
        }
        static medClass GetHISMed(string Code)
        {

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

            if (xmlElement == null)
            {
                return null;
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
            medClass _medClass = new medClass();
            if (_medClass == null)
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
            return _medClass;
        }
    }
}
