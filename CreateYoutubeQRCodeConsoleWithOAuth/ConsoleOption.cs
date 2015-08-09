using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreateYoutubeQRCodeConsoleWithOAuth
{
    class ConsoleOption
    {
        [Option('o', "outputDir", Required = true, HelpText = "出力先フォルダパス")]
        public string OutputDirPath
        { get; set; }

        [Option('c', "clientID", Required = true, HelpText = "OAuth Client ID")]
        public String ClientID { get; set; }

        [Option('s', "clientSeacret", Required = true, HelpText = "OAuth Client Seacret")]
        public String ClientSeacret { get; set; }

        //(3)HelpOption属性
        [HelpOption(HelpText = "ヘルプを表示")]
        public string GetUsage()
        {
            //ヘッダーの設定
            HeadingInfo head = new HeadingInfo("CreateYoutubeQRCode", "Version 0.1");
            HelpText help = new HelpText(head);
            help.Copyright = new CopyrightInfo("KAJIWARA Yutaka", 2015);


            //全オプションを表示(1行間隔)
            help.AdditionalNewLineAfterOption = true;
            help.AddOptions(this);

            return help.ToString();
        }
    }
}
