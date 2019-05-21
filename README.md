# fastReportTest
检测.net core 下FastReport 打印模板是否有内存泄露，一个.net core tool

## 问题描述
.Net core 下提供的打印服务，利用了第三方的收费库FastReport,该类库在.net Framework下一只表现不错，因此我司在微服务迁移后，仍采用该方案。
上线后，某天某人编辑了打印模板，结果几个小时后，悲剧发生，微服务崩溃，打印服务彻底宕机，紧急增加虚机配置，并无什么卵用。
## 问题分析
最直接的方法：比对模板文件，并未发现不妥，有些字体方面的不同，也是必须的，因此初期并未重视。
线上已经炸锅，怎么办？
那就直接上测试吧，把之前的打印快速移植到一个控制台程序，模板文件加载上。测试一切OK。什么鸟问题？
循环加载试下，oooop，内存竟然在涨，不会自己释放吗？
比对下旧模板，嗯，内存可以释放。那可以确定是模板的问题了！
再次比对FastReport打印模板，决定吧字体删除，这下测试下，并不 增加内存，好了，上线，问题消除。
## 线上怎么办？
FastReport类库最致命的是内存一直暴涨，直至服务崩溃，作为微服务的部署，只能把其迁移到独立服务器，不至于影响别的服务。
**因为是第三方类库，因此，其实没有更好的解决途径！！！**
## 写个测试工具，测试下打印模板文件
如果内存不增加，则可以使用该模板，就这么搞。
测试小工具利用.net core tool方式写。
### 拉取模板中的数据源
```csharp
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
```
### 模拟Datatable
```csharp
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
```
### 打印
```csharp
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
```
## 见好就收
.net core 引入第三方类库，谨慎谨慎，特别是收费的，非开源的！！！
## 引用链接
1. [口袋代码仓库](http://codeex.cn)
2. [在线计算器](http://jisuanqi.codeex.cn)
3. 本节源码：[github](https://github.com/webmote-org/)
