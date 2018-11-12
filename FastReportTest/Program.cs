using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Xml;
using FastReport.Export.Pdf;

namespace FastReportTest
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args == null || args.Length <= 0)
            {
                Console.WriteLine("第二个参数为数字，可设定测试次数，默认20次");
                Console.WriteLine("请输入frx文件的绝对路径");
                return;
            }

            var frx = args[0];
            //var frx = "d:\\1_OK.frx";
            var testNum = 20;
            if (args.Length > 1)
            {
                int.TryParse(args[1], out testNum);
                if (testNum < 0)
                {
                    testNum = 1;
                }
            }
            Console.WriteLine($"解析frx:[{frx}]文件的数据源");
            if (!File.Exists(frx))
            {
                Console.WriteLine($"frx:[{frx}]文件不存在!");
                return;
            }

            var currPath = Environment.CurrentDirectory;

            try
            {
                var s = ParseFrx(frx);
                var ds = InitDataSource(s);
                for (int i = 0; i < testNum; i++)
                {
                    var p = $"{currPath}{i}.pdf";
                    Print(ds, frx, p);
                    var m = GetProcessUsedMemory();
                    var n = GetProcessPrivateMemory();
                    var k = Process.GetCurrentProcess().VirtualMemorySize64 / (1024.0 * 1024.0 * 1024.0);
                    Console.WriteLine($"循环{i+1}次后，物理内存占用 {m:.4} MB，专用内存占用 {n:.4} MB，虚拟内存占用 {k:.4} GB");
                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.InnerException?.Message);
                Console.WriteLine(ex.StackTrace);
            }

            Console.ReadLine();

        }
        public static double GetProcessUsedMemory()
        {
            double usedMemory = 0;
            usedMemory = Process.GetCurrentProcess().WorkingSet64 / (1024.0 * 1024.0);
            return usedMemory;
        }

        public static double GetProcessPrivateMemory()
        {
            var currentSize = Process.GetCurrentProcess().PrivateMemorySize64 / (1024.0 * 1024.0);
            return currentSize;
        }
        public static DataSet InitDataSource(List<SourceInfo> sis)
        {
            DataSet dsData = new DataSet();
            var i = 0;
            foreach (var t in sis)
            {
                var dtTmp = new DataTable(t.TableName);
                InitTable(dtTmp, t.Columns);
                dsData.Tables.Add(dtTmp);
            }

            return dsData;
        }

        public static void Print(DataSet dsData, string frxPath, string path)
        {

            FastReport.Report report = new FastReport.Report();
            report.RegisterData(dsData);
            using (FileStream fs = new FileStream(frxPath, FileMode.Open,FileAccess.Read,FileShare.ReadWrite))
            {
                report.Load(fs);
            }
            report.Prepare();

            PDFExport export = new PDFExport();
            export.SetReport(report);
            export.Compressed = true;
            export.Background = false;
            export.PrintOptimized = false;
            export.OpenAfterExport = false;
            export.EmbeddingFonts = true;
            report.Export(export, path);
            report.Dispose();
        }

        private static List<SourceInfo> ParseFrx(string path)
        {
            try
            {
                var txt = File.ReadAllText(path);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(txt);
                XmlNodeList nodeList = doc.SelectNodes("Report/Dictionary/TableDataSource");
                List<SourceInfo> sis = new List<SourceInfo>();
                foreach (XmlNode node in nodeList)
                {
                    var si = new SourceInfo();
                    var dtName = node.Attributes["ReferenceName"].InnerText;
                    // Data.
                    si.TableName = dtName.Substring(5);
                    foreach (XmlNode child in node.ChildNodes)
                    {
                        var columnName = child.Attributes["Name"].InnerText;
                        var columnType = child.Attributes["DataType"].InnerText;
                        si.Columns[columnName] = columnType;
                    }

                    sis.Add(si);
                }
                return sis;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.InnerException?.Message);
                Console.WriteLine(ex.StackTrace);
                return null;
            }
        }

        private static void InitTable(DataTable dt, Dictionary<string, string> colDefs)
        {
            foreach (var key in colDefs.Keys)
            {
                dt.Columns.Add(key, Type.GetType(colDefs[key]));
            }
            //初始化数据
            for (int i = 0; i < 20; i++)
            {
                DataRow dr = dt.NewRow();
                foreach (var key in colDefs.Keys)
                {
                    //var t = Type.GetType(colDefs[key]);
                    switch (colDefs[key])
                    {
                        case "System.String":
                            dr[key] = "s11";
                            break;
                        case "System.DateTime":
                            dr[key] = DateTime.Now;
                            break;
                        default:
                            var obj = Assembly.GetExecutingAssembly().CreateInstance(colDefs[key]);
                            if (obj == null)
                            {
                                dr[key] = DBNull.Value;
                            }
                            else
                            {
                                dr[key] = obj;
                            }

                            break;
                    }

                }
                dt.Rows.Add(dr);
            }
        }

    }
}
