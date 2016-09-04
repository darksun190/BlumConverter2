using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Configuration;
using IniParser;
using CalypsoResultConverter;

namespace BlumConverter2
{
    class Program
    {
        static string inspection_name;
        static string u_status;
        static string increment_partno;

        static string output_path_name;
        static IniParser.Model.IniData dic;
        static int type_len;
        //file encoding
        static Encoding encd = Encoding.Default;
        //Encoding.GetEncoding("Windows-1252");

        static void Main(string[] args)
        {
            //about the environment, like ddmmyyyy or ,. comma dot
            Thread.CurrentThread.CurrentCulture =
                System.Globalization.CultureInfo.InvariantCulture;

            //if paras less than 2, return directly
            if (args.Length < 2)
                return;
            //Add track listener, use logfile.txt for record.
            File.Delete("logfile.txt");
            Trace.Listeners.Add(new TextWriterTraceListener("logfile.txt", "myListener"));

            //read settings from exe.ini file.
            type_len =
              Convert.ToInt32(ConfigurationManager.AppSettings["type_len"]);
            //input & output file from command line paras
            string input_file_name = args[0];

            output_path_name = args[1];

            //save the input strings from the calypso result
            List<string> contents = new List<string>();
            Trace.WriteLine("Start init the dictionary");

            //read the ini file
            init_dic();
            //try to record the text from the source file only for debug
            try
            {
                Trace.WriteLine("Init Finished");

                Trace.WriteLine("start to load File : " + input_file_name);
                var test = File.ReadAllBytes(input_file_name);
                Trace.WriteLine("----------------ASCII code-----------------");
                Trace.WriteLine(Encoding.ASCII.GetString(test));
                Trace.WriteLine("----------------UTF-8 code-----------------");
                Trace.WriteLine(Encoding.UTF8.GetString(test));
                Trace.WriteLine("----------------Default code-----------------");
                //Trace.WriteLine(Encoding.Default.GetString(test));
                Trace.WriteLine(Encoding.GetEncoding("Windows-1252").GetString(test));
            }
            catch
            {
                Trace.WriteLine("Exception when read & encoding");
                Trace.Flush();
            }
            //read all data from the 3 txt files.


            string file_header_name;
            FileInfo input_file_info = new FileInfo(input_file_name);
            FileInfo hdr_file, chr_file, fet_file;
            file_header_name = input_file_info.FullName;
            file_header_name = file_header_name.Substring(0, file_header_name.Count() - 7);
            hdr_file = new FileInfo(file_header_name + "hdr.txt");
            chr_file = new FileInfo(file_header_name + "chr.txt");
            fet_file = new FileInfo(file_header_name + "fet.txt");

            if (!hdr_file.Exists ||
                !chr_file.Exists ||
                !fet_file.Exists)
            {
                throw new IOException("file didn't exist");
            }
            var OR = CalypsoResultConverter.CalypsoTableResult.getORfromFile(
                hdr_file, chr_file, fet_file
                );

            inspection_name = OR.HeaderData["planid"];
            //increment_partno = OR.HeaderData[""]

            Trace.WriteLine("start to Save File : " + output_path_name);

            //save to target file.
            writeResult(output_path_name, OR);
            Trace.WriteLine("Write Finish");
            Trace.Flush();

        }

        private static void writeResult(string output_path_name, OperationResult oR)
        {
            StreamWriter sw = new StreamWriter(File.Open(output_path_name, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write), encd);
            sw.WriteLine("  #####  Start Automatik: {0}", output_path_name);
            string u_status = "";
            try
            {
                u_status = oR.HeaderData["u_status"];
            }
            catch
            {

            }
            sw.WriteLine(oR.HeaderData["planid"]);
            sw.WriteLine(u_status);
            sw.WriteLine(oR.HeaderData["partnb"]);

            for (int i = 0; i < oR.CharData.Rows.Count; ++i)
            {
                var ddd = dic["shortname"];
                string str1_type;
                string type = oR.CharData["type", i];
                if (ddd.ContainsKey(type))
                {
                    str1_type = ddd[type];
                }
                else
                {
                    str1_type = "UNK";
                }
                if (str1_type.Length > type_len)
                {
                    str1_type = str1_type.Substring(0, type_len);
                }
                else
                {
                    str1_type = str1_type.PadRight(type_len);
                }

                double nom, act, upt, lot, dev, exceed;
                act = Convert.ToDouble(oR.CharData["actual", i]);
                nom = Convert.ToDouble(oR.CharData["nominal", i]);
                upt = Convert.ToDouble(oR.CharData["uppertol", i]);
                lot = Convert.ToDouble(oR.CharData["lowertol", i]);
                dev = Convert.ToDouble(oR.CharData["deviation", i]);

                string str2_act = act.ToString("0.0000").PadLeft(11);
                string str3_nom = nom.ToString("0.0000").PadLeft(11);
                string str4_upt = upt.ToString("0.0000").PadLeft(11);
                string str5_lot = lot.ToString("0.0000").PadLeft(11);
                string str6_dev = dev.ToString("0.0000").PadLeft(11);
                string str7_exceed;
                string str8_sign;
                if (oR.CharData["exceed", i] == string.Empty)  //means inside the tolerance
                {
                    str7_exceed = string.Empty.PadLeft(11);
                    double step = (upt - lot) / 8.0d;
                    int j;
                    for (j = 0; j < 8; ++j)
                    {
                        double s, b;
                        s = lot + j * step;
                        b = s + step;
                        if (dev > s && dev < b)
                            break;
                    }
                    switch (j)
                    {
                        case 0:
                            str8_sign = "  ----    ";
                            break;
                        case 1:
                            str8_sign = "   ---    ";
                            break;
                        case 2:
                            str8_sign = "    --    ";
                            break;
                        case 3:
                            str8_sign = "     -    ";
                            break;
                        case 4:
                            str8_sign = "      +   ";
                            break;
                        case 5:
                            str8_sign = "      ++  ";
                            break;
                        case 6:
                            str8_sign = "      +++ ";
                            break;
                        case 7:
                            str8_sign = "      ++++";
                            break;
                        default:
                            str8_sign = "          ";
                            break;
                    }
                }
                else
                {
                    exceed = Convert.ToDouble(oR.CharData["exceed",i]);
                    str7_exceed = exceed.ToString("0.0000").PadLeft(11);
                    if (Math.Abs(exceed) < 0.001) //means minus out of tol
                    {
                        if (exceed < 0)
                            str8_sign = "  <<<<    ";
                        else
                            str8_sign = "      >>>>";
                    }
                    else
                    {
                        str8_sign = exceed.ToString("0.0000").PadLeft(10);
                    }
                }
                sw.WriteLine(
                    str1_type +
                    str2_act +
                    str3_nom +
                    str4_upt +
                    str5_lot +
                    str6_dev +
                    str7_exceed +
                    str8_sign +
                    "  " +
                    oR.CharData["id", i]
                    );
            }
           
            sw.WriteLine("  #####  Stop Automatik: {0}", output_path_name);
            sw.Close();
        }

        private static void init_dic()
        {
            string ini_file_path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "name.txt");

            var ifile = new FileIniDataParser();

            dic = ifile.ReadFile(ini_file_path);


        }
    }
}
